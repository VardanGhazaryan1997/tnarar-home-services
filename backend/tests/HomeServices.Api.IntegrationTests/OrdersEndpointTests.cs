using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using HomeServices.Api.IntegrationTests.Infrastructure;
using HomeServices.Api.Orders;
using HomeServices.Application.Common;
using HomeServices.Application.Identity;
using HomeServices.Application.Offers;
using HomeServices.Application.Orders;
using HomeServices.Application.Requests;
using HomeServices.Application.Staff;
using HomeServices.Domain.Catalog;
using HomeServices.Domain.Files;
using HomeServices.Domain.Identity;
using HomeServices.Domain.Localization;
using HomeServices.Domain.Partners;
using HomeServices.Domain.Staff;
using HomeServices.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HomeServices.Api.IntegrationTests;

[Collection(ApiCollection.Name)]
public class OrdersEndpointTests(ApiFactory factory) : IAsyncLifetime
{
    private readonly List<Guid> _categoryIds = [];
    private readonly List<Guid> _cityIds = [];

    private static async Task<string?> CodeAsync(HttpResponseMessage response) =>
        (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("code").GetString();

    private static User NewUser() => User.Register(PhoneNumber.Parse($"+37494{Random.Shared.Next(100_000, 999_999)}"));

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

    public Task InitializeAsync() => Task.CompletedTask;

    /// <summary>The database is shared: hide this test's category and city so catalog tests still see only the seeded ones.</summary>
    public async Task DisposeAsync()
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Categories.IgnoreQueryFilters().Where(c => _categoryIds.Contains(c.Id)).ExecuteUpdateAsync(s => s.SetProperty(c => c.IsActive, false));
        await db.Cities.IgnoreQueryFilters().Where(c => _cityIds.Contains(c.Id)).ExecuteUpdateAsync(s => s.SetProperty(c => c.IsActive, false));
    }

    /// <summary>A customer's request, an approved partner's work offer, accepted: the order and both parties' clients.</summary>
    private async Task<(OrderDto Order, HttpClient Customer, HttpClient Partner, HttpClient Stranger, string PartnerName)> GivenOrderAsync(string kind = "Work")
    {
        static LocalizedText Text(string hy) => LocalizedText.Empty.With("hy", hy);
        var category = Category.Create($"ord-{Guid.NewGuid():N}"[..20], Text("Սանտեխնիկա"), 1);
        var city = City.Create($"city-{Guid.NewGuid():N}"[..20], Text("Քաղաք"), 1);
        var customer = NewUser();
        var stranger = NewUser();
        var owner = NewUser();
        var work = StoredFile.Begin(FileOwnerType.User, owner.Id.ToString(), "work.jpg", "image/jpeg", 100, DateTimeOffset.UtcNow);
        work.MarkReady(100, DateTimeOffset.UtcNow);
        var name = $"Aram {Guid.NewGuid():N}"[..16];
        var partner = PartnerProfile.Create(owner.Id, PartnerType.Specialist, name);
        partner.UpdateDetails(PartnerType.Specialist, name, new string('a', 60), null, null);
        partner.SetServices([category.Id]);
        partner.SetAreas([(city.Id, null)]);
        partner.AddMedia(PartnerMediaKind.WorkExample, work.Id, null);
        partner.Submit(DateTimeOffset.UtcNow);
        partner.Approve(DateTimeOffset.UtcNow);

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.AddRange(category, city, customer, stranger, owner, work, partner);
            await db.SaveChangesAsync();
        }

        _categoryIds.Add(category.Id);
        _cityIds.Add(city.Id);

