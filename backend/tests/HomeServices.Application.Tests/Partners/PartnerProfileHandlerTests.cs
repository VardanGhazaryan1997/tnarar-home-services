using HomeServices.Application.Abstractions;
using HomeServices.Application.Errors;
using HomeServices.Application.Partners;
using HomeServices.Application.Tests.Support;
using HomeServices.Domain;
using HomeServices.Domain.Identity;
using HomeServices.Domain.Partners;
using Microsoft.EntityFrameworkCore;

namespace HomeServices.Application.Tests.Partners;

public class PartnerProfileHandlerTests
{
    private readonly PartnerTestData _data = new();

    private InMemoryAppDbContext Db => _data.Db;

    private Task<PartnerProfileDto> GetAsync(ICurrentUser? user = null) =>
        new GetMyPartnerProfileHandler(Db, user ?? _data.Me, _data.Dtos).HandleAsync(new GetMyPartnerProfile(), CancellationToken.None);

    private Task<PartnerProfileDto> SaveAsync(SaveMyPartnerProfile command, ICurrentUser? user = null) =>
        new SaveMyPartnerProfileHandler(Db, user ?? _data.Me, _data.Dtos).HandleAsync(command, CancellationToken.None);

    private Task<PartnerProfileDto> AddMediaAsync(PartnerMediaKind kind, Guid fileId, string? caption = null) =>
        new AddMyPartnerMediaHandler(Db, _data.Me, _data.Dtos).HandleAsync(new AddMyPartnerMedia(kind, fileId, caption), CancellationToken.None);

    private Task<PartnerProfileDto> RemoveMediaAsync(Guid mediaId) =>
        new RemoveMyPartnerMediaHandler(Db, _data.Me, _data.Dtos).HandleAsync(new RemoveMyPartnerMedia(mediaId), CancellationToken.None);

    private Task<PartnerProfileDto> SubmitAsync(ICurrentUser? user = null) =>
        new SubmitMyPartnerProfileHandler(Db, user ?? _data.Me, _data.Dtos, _data.Clock).HandleAsync(new SubmitMyPartnerProfile(), CancellationToken.None);

    private async Task<PartnerProfile> ReloadAsync()
    {
        Db.ChangeTracker.Clear();
        return await Db.PartnerProfiles
            .Include(p => p.Services).Include(p => p.Areas).Include(p => p.Media).Include(p => p.StatusChanges)
            .SingleAsync(p => p.UserId == _data.User.Id);
    }

    [Fact]
    public async Task A_user_without_a_profile_gets_404()
    {
        (await Should.ThrowAsync<NotFoundException>(() => GetAsync())).Code.ShouldBe("partner.not_found");
        (await Should.ThrowAsync<NotFoundException>(() => SubmitAsync())).Code.ShouldBe("partner.not_found");
        (await Should.ThrowAsync<NotFoundException>(() => AddMediaAsync(PartnerMediaKind.WorkExample, Guid.NewGuid()))).Code.ShouldBe("partner.not_found");
        (await Should.ThrowAsync<NotFoundException>(() => RemoveMediaAsync(Guid.NewGuid()))).Code.ShouldBe("partner.not_found");
    }

    [Fact]
    public async Task Saving_the_first_time_creates_a_draft_and_makes_the_user_a_partner()
    {
        var dto = await SaveAsync(_data.ValidSave());

        dto.Type.ShouldBe("Specialist");
        dto.Status.ShouldBe("Draft");
        dto.DisplayName.ShouldBe("Aram Plumbing");
        dto.YearsOfExperience.ShouldBe(12);
        dto.CategoryIds.ShouldBe(new[] { _data.Plumbing.Id });
        dto.Areas.ShouldBe(new[] { new PartnerAreaDto(_data.Yerevan.Id, null) });
        dto.CanEdit.ShouldBeTrue();
        dto.CanSubmit.ShouldBeFalse();
        dto.MissingForSubmit.ShouldBe(new[] { "work_examples" });
        dto.Avatar.ShouldBeNull();
        dto.WorkExamples.ShouldBeEmpty();
        dto.ReviewComment.ShouldBeNull();

        Db.ChangeTracker.Clear();
        (await Db.Users.SingleAsync(u => u.Id == _data.User.Id)).HasRole(UserRoles.Partner).ShouldBeTrue();
        (await GetAsync()).Id.ShouldBe(dto.Id);
    }

    [Fact]
    public async Task Saving_again_changes_the_same_profile_and_replaces_services_and_areas()
    {
        var created = await SaveAsync(_data.ValidSave());

        var updated = await SaveAsync(_data.ValidSave() with
        {
            Type = PartnerType.Company,
            DisplayName = "Aram & Sons",
            CategoryIds = [_data.Heating.Id, _data.Plumbing.Id],
            Areas = [new PartnerAreaDto(_data.Yerevan.Id, _data.Kentron.Id), new PartnerAreaDto(_data.Masis.Id, null)],
        });

        updated.Id.ShouldBe(created.Id);
        updated.Type.ShouldBe("Company");
        updated.DisplayName.ShouldBe("Aram & Sons");
        updated.CategoryIds.ShouldBe(new[] { _data.Heating.Id, _data.Plumbing.Id }, ignoreOrder: true);
        updated.Areas.Count.ShouldBe(2);
        (await Db.PartnerProfiles.CountAsync()).ShouldBe(1);

        var saved = await ReloadAsync();
        saved.Services.Count.ShouldBe(2);
        saved.Areas.Select(a => a.DistrictId).ShouldBe(new Guid?[] { _data.Kentron.Id, null }, ignoreOrder: true);
    }

