using HomeServices.Application.Abstractions;
using HomeServices.Application.Partners;
using HomeServices.Application.Tests.Support;
using HomeServices.Domain.Partners;

namespace HomeServices.Application.Tests.Partners;

public class PartnerProfileValidatorTests
{
    private readonly PartnerTestData _data = new();

    private async Task<IEnumerable<string>> ErrorsAsync(SaveMyPartnerProfile command, ICurrentUser? user = null) =>
        (await new SaveMyPartnerProfileValidator(_data.Db, user ?? _data.Me).ValidateAsync(command))
            .Errors.Select(e => $"{e.PropertyName}:{e.ErrorCode}");

    private async Task<IEnumerable<string>> MediaErrorsAsync(AddMyPartnerMedia command, ICurrentUser? user = null) =>
        (await new AddMyPartnerMediaValidator(_data.Db, user ?? _data.Me).ValidateAsync(command))
            .Errors.Select(e => $"{e.PropertyName}:{e.ErrorCode}");

    [Fact]
    public async Task A_complete_profile_is_valid()
    {
        var avatar = _data.GivenFile();
        var command = _data.ValidSave(avatar.Id) with
        {
            Areas = [new PartnerAreaDto(_data.Yerevan.Id, _data.Kentron.Id), new PartnerAreaDto(_data.Masis.Id, null)],
        };

        (await ErrorsAsync(command)).ShouldBeEmpty();
    }

    [Fact]
    public async Task A_bare_draft_is_valid()
    {
        var command = new SaveMyPartnerProfile(PartnerType.Supplier, "Stone Co", null, null, null, [], []);

        (await ErrorsAsync(command)).ShouldBeEmpty();
    }

    [Fact]
    public async Task Type_and_details_are_checked()
    {
        var command = _data.ValidSave() with
        {
            Type = (PartnerType)9,
            About = new string('a', PartnerProfile.AboutMaxLength + 1),
            YearsOfExperience = 71,
        };

        (await ErrorsAsync(command)).ShouldBe(
            new[] { "Type:type.invalid", "About:about.too_long", "YearsOfExperience:years_of_experience.invalid" },
            ignoreOrder: true);
    }

    [Theory]
    [InlineData("", "DisplayName:display_name.required")]
    [InlineData("  ", "DisplayName:display_name.required")]
    [InlineData(" A ", "DisplayName:display_name.length")]
    public async Task The_name_is_required_and_has_a_length(string name, string error)
    {
        (await ErrorsAsync(_data.ValidSave() with { DisplayName = name })).ShouldBe(new[] { error });
    }

    [Fact]
    public async Task A_name_that_is_too_long_is_refused()
    {
        (await ErrorsAsync(_data.ValidSave() with { DisplayName = new string('a', PartnerProfile.DisplayNameMaxLength + 1) }))
            .ShouldBe(new[] { "DisplayName:display_name.length" });
    }

    [Fact]
    public async Task Lists_are_required()
    {
        (await ErrorsAsync(_data.ValidSave() with { CategoryIds = null!, Areas = null! }))
            .ShouldBe(new[] { "CategoryIds:categories.required", "Areas:areas.required" }, ignoreOrder: true);
    }

    [Fact]
    public async Task Only_active_categories_can_be_chosen()
    {
        (await ErrorsAsync(_data.ValidSave() with { CategoryIds = [_data.Plumbing.Id, _data.Hidden.Id] }))
            .ShouldBe(new[] { "CategoryIds:categories.invalid" });
        (await ErrorsAsync(_data.ValidSave() with { CategoryIds = [Guid.NewGuid()] }))
            .ShouldBe(new[] { "CategoryIds:categories.invalid" });
        (await ErrorsAsync(_data.ValidSave() with { CategoryIds = [_data.Plumbing.Id, _data.Plumbing.Id] }))
            .ShouldBeEmpty();
    }

    [Fact]
    public async Task Lists_have_limits()
    {
        var categories = Enumerable.Range(0, PartnerProfile.MaxServices + 1).Select(_ => Guid.NewGuid()).ToList();
        var areas = Enumerable.Range(0, PartnerProfile.MaxAreas + 1).Select(_ => new PartnerAreaDto(Guid.NewGuid(), null)).ToList();

        var errors = (await ErrorsAsync(_data.ValidSave() with { CategoryIds = categories, Areas = areas })).ToList();

        errors.ShouldContain("CategoryIds:categories.too_many");
        errors.ShouldContain("Areas:areas.too_many");
    }