        var customerClient = ClientFor(customer);
        var partnerClient = ClientFor(owner);
        var created = await customerClient.PostAsJsonAsync("/api/v1/requests", new
        {
            kind = "Open",
            categoryId = category.Id,
            cityId = city.Id,
            description = "The kitchen tap is leaking, please help.",
        });
        var request = (await created.Content.ReadFromJsonAsync<MyRequestDto>())!;
        object body = kind == "Work"
            ? new
            {
                kind = "Work",
                summary = "Replace the kitchen tap and the pipes under the sink.",
                lines = new[] { new { title = "Remove the old tap", included = true } },
                price = 100_000,
                startDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(3)),
                durationDays = 2,
                stages = new[] { new { purpose = "Deposit", amount = 40_000 }, new { purpose = "Final", amount = 60_000 } },
            }
            : new { kind = "Visit", summary = "I'll come and look at the pipes first.", price = 0, visitAt = DateTimeOffset.UtcNow.AddDays(1) };
        var sent = await partnerClient.PostAsJsonAsync($"/api/v1/requests/{request.Id}/offers", body);
        sent.StatusCode.ShouldBe(HttpStatusCode.Created);
        var offer = (await sent.Content.ReadFromJsonAsync<OfferDto>())!;
        var accepted = await customerClient.PostAsync($"/api/v1/offers/{offer.Id}/accept", null);
        return ((await accepted.Content.ReadFromJsonAsync<OrderDto>())!, customerClient, partnerClient, ClientFor(stranger), name);
    }

    private static async Task<OrderDto> OkAsync(Task<HttpResponseMessage> call)
    {
        var response = await call;
        response.StatusCode.ShouldBe(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        return (await response.Content.ReadFromJsonAsync<OrderDto>())!;
    }

    [Fact]
    public async Task An_order_is_started_marked_done_sent_back_and_confirmed()
    {
        var (order, customer, partner, stranger, _) = await GivenOrderAsync();
        var url = $"/api/v1/orders/{order.Id}";
        order.Actions.ShouldBe(new[] { "proposeChange", "cancel", "recordPayment" });

        var started = await OkAsync(partner.PostAsync($"{url}/start", null));
        started.Status.ShouldBe("InProgress");
        var wrongSide = await customer.PostAsync($"{url}/start", null);
        wrongSide.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
        (await CodeAsync(wrongSide)).ShouldBe("order.wrong_party");
        (await stranger.PostAsync($"{url}/start", null)).StatusCode.ShouldBe(HttpStatusCode.NotFound);

        var done = await OkAsync(partner.PostAsync($"{url}/request-completion", null));
        done.Status.ShouldBe("CompletionRequested");
        done.AutoCompleteAt.ShouldNotBeNull();

        var noReason = await customer.PostAsJsonAsync($"{url}/reject-completion", new { reason = "" });
        noReason.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await noReason.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("errors").GetProperty("reason")[0].GetString().ShouldBe("reason.required");
        var back = await OkAsync(customer.PostAsJsonAsync($"{url}/reject-completion", new { reason = "The tap still drips" }));
        back.Status.ShouldBe("InProgress");
        back.History[^1].Note.ShouldBe("The tap still drips");

        await OkAsync(partner.PostAsync($"{url}/request-completion", null));
        var completed = await OkAsync(customer.PostAsync($"{url}/confirm-completion", null));
        completed.Status.ShouldBe("Completed");
        completed.History.Select(h => h.Status).ShouldBe(new[] { "Confirmed", "InProgress", "CompletionRequested", "InProgress", "CompletionRequested", "Completed" });

        var late = await customer.PostAsJsonAsync($"{url}/cancel", new { reason = "Too late" });
        (await CodeAsync(late)).ShouldBe("order.cannot_cancel");
    }

    [Fact]
    public async Task Extra_work_and_a_new_schedule_are_proposed_and_answered()
    {
        var (order, customer, partner, _, _) = await GivenOrderAsync();
        var url = $"/api/v1/orders/{order.Id}";

        var proposed = await OkAsync(partner.PostAsJsonAsync($"{url}/change-requests", new
        {
            kind = "ExtraWork",
            title = "Replace the siphon",
            description = "It is cracked.",
            amount = 15_000,
        }));
        var change = proposed.ChangeRequests.ShouldHaveSingleItem();
        change.Mine.ShouldBeTrue();
        var second = await customer.PostAsJsonAsync($"{url}/change-requests", new { kind = "Schedule", newDurationDays = 3 });
        (await CodeAsync(second)).ShouldBe("change.pending_exists");

        var accepted = await OkAsync(customer.PostAsync($"{url}/change-requests/{change.Id}/accept", null));
        accepted.Price.ShouldBe(115_000);
        accepted.Stages.Select(s => s.Purpose).ShouldBe(new[] { "Deposit", "Stage", "Final" });

        var invalid = await customer.PostAsJsonAsync($"{url}/change-requests", new { kind = "ExtraWork", amount = 0 });
        invalid.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var errors = (await invalid.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("errors");
        errors.GetProperty("title")[0].GetString().ShouldBe("title.required");
        errors.GetProperty("amount")[0].GetString().ShouldBe("amount.invalid");

        var later = await OkAsync(customer.PostAsJsonAsync($"{url}/change-requests", new { kind = "Schedule", newDurationDays = 3, description = "Weekends only" }));
        var rejected = await OkAsync(partner.PostAsJsonAsync($"{url}/change-requests/{later.ChangeRequests[0].Id}/reject", new { note = "Can't do weekends" }));
        rejected.ChangeRequests[0].Status.ShouldBe("Rejected");
        rejected.DurationDays.ShouldBe(2);

        var again = await OkAsync(customer.PostAsJsonAsync($"{url}/change-requests", new { kind = "Schedule", newDurationDays = 4 }));
        var pending = again.ChangeRequests.Single(c => c.Status == "Pending");
        (await CodeAsync(await partner.PostAsync($"{url}/change-requests/{pending.Id}/withdraw", null))).ShouldBe("change.wrong_party");
        var withdrawn = await OkAsync(customer.PostAsync($"{url}/change-requests/{pending.Id}/withdraw", null));
        withdrawn.ChangeRequests.Single(c => c.Id == pending.Id).Status.ShouldBe("Withdrawn");
    }

    [Fact]
    public async Task A_visit_can_be_moved_and_cancelling_after_it_started_flags_it_for_staff()
    {
        var (order, customer, partner, _, partnerName) = await GivenOrderAsync("Visit");
        var url = $"/api/v1/orders/{order.Id}";
        var newTime = DateTimeOffset.UtcNow.AddDays(2);

        var moved = await OkAsync(customer.PostAsJsonAsync($"{url}/change-requests", new { kind = "Schedule", newVisitAt = newTime }));
        var visit = await OkAsync(partner.PostAsync($"{url}/change-requests/{moved.ChangeRequests[0].Id}/accept", null));
        visit.VisitAt!.Value.ToUnixTimeSeconds().ShouldBe(newTime.ToUnixTimeSeconds());

        await OkAsync(partner.PostAsync($"{url}/start", null));
        var cancelled = await OkAsync(partner.PostAsJsonAsync($"{url}/cancel", new { reason = "The customer wasn't home" }));
        cancelled.CancelledBy.ShouldBe("Partner");
        cancelled.NeedsAttentionSince.ShouldNotBeNull();

        var viewer = StaffClient(Permissions.OrdersView);
        var queue = await viewer.GetFromJsonAsync<PagedResult<AdminOrderListItemDto>>($"/api/v1/admin/orders?needsAttention=true&search={Uri.EscapeDataString(partnerName)}");
        queue!.Items.ShouldHaveSingleItem().Id.ShouldBe(order.Id);
        var detail = await viewer.GetFromJsonAsync<OrderDto>($"/api/v1/admin/orders/{order.Id}");
        detail!.MyRole.ShouldBe("Staff");
        detail.Customer.Phone.ShouldStartWith("+37494");
        (await viewer.PostAsync($"/api/v1/admin/orders/{order.Id}/resolve", null)).StatusCode.ShouldBe(HttpStatusCode.Forbidden);

        var manager = StaffClient(Permissions.OrdersView, Permissions.OrdersManage);
        var resolved = await OkAsync(manager.PostAsync($"/api/v1/admin/orders/{order.Id}/resolve", null));
        resolved.NeedsAttentionSince.ShouldBeNull();
        (await StaffClient(Permissions.RequestsView).GetAsync("/api/v1/admin/orders")).StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Staff_cancel_open_orders_and_the_auto_complete_pass_runs()
    {
        var (order, _, partner, _, _) = await GivenOrderAsync();
        var manager = StaffClient(Permissions.OrdersView, Permissions.OrdersManage);

        var noReason = await manager.PostAsJsonAsync($"/api/v1/admin/orders/{order.Id}/cancel", new { reason = " " });
        noReason.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var cancelled = await OkAsync(manager.PostAsJsonAsync($"/api/v1/admin/orders/{order.Id}/cancel", new { reason = "Duplicate order" }));
        cancelled.CancelledBy.ShouldBe("Staff");
        cancelled.NeedsAttentionSince.ShouldBeNull();
        (await CodeAsync(await partner.PostAsync($"/api/v1/orders/{order.Id}/start", null))).ShouldBe("order.cannot_start");
        (await manager.GetAsync($"/api/v1/admin/orders/{Guid.NewGuid()}")).StatusCode.ShouldBe(HttpStatusCode.NotFound);

        (await factory.Services.GetRequiredService<OrderAutoCompleteService>().RunOnceAsync(CancellationToken.None)).ShouldBeGreaterThanOrEqualTo(0);
        (await factory.CreateClient().PostAsync($"/api/v1/orders/{order.Id}/start", null)).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }
}