    [Fact]
    public async Task A_whole_region_replaces_the_towns_chosen_inside_it()
    {
        var dto = await SaveAsync(_data.ValidSave() with
        {
            Areas = [new PartnerAreaDto(null, null, _data.Ararat.Id), new PartnerAreaDto(_data.Masis.Id, null), new PartnerAreaDto(_data.Yerevan.Id, null)],
        });

        dto.Areas.ShouldBe(new[] { new PartnerAreaDto(null, null, _data.Ararat.Id), new PartnerAreaDto(_data.Yerevan.Id, null) }, ignoreOrder: true);
        var saved = await ReloadAsync();
        saved.Areas.Select(a => (a.RegionId, a.CityId)).ShouldBe(new (Guid?, Guid?)[] { (_data.Ararat.Id, null), (null, _data.Yerevan.Id) }, ignoreOrder: true);
    }

    [Fact]
    public async Task The_avatar_comes_with_a_signed_link()
    {
        var avatar = _data.GivenFile();

        var dto = await SaveAsync(_data.ValidSave(avatar.Id));

        dto.Avatar.ShouldNotBeNull();
        dto.Avatar.Id.ShouldBe(avatar.Id);
        dto.Avatar.Url.ShouldStartWith($"https://storage.test/{avatar.Key}");
    }

    [Fact]
    public async Task Work_examples_and_documents_are_attached_and_listed_separately()
    {
        await SaveAsync(_data.ValidSave());
        var photo = _data.GivenFile();
        var video = _data.GivenFile("video/mp4");
        var license = _data.GivenFile("application/pdf");

        await AddMediaAsync(PartnerMediaKind.WorkExample, photo.Id, "Bathroom");
        await AddMediaAsync(PartnerMediaKind.WorkExample, video.Id);
        var dto = await AddMediaAsync(PartnerMediaKind.Document, license.Id, "License");

        dto.WorkExamples.Select(m => m.File.Id).ShouldBe(new[] { photo.Id, video.Id });
        dto.WorkExamples[0].Caption.ShouldBe("Bathroom");
        dto.WorkExamples[0].SortOrder.ShouldBe(1);
        dto.WorkExamples[0].File.Url.ShouldNotBeNull();
        dto.Documents.ShouldHaveSingleItem().File.Id.ShouldBe(license.Id);
        dto.MissingForSubmit.ShouldBeEmpty();
        dto.CanSubmit.ShouldBeTrue();
    }

    [Fact]
    public async Task Media_are_removed()
    {
        await SaveAsync(_data.ValidSave());
        var added = await AddMediaAsync(PartnerMediaKind.WorkExample, _data.GivenFile().Id);

        var dto = await RemoveMediaAsync(added.WorkExamples.Single().Id);

        dto.WorkExamples.ShouldBeEmpty();
        (await ReloadAsync()).Media.ShouldBeEmpty();
        (await Should.ThrowAsync<DomainException>(() => RemoveMediaAsync(Guid.NewGuid()))).Code.ShouldBe("partner.media_not_found");
    }

    [Fact]
    public async Task An_incomplete_profile_cannot_be_submitted()
    {
        await SaveAsync(_data.ValidSave());

        (await Should.ThrowAsync<DomainException>(() => SubmitAsync())).Code.ShouldBe("partner.incomplete");
        (await ReloadAsync()).Status.ShouldBe(PartnerStatus.Draft);
    }

    [Fact]
    public async Task A_complete_profile_is_submitted_and_locked_until_reviewed()
    {
        await SaveAsync(_data.ValidSave());
        await AddMediaAsync(PartnerMediaKind.WorkExample, _data.GivenFile().Id);

        var dto = await SubmitAsync();

        dto.Status.ShouldBe("UnderReview");
        dto.SubmittedAt.ShouldBe(PartnerTestData.Now);
        dto.CanEdit.ShouldBeFalse();
        var saved = await ReloadAsync();
        saved.StatusChanges.ShouldHaveSingleItem().ToStatus.ShouldBe(PartnerStatus.UnderReview);
        (await Should.ThrowAsync<DomainException>(() => SaveAsync(_data.ValidSave()))).Code.ShouldBe("partner.not_editable");
    }

    [Fact]
    public async Task The_partner_sees_what_staff_asked_to_change()
    {
        await SaveAsync(_data.ValidSave());
        await AddMediaAsync(PartnerMediaKind.WorkExample, _data.GivenFile().Id);
        await SubmitAsync();
        var profile = await ReloadAsync();
        profile.RequestChanges("Please add your license.");
        await Db.SaveChangesAsync();

        var dto = await GetAsync();

        dto.Status.ShouldBe("NeedsChanges");
        dto.ReviewComment.ShouldBe("Please add your license.");
        dto.CanEdit.ShouldBeTrue();
        dto.CanSubmit.ShouldBeTrue();
    }

    [Fact]
    public async Task Staff_and_signed_out_requests_are_refused()
    {
        var staff = new FakeCurrentUser(Guid.CreateVersion7(), isStaff: true);
        var anonymous = new FakeCurrentUser(null);

        await Should.ThrowAsync<UnauthorizedException>(() => GetAsync(staff));
        await Should.ThrowAsync<UnauthorizedException>(() => SaveAsync(_data.ValidSave(), anonymous));
        await Should.ThrowAsync<UnauthorizedException>(() => SubmitAsync(anonymous));
    }

    [Fact]
    public async Task A_deleted_account_cannot_create_a_profile()
    {
        var ghost = new FakeCurrentUser(Guid.CreateVersion7());

        await Should.ThrowAsync<UnauthorizedException>(() => SaveAsync(_data.ValidSave(), ghost));
    }
}
