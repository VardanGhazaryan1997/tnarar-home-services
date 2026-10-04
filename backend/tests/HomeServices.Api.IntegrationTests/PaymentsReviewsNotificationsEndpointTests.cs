using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using HomeServices.Api.IntegrationTests.Infrastructure;
using HomeServices.Application.Common;
using HomeServices.Application.Identity;
using HomeServices.Application.Notifications;
using HomeServices.Application.Offers;
using HomeServices.Application.Orders;
using HomeServices.Application.Partners;
using HomeServices.Application.Payments;
using HomeServices.Application.Requests;
using HomeServices.Application.Reviews;
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
public class PaymentsReviewsNotificationsEndpointTests(ApiFactory factory) : IAsyncLifetime
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

    private static object Payment(int amount) => new { amount, method = "Cash", paidOn = DateOnly.FromDateTime(DateTime.UtcNow) };

    [Fact]
    public async Task A_payment_is_recorded_by_one_side_and_confirmed_by_the_other()
    {
        var (order, customer, partner, stranger, _) = await GivenOrderAsync();

        var recorded = await OkAsync(customer.PostAsJsonAsync($"/api/v1/orders/{order.Id}/payments", new
        {
            amount = 40_000,
            method = "BankTransfer",
            paidOn = DateOnly.FromDateTime(DateTime.UtcNow),
            stageId = order.Stages[0].Id,
            note = "Deposit",
        }));
        var payment = recorded.Payments.ShouldHaveSingleItem();
        payment.Mine.ShouldBeTrue();
        payment.StageTitle.ShouldBeNull();
        payment.Status.ShouldBe("Pending");

        var tooMuch = await partner.PostAsJsonAsync($"/api/v1/orders/{order.Id}/payments", Payment(70_000));
        tooMuch.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
        (await CodeAsync(tooMuch)).ShouldBe("payment.exceeds_price");

        (await stranger.PostAsync($"/api/v1/payments/{payment.Id}/confirm", null)).StatusCode.ShouldBe(HttpStatusCode.NotFound);
        var own = await customer.PostAsync($"/api/v1/payments/{payment.Id}/confirm", null);
        (await CodeAsync(own)).ShouldBe("payment.wrong_party");

        var confirmed = await OkAsync(partner.PostAsync($"/api/v1/payments/{payment.Id}/confirm", null));
        confirmed.PaidAmount.ShouldBe(40_000);
        confirmed.Payments[0].Mine.ShouldBeFalse();
    }

    [Fact]
    public async Task Staff_see_disputes_and_decide_them()
    {
        var (order, customer, partner, _, partnerName) = await GivenOrderAsync();
        var payment = (await OkAsync(partner.PostAsJsonAsync($"/api/v1/orders/{order.Id}/payments", Payment(10_000)))).Payments[0];
        var empty = await customer.PostAsJsonAsync($"/api/v1/payments/{payment.Id}/dispute", new { reason = " " });
        empty.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        await OkAsync(customer.PostAsJsonAsync($"/api/v1/payments/{payment.Id}/dispute", new { reason = "I paid 5 000 only" }));

        (await StaffClient().GetAsync("/api/v1/admin/payments?status=Disputed")).StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        var staff = StaffClient(Permissions.PaymentsView, Permissions.PaymentsManage);
        var queue = await staff.GetFromJsonAsync<PagedResult<AdminPaymentDto>>($"/api/v1/admin/payments?status=Disputed&orderId={order.Id}");
        var row = queue!.Items.ShouldHaveSingleItem();
        row.PartnerName.ShouldBe(partnerName);
        row.DisputeReason.ShouldBe("I paid 5 000 only");

        var resolve = await staff.PostAsJsonAsync($"/api/v1/admin/payments/{row.Id}/resolve", new { counts = false, note = "Receipt shows 5 000" });
        resolve.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await resolve.Content.ReadFromJsonAsync<AdminPaymentDto>())!.Status.ShouldBe("Rejected");
        (await (await staff.PostAsJsonAsync($"/api/v1/admin/payments/{row.Id}/resolve", new { counts = true })).Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("code").GetString().ShouldBe("payment.not_disputed");
    }

    [Fact]
    public async Task A_completed_order_is_reviewed_shown_publicly_replied_to_and_moderated()
    {
        var (order, customer, partner, _, _) = await GivenOrderAsync();
        var early = await customer.PostAsJsonAsync($"/api/v1/orders/{order.Id}/review", new { rating = 5 });
        (await CodeAsync(early)).ShouldBe("review.order_not_completed");

        await OkAsync(partner.PostAsync($"/api/v1/orders/{order.Id}/request-completion", null));
        await OkAsync(customer.PostAsync($"/api/v1/orders/{order.Id}/confirm-completion", null));
        var invalid = await customer.PostAsJsonAsync($"/api/v1/orders/{order.Id}/review", new { rating = 6 });
        invalid.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var reviewed = await OkAsync(customer.PostAsJsonAsync($"/api/v1/orders/{order.Id}/review", new { rating = 5, text = "Fast and clean" }));
        reviewed.Review!.Rating.ShouldBe(5);

        var replied = await OkAsync(partner.PostAsJsonAsync($"/api/v1/orders/{order.Id}/review/reply", new { text = "Thanks!" }));
        replied.Review!.Reply.ShouldBe("Thanks!");

        var slug = order.Partner.Slug!;
        var profile = await factory.CreateClient().GetFromJsonAsync<PublicPartnerDto>($"/api/v1/partners/{slug}");
        profile!.Rating.ShouldBe(5.0);
        profile.ReviewCount.ShouldBe(1);
        var reviews = await factory.CreateClient().GetFromJsonAsync<PagedResult<PublicReviewDto>>($"/api/v1/partners/{slug}/reviews");
        reviews!.Items.ShouldHaveSingleItem().Reply.ShouldBe("Thanks!");

        var moderator = StaffClient(Permissions.ReviewsModerate);
        var hide = await moderator.PostAsJsonAsync($"/api/v1/admin/reviews/{reviewed.Review.Id}/hide", new { reason = "Mentions a price outside the platform" });
        hide.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await factory.CreateClient().GetFromJsonAsync<PagedResult<PublicReviewDto>>($"/api/v1/partners/{slug}/reviews"))!.Items.ShouldBeEmpty();
        var hidden = await moderator.GetFromJsonAsync<PagedResult<AdminReviewDto>>("/api/v1/admin/reviews?hidden=true&pageSize=100");
        hidden!.Items.ShouldContain(r => r.Id == reviewed.Review.Id);
        (await moderator.PostAsync($"/api/v1/admin/reviews/{reviewed.Review.Id}/restore", null)).StatusCode.ShouldBe(HttpStatusCode.OK);
        (await StaffClient(Permissions.OrdersView).GetAsync("/api/v1/admin/reviews")).StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Users_list_count_and_read_their_notifications()
    {
        var (order, _, partner, _, _) = await GivenOrderAsync();

        var list = await partner.GetFromJsonAsync<PagedResult<NotificationDto>>("/api/v1/notifications");
        list!.Items.Select(n => n.Type).ShouldBe(new[] { "OfferAccepted", "RequestReceived" });
        list.Items[0].Link.ShouldBe($"/orders/{order.Id}");
        (await partner.GetFromJsonAsync<JsonElement>("/api/v1/notifications/unread-count")).GetProperty("count").GetInt32().ShouldBe(2);

        var read = await partner.PostAsync($"/api/v1/notifications/{list.Items[1].Id}/read", null);
        (await read.Content.ReadFromJsonAsync<NotificationDto>())!.ReadAt.ShouldNotBeNull();
        (await partner.GetFromJsonAsync<PagedResult<NotificationDto>>("/api/v1/notifications?unreadOnly=true"))!.Items.ShouldHaveSingleItem().Type.ShouldBe("OfferAccepted");

        var all = await partner.PostAsync("/api/v1/notifications/read-all", null);
        (await all.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("count").GetInt32().ShouldBe(1);
        (await partner.GetFromJsonAsync<JsonElement>("/api/v1/notifications/unread-count")).GetProperty("count").GetInt32().ShouldBe(0);
        (await factory.CreateClient().GetAsync("/api/v1/notifications")).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }
}
