using HomeServices.Application.Abstractions;
using HomeServices.Application.Requests;
using HomeServices.Domain.Requests;

namespace HomeServices.Application.Tests.Requests;

public class RequestValidatorTests
{
    private readonly RequestTestData _data = new();

    private async Task<IEnumerable<string>> ErrorsAsync(CreateRequest command, ICurrentUser? user = null) =>
        (await new CreateRequestValidator(_data.Db, user ?? _data.Customer, _data.Clock).ValidateAsync(command))
            .Errors.Select(e => $"{e.PropertyName}:{e.ErrorCode}");

    [Fact]
    public async Task A_complete_request_is_valid()
    {
        var photo = _data.Partners.GivenFile();

        (await ErrorsAsync(_data.OpenRequest(districtId: _data.Partners.Kentron.Id) with { MediaFileIds = [photo.Id] })).ShouldBeEmpty();
        (await ErrorsAsync(_data.DirectRequest(Guid.NewGuid()))).ShouldBeEmpty();
    }

    [Fact]
    public async Task A_minimal_request_is_valid()
    {
        var command = _data.OpenRequest() with { PreferredDate = null, TimeNote = null, BudgetMin = null, BudgetMax = null, MediaFileIds = [] };

        (await ErrorsAsync(command)).ShouldBeEmpty();
    }

    [Fact]
    public async Task The_kind_decides_whether_a_partner_is_needed()
    {
        (await ErrorsAsync(_data.OpenRequest() with { Kind = (RequestKind)9 })).ShouldContain("Kind:kind.invalid");
        (await ErrorsAsync(_data.OpenRequest() with { Kind = RequestKind.Direct })).ShouldBe(new[] { "PartnerId:partner_id.required" });
        (await ErrorsAsync(_data.OpenRequest() with { PartnerId = Guid.NewGuid() })).ShouldBe(new[] { "PartnerId:partner_id.not_allowed" });
    }

    [Fact]
    public async Task The_category_and_places_must_be_active()
    {
        var p = _data.Partners;

        (await ErrorsAsync(_data.OpenRequest(categoryId: p.Hidden.Id))).ShouldBe(new[] { "CategoryId:category.invalid" });
        (await ErrorsAsync(_data.OpenRequest() with { CityId = p.ClosedCity.Id })).ShouldContain("CityId:city.invalid");
        (await ErrorsAsync(_data.OpenRequest(districtId: p.ClosedDistrict.Id))).ShouldBe(new[] { "DistrictId:district.invalid" });
        (await ErrorsAsync(_data.OpenRequest(districtId: Guid.NewGuid()))).ShouldBe(new[] { "DistrictId:district.invalid" });
    }

    [Theory]
    [InlineData("", "Description:description.required")]
    [InlineData("   ", "Description:description.required")]
    [InlineData("Too short", "Description:description.length")]
    public async Task The_description_is_required_and_long_enough(string description, string error) =>
        (await ErrorsAsync(_data.OpenRequest() with { Description = description })).ShouldBe(new[] { error });

    [Fact]
    public async Task Long_texts_are_refused()
    {
        var command = _data.OpenRequest() with
        {
            Description = new string('a', ServiceRequest.DescriptionMaxLength + 1),
            TimeNote = new string('n', ServiceRequest.TimeNoteMaxLength + 1),
        };

        (await ErrorsAsync(command)).ShouldBe(new[] { "Description:description.length", "TimeNote:time_note.too_long" }, ignoreOrder: true);
    }

    [Fact]
    public async Task The_preferred_date_cannot_be_in_the_past()
    {
        var today = DateOnly.FromDateTime(RequestTestData.Now.UtcDateTime);

        (await ErrorsAsync(_data.OpenRequest() with { PreferredDate = today.AddDays(-1) })).ShouldBeEmpty();
        (await ErrorsAsync(_data.OpenRequest() with { PreferredDate = today.AddDays(-2) })).ShouldBe(new[] { "PreferredDate:preferred_date.past" });
    }

    [Fact]
    public async Task The_budget_must_be_a_sensible_range()
    {
        (await ErrorsAsync(_data.OpenRequest() with { BudgetMin = -1, BudgetMax = null })).ShouldBe(new[] { "BudgetMin:budget.invalid" });
        (await ErrorsAsync(_data.OpenRequest() with { BudgetMin = null, BudgetMax = ServiceRequest.MaxBudget + 1 })).ShouldBe(new[] { "BudgetMax:budget.invalid" });
        (await ErrorsAsync(_data.OpenRequest() with { BudgetMin = 50_000, BudgetMax = 10_000 })).ShouldBe(new[] { "BudgetMax:budget.range_invalid" });
    }

