using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using HomeServices.Api.IntegrationTests.Infrastructure;
using HomeServices.Api.Requests;
using HomeServices.Application.Common;
using HomeServices.Application.Identity;
using HomeServices.Application.Requests;
using HomeServices.Application.Staff;
using HomeServices.Domain.Catalog;
using HomeServices.Domain.Files;
using HomeServices.Domain.Identity;
using HomeServices.Domain.Localization;
using HomeServices.Domain.Partners;
using HomeServices.Domain.Requests;
using HomeServices.Domain.Staff;
using HomeServices.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HomeServices.Api.IntegrationTests;

[Collection(ApiCollection.Name)]
public class RequestsEndpointTests(ApiFactory factory) : IAsyncLifetime
{
    private readonly List<Guid> _categoryIds = [];
    private readonly List<Guid> _cityIds = [];
    private const string Url = "/api/v1/requests";
    private const string AdminUrl = "/api/v1/admin/requests";

    private static async Task<string?> CodeAsync(HttpResponseMessage response) =>
        (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("code").GetString();

    private HttpClient ClientFor(User user)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer", factory.Services.GetRequiredService<ITokenService>().CreateAccessToken(user).Token);
        return client;
    }

    private HttpClient StaffClient(params string[] permissions)
    {
        var staff = StaffUser.Create($"{Guid.NewGuid():N}@example.com", "Ani Operator", "hash");
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer", factory.Services.GetRequiredService<IStaffTokenService>().CreateAccessToken(staff, permissions).Token);
        return client;
    }

    /// <summary>
    /// A category and city only this test uses (so partners from other tests don't match), a customer and
    /// an approved partner offering the category in the whole city.
    /// </summary>
    private async Task<(Guid CategoryId, Guid CityId, User Customer, User PartnerOwner, PartnerProfile Partner)> GivenMarketAsync()
    {
        static LocalizedText Text(string hy) => LocalizedText.Empty.With("hy", hy);
        var category = Category.Create($"req-{Guid.NewGuid():N}"[..20], Text("Սանտեխնիկա"), 1);
        var city = City.Create($"city-{Guid.NewGuid():N}"[..20], Text("Քաղաք"), 1);
        var customer = User.Register(PhoneNumber.Parse($"+37496{Random.Shared.Next(100_000, 999_999)}"));
        customer.UpdateProfile("Ani Petrosyan", null);
        var owner = User.Register(PhoneNumber.Parse($"+37496{Random.Shared.Next(100_000, 999_999)}"));
        var work = StoredFile.Begin(FileOwnerType.User, owner.Id.ToString(), "work.jpg", "image/jpeg", 100, DateTimeOffset.UtcNow);
        work.MarkReady(100, DateTimeOffset.UtcNow);
        var partner = PartnerProfile.Create(owner.Id, PartnerType.Specialist, "Aram Plumbing");
        partner.UpdateDetails(PartnerType.Specialist, "Aram Plumbing", new string('a', 60), null, null);
        partner.SetServices([category.Id]);
        partner.SetAreas([(city.Id, null)]);
        partner.AddMedia(PartnerMediaKind.WorkExample, work.Id, null);
        partner.Submit(DateTimeOffset.UtcNow);
        partner.Approve(DateTimeOffset.UtcNow);

        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.AddRange(category, city, customer, owner, work, partner);
        await db.SaveChangesAsync();
        _categoryIds.Add(category.Id);
        _cityIds.Add(city.Id);
        return (category.Id, city.Id, customer, owner, partner);
    }

    public Task InitializeAsync() => Task.CompletedTask;

