using HomeServices.Application.Common;
using HomeServices.Application.Errors;
using HomeServices.Application.Partners;
using HomeServices.Domain;
using HomeServices.Domain.Partners;
using HomeServices.Domain.Staff;

namespace HomeServices.Application.Tests.Partners;

public class AdminPartnersTests
{
    private readonly PartnerTestData _data = new();

    private Task<PagedResult<AdminPartnerListItemDto>> ListAsync(GetAdminPartners query) =>
        new GetAdminPartnersHandler(_data.Db).HandleAsync(query, CancellationToken.None);

    private Task<AdminPartnerDto> GetAsync(Guid id) =>
        new GetAdminPartnerHandler(_data.Db, _data.Dtos).HandleAsync(new GetAdminPartner(id), CancellationToken.None);

    private Task<AdminPartnerDto> DecideAsync(Guid id, PartnerDecision decision, string? comment = null) =>
        new DecideOnPartnerHandler(_data.Db, _data.Dtos, _data.Clock).HandleAsync(new DecideOnPartner(id, decision, comment), CancellationToken.None);

    [Fact]
    public async Task The_review_queue_lists_submitted_profiles_oldest_first()
    {
        var newer = _data.GivenProfile("Newer", submittedAt: PartnerTestData.Now.AddHours(-1));
        var older = _data.GivenProfile("Older", submittedAt: PartnerTestData.Now.AddDays(-2));
        _data.GivenProfile("Approved", PartnerStatus.Approved);
        _data.GivenProfile("Draft", PartnerStatus.Draft);

        var page = await ListAsync(new GetAdminPartners(PartnerStatus.UnderReview));

        page.TotalCount.ShouldBe(2);
        page.Items.Select(i => i.Id).ShouldBe(new[] { older.Id, newer.Id });
        var item = page.Items[0];
        item.DisplayName.ShouldBe("Older");
        item.Status.ShouldBe("UnderReview");
        item.Type.ShouldBe("Specialist");
        item.OwnerName.ShouldBe("Owner of Older");
        item.Phone.ShouldStartWith("+37499");
        item.ServiceCount.ShouldBe(2);
        item.AreaCount.ShouldBe(1);
        item.SubmittedAt.ShouldBe(PartnerTestData.Now.AddDays(-2));
    }

    [Fact]
    public async Task Without_a_status_every_profile_is_listed_newest_first()
    {
        var first = _data.GivenProfile("First", PartnerStatus.Draft, submittedAt: PartnerTestData.Now.AddDays(-3));
        var second = _data.GivenProfile("Second", PartnerStatus.Approved, submittedAt: PartnerTestData.Now.AddDays(-1));

        var page = await ListAsync(new GetAdminPartners());

        page.Items.Select(i => i.Id).ShouldBe(new[] { second.Id, first.Id });
    }

    [Fact]
    public async Task Profiles_are_filtered_by_type_and_searched_by_name_or_phone()
    {
        var company = _data.GivenProfile("Best Build LLC", type: PartnerType.Company, phone: "+37477111222");
        var specialist = _data.GivenProfile("Aram Plumbing");

        (await ListAsync(new GetAdminPartners(Type: PartnerType.Company))).Items.ShouldHaveSingleItem().Id.ShouldBe(company.Id);
        (await ListAsync(new GetAdminPartners(Search: " plumb "))).Items.ShouldHaveSingleItem().Id.ShouldBe(specialist.Id);
        (await ListAsync(new GetAdminPartners(Search: "077 111 222"))).Items.ShouldHaveSingleItem().Id.ShouldBe(company.Id);
        (await ListAsync(new GetAdminPartners(Search: "nobody"))).Items.ShouldBeEmpty();
    }

    [Fact]
    public async Task The_list_is_paged()
    {
        for (var i = 0; i < 3; i++)
        {
            _data.GivenProfile($"Partner {i}", submittedAt: PartnerTestData.Now.AddMinutes(i));
        }

        var page = await ListAsync(new GetAdminPartners(PartnerStatus.UnderReview, Page: 2, PageSize: 2));

        page.TotalCount.ShouldBe(3);
        page.TotalPages.ShouldBe(2);
        page.Items.ShouldHaveSingleItem().DisplayName.ShouldBe("Partner 2");
    }

    [Fact]
    public async Task A_reviewer_sees_the_profile_with_documents_owner_and_history()
    {
        var profile = _data.GivenProfile("Aram Plumbing", PartnerStatus.NeedsChanges);
        var reviewer = StaffUser.Create("reviewer@example.com", "Ani Reviewer", "hash");
        _data.Db.StaffUsers.Add(reviewer);
        var changes = profile.StatusChanges.OrderBy(c => c.Sequence).ToList();
        changes[0].MarkCreated(PartnerTestData.Now, profile.UserId.ToString());
        changes[1].MarkCreated(PartnerTestData.Now.AddHours(1), reviewer.Id.ToString());
        await _data.Db.SaveChangesAsync();

        var dto = await GetAsync(profile.Id);

        dto.Profile.Id.ShouldBe(profile.Id);
        dto.Profile.Documents.ShouldHaveSingleItem().File.Url.ShouldNotBeNull();
        dto.Profile.ReviewComment.ShouldBe("Fix it.");
        dto.Owner.UserId.ShouldBe(profile.UserId);
        dto.Owner.FullName.ShouldBe("Owner of Aram Plumbing");
        dto.Owner.IsBlocked.ShouldBeFalse();
        dto.History.Select(h => h.Sequence).ShouldBe(new[] { 2, 1 });
        dto.History[0].ShouldBe(new PartnerStatusChangeDto(
            2, "UnderReview", "NeedsChanges", "Fix it.", PartnerTestData.Now.AddHours(1), reviewer.Id.ToString(), "Ani Reviewer"));
        dto.History[1].ActorName.ShouldBe("Owner of Aram Plumbing");
    }