    [Fact]
    public async Task Media_must_be_the_customers_ready_photos_or_videos()
    {
        var p = _data.Partners;
        var photo = p.GivenFile();
        var pdf = p.GivenFile("application/pdf");
        var pending = p.GivenFile(ready: false);
        var someoneElses = p.GivenFile(owner: p.OtherUser);

        (await ErrorsAsync(_data.OpenRequest() with { MediaFileIds = [photo.Id, pdf.Id] })).ShouldBe(new[] { "MediaFileIds:media.invalid" });
        (await ErrorsAsync(_data.OpenRequest() with { MediaFileIds = [pending.Id] })).ShouldBe(new[] { "MediaFileIds:media.invalid" });
        (await ErrorsAsync(_data.OpenRequest() with { MediaFileIds = [someoneElses.Id] })).ShouldBe(new[] { "MediaFileIds:media.invalid" });
        (await ErrorsAsync(_data.OpenRequest() with { MediaFileIds = [photo.Id] }, RequestTestData.Staff)).ShouldBe(new[] { "MediaFileIds:media.invalid" });
        (await ErrorsAsync(_data.OpenRequest() with { MediaFileIds = [photo.Id, photo.Id] })).ShouldBeEmpty();
    }

    [Fact]
    public async Task At_most_ten_photos()
    {
        var ids = Enumerable.Range(0, ServiceRequest.MaxMedia + 1).Select(_ => Guid.NewGuid()).ToList();

        (await ErrorsAsync(_data.OpenRequest() with { MediaFileIds = ids })).ShouldContain("MediaFileIds:media.too_many");
    }

    [Fact]
    public async Task Lists_and_reasons_are_checked()
    {
        (await new GetMyRequestsValidator().ValidateAsync(new GetMyRequests((RequestStatus)9, 0, 101))).Errors.Select(e => e.ErrorCode)
            .ShouldBe(new[] { "status.invalid", "page.invalid", "page_size.invalid" }, ignoreOrder: true);
        (await new GetInboxValidator().ValidateAsync(new GetInbox((RecipientStatus)9, 0, 0))).Errors.Select(e => e.ErrorCode)
            .ShouldBe(new[] { "status.invalid", "page.invalid", "page_size.invalid" }, ignoreOrder: true);
        (await new CancelMyRequestValidator().ValidateAsync(new CancelMyRequest(Guid.NewGuid(), new string('r', 501)))).Errors.Single().ErrorCode
            .ShouldBe("reason.too_long");
        (await new CancelMyRequestValidator().ValidateAsync(new CancelMyRequest(Guid.NewGuid(), null))).IsValid.ShouldBeTrue();
        (await new DeclineInboxRequestValidator().ValidateAsync(new DeclineInboxRequest(Guid.NewGuid(), new string('r', 501)))).Errors.Single().ErrorCode
            .ShouldBe("reason.too_long");
        (await new DeclineInboxRequestValidator().ValidateAsync(new DeclineInboxRequest(Guid.NewGuid(), null))).IsValid.ShouldBeTrue();
    }

    [Fact]
    public async Task Admin_queries_and_commands_are_checked()
    {
        (await new GetAdminRequestsValidator().ValidateAsync(new GetAdminRequests((RequestStatus)9, (RequestKind)9, Search: new string('s', 101), Page: 0, PageSize: 101)))
            .Errors.Select(e => e.ErrorCode)
            .ShouldBe(new[] { "status.invalid", "kind.invalid", "search.too_long", "page.invalid", "page_size.invalid" }, ignoreOrder: true);

        (await new AssignRequestValidator().ValidateAsync(new AssignRequest(Guid.NewGuid(), []))).Errors.Single().ErrorCode.ShouldBe("partner_ids.required");
        var many = Enumerable.Range(0, AssignRequestValidator.MaxPartners + 1).Select(_ => Guid.NewGuid()).ToList();
        (await new AssignRequestValidator().ValidateAsync(new AssignRequest(Guid.NewGuid(), many))).Errors.Single().ErrorCode.ShouldBe("partner_ids.too_many");

        (await new CancelRequestByStaffValidator().ValidateAsync(new CancelRequestByStaff(Guid.NewGuid(), " "))).Errors.Single().ErrorCode.ShouldBe("reason.required");
        (await new CancelRequestByStaffValidator().ValidateAsync(new CancelRequestByStaff(Guid.NewGuid(), new string('r', 501)))).Errors.Single().ErrorCode
            .ShouldBe("reason.too_long");
    }
}