    /// <summary>The database is shared: hide this test's category and city so catalog tests still see only the seeded ones.</summary>
    public async Task DisposeAsync()
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Categories.IgnoreQueryFilters().Where(c => _categoryIds.Contains(c.Id)).ExecuteUpdateAsync(s => s.SetProperty(c => c.IsActive, false));
        await db.Cities.IgnoreQueryFilters().Where(c => _cityIds.Contains(c.Id)).ExecuteUpdateAsync(s => s.SetProperty(c => c.IsActive, false));
    }

    private static object OpenBody(Guid categoryId, Guid cityId) => new
    {
        kind = "Open",
        categoryId,
        cityId,
        description = "The kitchen tap is leaking, please help.",
        preferredDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(2)),
        budgetMax = 30_000,
    };

    [Fact]
    public async Task A_customer_sends_an_open_request_and_the_matching_partner_works_on_it()
    {
        var (categoryId, cityId, customer, owner, _) = await GivenMarketAsync();
        var customerClient = ClientFor(customer);
        var partnerClient = ClientFor(owner);

        var created = await customerClient.PostAsJsonAsync(Url, OpenBody(categoryId, cityId));
        created.StatusCode.ShouldBe(HttpStatusCode.Created);
        var request = (await created.Content.ReadFromJsonAsync<MyRequestDto>())!;
        request.SentTo.ShouldBe(1);
        request.Place.CategoryName.ShouldBe("Սանտեխնիկա");

        var mine = await customerClient.GetFromJsonAsync<PagedResult<MyRequestListItemDto>>($"{Url}/mine?status=Open");
        mine!.Items.ShouldHaveSingleItem().Id.ShouldBe(request.Id);
        (await customerClient.GetFromJsonAsync<MyRequestDto>($"{Url}/{request.Id}"))!.Description.ShouldBe("The kitchen tap is leaking, please help.");

        var inbox = await partnerClient.GetFromJsonAsync<PagedResult<InboxItemDto>>($"{Url}/inbox?status=New");
        inbox!.Items.ShouldContain(i => i.Id == request.Id);
        var opened = await partnerClient.GetFromJsonAsync<InboxRequestDto>($"{Url}/inbox/{request.Id}");
        opened!.MyStatus.ShouldBe("Viewed");
        opened.CustomerFirstName.ShouldBe("Ani");

        var declined = await partnerClient.PostAsJsonAsync($"{Url}/inbox/{request.Id}/decline", new { reason = "Booked this week" });
        declined.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await declined.Content.ReadFromJsonAsync<InboxRequestDto>())!.MyStatus.ShouldBe("Declined");

        // Everyone declined: the operator queue has it.
        var queue = await StaffClient(Permissions.RequestsView).GetFromJsonAsync<PagedResult<AdminRequestListItemDto>>(
            $"{AdminUrl}?needsAttention=true&categoryId={categoryId}");
        queue!.Items.ShouldHaveSingleItem().AttentionReason.ShouldBe("AllDeclined");

        var cancelled = await customerClient.PostAsJsonAsync($"{Url}/{request.Id}/cancel", new { reason = "Fixed it" });
        cancelled.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await cancelled.Content.ReadFromJsonAsync<MyRequestDto>())!.Status.ShouldBe("Cancelled");
    }

    [Fact]
    public async Task Invalid_requests_get_field_errors()
    {
        var (_, cityId, customer, _, _) = await GivenMarketAsync();

        var response = await ClientFor(customer).PostAsJsonAsync(Url, new { kind = "Direct", categoryId = Guid.NewGuid(), cityId, description = "short" });

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var errors = (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("errors");
        errors.GetProperty("partnerId")[0].GetString().ShouldBe("partner_id.required");
        errors.GetProperty("categoryId")[0].GetString().ShouldBe("category.invalid");
        errors.GetProperty("description")[0].GetString().ShouldBe("description.length");
    }

    [Fact]
    public async Task A_direct_request_to_an_unavailable_partner_is_refused()
    {
        var (categoryId, cityId, customer, _, _) = await GivenMarketAsync();

        var response = await ClientFor(customer).PostAsJsonAsync(Url, new { kind = "Direct", partnerId = Guid.NewGuid(), categoryId, cityId, description = "The kitchen tap is leaking, please help." });

        response.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
        (await CodeAsync(response)).ShouldBe("request.partner_unavailable");
    }

    [Fact]
    public async Task Requests_are_private_to_their_customer_and_recipients()
    {
        var (categoryId, cityId, customer, _, _) = await GivenMarketAsync();
        var stranger = User.Register(PhoneNumber.Parse($"+37496{Random.Shared.Next(100_000, 999_999)}"));
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Users.Add(stranger);
            await db.SaveChangesAsync();
        }

        var request = (await (await ClientFor(customer).PostAsJsonAsync(Url, OpenBody(categoryId, cityId))).Content.ReadFromJsonAsync<MyRequestDto>())!;
        var strangerClient = ClientFor(stranger);

        (await strangerClient.GetAsync($"{Url}/{request.Id}")).StatusCode.ShouldBe(HttpStatusCode.NotFound);
        var inbox = await strangerClient.GetAsync($"{Url}/inbox");
        inbox.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await CodeAsync(inbox)).ShouldBe("partner.not_found");
        (await factory.CreateClient().GetAsync($"{Url}/mine")).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Operators_see_a_request_send_it_to_partners_and_cancel_it()
    {
        var (categoryId, cityId, customer, _, partner) = await GivenMarketAsync();
        var request = (await (await ClientFor(customer).PostAsJsonAsync(Url, OpenBody(categoryId, cityId))).Content.ReadFromJsonAsync<MyRequestDto>())!;
        var operatorClient = StaffClient(Permissions.RequestsView, Permissions.RequestsManage);

        var detail = await operatorClient.GetFromJsonAsync<AdminRequestDto>($"{AdminUrl}/{request.Id}");
        detail!.Customer.FullName.ShouldBe("Ani Petrosyan");
        detail.Recipients.ShouldHaveSingleItem().PartnerId.ShouldBe(partner.Id);

        var list = await operatorClient.GetFromJsonAsync<PagedResult<AdminRequestListItemDto>>($"{AdminUrl}?status=Open&kind=Open&cityId={cityId}&search={customer.Phone.Value}");
        list!.Items.ShouldHaveSingleItem().Id.ShouldBe(request.Id);

        var unavailable = await operatorClient.PostAsJsonAsync($"{AdminUrl}/{request.Id}/recipients", new { partnerIds = new[] { Guid.NewGuid() } });
        unavailable.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
        (await CodeAsync(unavailable)).ShouldBe("request.partner_unavailable");
        var again = await operatorClient.PostAsJsonAsync($"{AdminUrl}/{request.Id}/recipients", new { partnerIds = new[] { partner.Id } });
        again.StatusCode.ShouldBe(HttpStatusCode.OK);

        var cancelled = await operatorClient.PostAsJsonAsync($"{AdminUrl}/{request.Id}/cancel", new { reason = "Duplicate" });
        cancelled.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await cancelled.Content.ReadFromJsonAsync<AdminRequestDto>())!.CancelReason.ShouldBe("Duplicate");
    }

    [Fact]
    public async Task Request_screens_need_request_permissions()
    {
        var viewer = StaffClient(Permissions.RequestsView);

        (await StaffClient(Permissions.PartnersView).GetAsync(AdminUrl)).StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        (await viewer.PostAsJsonAsync($"{AdminUrl}/{Guid.NewGuid()}/cancel", new { reason = "x" })).StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        (await viewer.GetAsync($"{AdminUrl}/{Guid.NewGuid()}")).StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task The_follow_up_pass_moves_old_unanswered_requests_to_the_operator_queue()
    {
        var (categoryId, cityId, customer, _, partner) = await GivenMarketAsync();
        var request = ServiceRequest.Create(customer.Id, RequestKind.Open, categoryId, cityId, null, "Old request nobody answered yet.", null, null, null, null);
        request.SendTo([partner.Id], RecipientSource.Matched, DateTimeOffset.UtcNow.AddDays(-2));
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.ServiceRequests.Add(request);
            await db.SaveChangesAsync();
        }

        var flagged = await factory.Services.GetRequiredService<RequestFollowUpService>().RunOnceAsync(CancellationToken.None);

        flagged.ShouldBeGreaterThanOrEqualTo(1);
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            (await db.ServiceRequests.SingleAsync(r => r.Id == request.Id)).AttentionReason.ShouldBe(AttentionReason.NoResponse);
        }
    }
}