    public static TheoryData<string> InvalidAreas => new() { "closed city", "closed district", "district of another city", "unknown city", "missing" };

    [Theory]
    [MemberData(nameof(InvalidAreas))]
    public async Task Areas_must_be_active_places(string which)
    {
        var area = which switch
        {
            "closed city" => new PartnerAreaDto(_data.ClosedCity.Id, null),
            "closed district" => new PartnerAreaDto(_data.Yerevan.Id, _data.ClosedDistrict.Id),
            "district of another city" => new PartnerAreaDto(_data.Masis.Id, _data.Kentron.Id),
            "unknown city" => new PartnerAreaDto(Guid.NewGuid(), null),
            _ => null!,
        };

        (await ErrorsAsync(_data.ValidSave() with { Areas = [new PartnerAreaDto(_data.Yerevan.Id, null), area] }))
            .ShouldBe(new[] { "Areas:areas.invalid" });
    }

    [Fact]
    public async Task The_avatar_must_be_my_ready_photo()
    {
        var notMine = _data.GivenFile(owner: _data.OtherUser);
        var pending = _data.GivenFile(ready: false);
        var pdf = _data.GivenFile("application/pdf");
        var mine = _data.GivenFile();

        foreach (var fileId in new[] { notMine.Id, pending.Id, pdf.Id, Guid.NewGuid() })
        {
            (await ErrorsAsync(_data.ValidSave(fileId))).ShouldBe(new[] { "AvatarFileId:avatar.invalid" });
        }

        (await ErrorsAsync(_data.ValidSave(mine.Id), new FakeCurrentUser(Guid.CreateVersion7(), isStaff: true)))
            .ShouldBe(new[] { "AvatarFileId:avatar.invalid" });
        (await ErrorsAsync(_data.ValidSave(mine.Id), new FakeCurrentUser(null)))
            .ShouldBe(new[] { "AvatarFileId:avatar.invalid" });
    }

    [Theory]
    [InlineData(PartnerMediaKind.WorkExample, "image/jpeg")]
    [InlineData(PartnerMediaKind.WorkExample, "video/mp4")]
    [InlineData(PartnerMediaKind.Document, "image/png")]
    [InlineData(PartnerMediaKind.Document, "application/pdf")]
    public async Task Fitting_files_can_be_attached(PartnerMediaKind kind, string contentType)
    {
        var file = _data.GivenFile(contentType);

        (await MediaErrorsAsync(new AddMyPartnerMedia(kind, file.Id, "Caption"))).ShouldBeEmpty();
    }

    [Theory]
    [InlineData(PartnerMediaKind.WorkExample, "application/pdf")]
    [InlineData(PartnerMediaKind.Document, "video/quicktime")]
    public async Task Files_of_the_wrong_kind_are_refused(PartnerMediaKind kind, string contentType)
    {
        var file = _data.GivenFile(contentType);

        (await MediaErrorsAsync(new AddMyPartnerMedia(kind, file.Id, null))).ShouldBe(new[] { "FileId:file.invalid" });
    }

    [Fact]
    public async Task Only_my_ready_files_can_be_attached()
    {
        var notMine = _data.GivenFile(owner: _data.OtherUser);
        var pending = _data.GivenFile(ready: false);

        (await MediaErrorsAsync(new AddMyPartnerMedia(PartnerMediaKind.WorkExample, notMine.Id, null))).ShouldBe(new[] { "FileId:file.invalid" });
        (await MediaErrorsAsync(new AddMyPartnerMedia(PartnerMediaKind.WorkExample, pending.Id, null))).ShouldBe(new[] { "FileId:file.invalid" });
    }

    [Fact]
    public async Task Media_kind_and_caption_are_checked()
    {
        var file = _data.GivenFile();

        (await MediaErrorsAsync(new AddMyPartnerMedia((PartnerMediaKind)5, file.Id, new string('c', PartnerProfile.CaptionMaxLength + 1))))
            .ShouldBe(new[] { "Kind:kind.invalid", "Caption:caption.too_long" }, ignoreOrder: true);
    }
}