    [Fact]
    public async Task History_without_known_actors_has_no_names()
    {
        var profile = _data.GivenProfile("Aram Plumbing");
        profile.StatusChanges.Single().MarkCreated(PartnerTestData.Now, "not-a-guid");
        await _data.Db.SaveChangesAsync();

        (await GetAsync(profile.Id)).History.ShouldHaveSingleItem().ActorName.ShouldBeNull();
    }

    [Fact]
    public async Task Unknown_profiles_are_404()
    {
        (await Should.ThrowAsync<NotFoundException>(() => GetAsync(Guid.NewGuid()))).Code.ShouldBe("partner.not_found");
        (await Should.ThrowAsync<NotFoundException>(() => DecideAsync(Guid.NewGuid(), PartnerDecision.Approve))).Code.ShouldBe("partner.not_found");
    }

    [Fact]
    public async Task Approving_makes_the_partner_public()
    {
        var profile = _data.GivenProfile("Aram Plumbing");

        var dto = await DecideAsync(profile.Id, PartnerDecision.Approve);

        dto.Profile.Status.ShouldBe("Approved");
        dto.History[0].ToStatus.ShouldBe("Approved");
        _data.Db.ChangeTracker.Clear();
        (await GetAsync(profile.Id)).Profile.Status.ShouldBe("Approved");
    }

    [Theory]
    [InlineData(PartnerDecision.RequestChanges, "NeedsChanges")]
    [InlineData(PartnerDecision.Reject, "Rejected")]
    public async Task Sending_back_or_rejecting_keeps_the_comment(PartnerDecision decision, string status)
    {
        var profile = _data.GivenProfile("Aram Plumbing");

        var dto = await DecideAsync(profile.Id, decision, "  Add your license. ");

        dto.Profile.Status.ShouldBe(status);
        dto.Profile.ReviewComment.ShouldBe("Add your license.");
        dto.History[0].Comment.ShouldBe("Add your license.");
    }

    [Fact]
    public async Task Approved_partners_are_suspended_and_reinstated()
    {
        var profile = _data.GivenProfile("Aram Plumbing", PartnerStatus.Approved);

        (await DecideAsync(profile.Id, PartnerDecision.Suspend, "Complaints")).Profile.Status.ShouldBe("Suspended");
        (await DecideAsync(profile.Id, PartnerDecision.Reinstate)).Profile.Status.ShouldBe("Approved");
    }

    [Fact]
    public async Task Decisions_need_the_right_status()
    {
        var draft = _data.GivenProfile("Draft", PartnerStatus.Draft);

        (await Should.ThrowAsync<DomainException>(() => DecideAsync(draft.Id, PartnerDecision.Approve))).Code.ShouldBe("partner.not_under_review");
        (await Should.ThrowAsync<DomainException>(() => DecideAsync(draft.Id, PartnerDecision.Suspend, "x"))).Code.ShouldBe("partner.not_approved");
    }

    [Fact]
    public void List_queries_are_validated()
    {
        var validator = new GetAdminPartnersValidator();

        validator.Validate(new GetAdminPartners()).IsValid.ShouldBeTrue();
        validator.Validate(new GetAdminPartners((PartnerStatus)9, (PartnerType)9, new string('s', 101), 0, 101))
            .Errors.Select(e => e.ErrorCode)
            .ShouldBe(new[] { "status.invalid", "type.invalid", "page.invalid", "page_size.invalid", "search.too_long" }, ignoreOrder: true);
    }

    [Theory]
    [InlineData(PartnerDecision.Approve, null, true)]
    [InlineData(PartnerDecision.Reinstate, null, true)]
    [InlineData(PartnerDecision.RequestChanges, "Add a photo", true)]
    [InlineData(PartnerDecision.RequestChanges, null, false)]
    [InlineData(PartnerDecision.Reject, " ", false)]
    [InlineData(PartnerDecision.Suspend, "", false)]
    public void Comments_are_required_when_telling_the_partner_why(PartnerDecision decision, string? comment, bool valid)
    {
        var result = new DecideOnPartnerValidator().Validate(new DecideOnPartner(Guid.NewGuid(), decision, comment));

        result.IsValid.ShouldBe(valid);
        if (!valid)
        {
            result.Errors.ShouldHaveSingleItem().ErrorCode.ShouldBe("comment.required");
        }
    }

    [Fact]
    public void Comments_and_decisions_are_checked()
    {
        var result = new DecideOnPartnerValidator().Validate(
            new DecideOnPartner(Guid.NewGuid(), (PartnerDecision)9, new string('c', PartnerStatusChange.CommentMaxLength + 1)));

        result.Errors.Select(e => e.ErrorCode).ShouldBe(new[] { "decision.invalid", "comment.too_long" }, ignoreOrder: true);
    }
}
