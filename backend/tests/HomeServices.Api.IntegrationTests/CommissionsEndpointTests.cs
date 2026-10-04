using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using HomeServices.Api.Commissions;
using HomeServices.Api.IntegrationTests.Infrastructure;
using HomeServices.Application.Commissions;
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
public class CommissionsEndpointTests(ApiFactory factory) : IAsyncLifetime
{
    private readonly List<Guid> _categoryIds = [];
    private readonly List<Guid> _cityIds = [];

    private static User NewUser() => User.Register(PhoneNumber.Parse($"+37495{Random.Shared.Next(100_000, 999_999)}"));

    private HttpClient ClientFor(User user)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer", factory.Services.GetRequiredService<ITokenService>().CreateAccessToken(user).Token);
        return client;
    }

    private HttpClient StaffClient(params string[] permissions)
    {
        var staff = StaffUser.Create($"{Guid.NewGuid():N}@example.com", "Ani Finance", "hash");
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

    private static async Task<T> OkAsync<T>(Task<HttpResponseMessage> call)
    {
        var response = await call;
        response.StatusCode.ShouldBe(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        return (await response.Content.ReadFromJsonAsync<T>())!;
    }

    [Fact]
    public async Task Completed_orders_are_charged_billed_weekly_and_settled_by_staff()
    {
        static LocalizedText Text(string hy) => LocalizedText.Empty.With("hy", hy);
        var category = Category.Create($"com-{Guid.NewGuid():N}"[..20], Text("Սանտեխնիկա"), 1);
        var city = City.Create($"city-{Guid.NewGuid():N}"[..20], Text("Քաղաք"), 1);
        var customer = NewUser();
        var stranger = NewUser();
        var owner = NewUser();
        var work = StoredFile.Begin(FileOwnerType.User, owner.Id.ToString(), "work.jpg", "image/jpeg", 100, DateTimeOffset.UtcNow);
        work.MarkReady(100, DateTimeOffset.UtcNow);
        var name = $"Aram {Guid.NewGuid():N}"[..16];
        var profile = PartnerProfile.Create(owner.Id, PartnerType.Specialist, name);
        profile.UpdateDetails(PartnerType.Specialist, name, new string('a', 60), null, null);
        profile.SetServices([category.Id]);
        profile.SetAreas([(city.Id, null)]);
        profile.AddMedia(PartnerMediaKind.WorkExample, work.Id, null);
        profile.Submit(DateTimeOffset.UtcNow);
        profile.Approve(DateTimeOffset.UtcNow);
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.AddRange(category, city, customer, stranger, owner, work, profile);
            await db.SaveChangesAsync();
        }

        _categoryIds.Add(category.Id);
        _cityIds.Add(city.Id);

        // Staff give the category its own rate.
        (await StaffClient(Permissions.CommissionsView).PutAsJsonAsync($"/api/v1/admin/commission-rates/categories/{category.Id}", new { percent = 12.5 }))
            .StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        var finance = StaffClient(Permissions.CommissionsView, Permissions.CommissionsManage);
        var invalid = await finance.PutAsJsonAsync($"/api/v1/admin/commission-rates/categories/{category.Id}", new { percent = 60 });
        invalid.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var rates = await OkAsync<CommissionRatesDto>(finance.PutAsJsonAsync($"/api/v1/admin/commission-rates/categories/{category.Id}", new { percent = 12.5 }));
        rates.Categories.Single(c => c.CategoryId == category.Id).EffectivePercent.ShouldBe(12.5m);
        (await finance.PutAsJsonAsync($"/api/v1/admin/commission-rates/categories/{Guid.NewGuid()}", new { percent = 5 })).StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await finance.GetFromJsonAsync<CommissionRatesDto>("/api/v1/admin/commission-rates"))!.DefaultPercent.ShouldBeInRange(0m, 50m);

        // An order is agreed and completed.
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
        var sent = await partnerClient.PostAsJsonAsync($"/api/v1/requests/{request.Id}/offers", new
        {
            kind = "Work",
            summary = "Replace the kitchen tap and the pipes under the sink.",
            lines = new[] { new { title = "Remove the old tap", included = true } },
            price = 100_000,
            startDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(3)),
            durationDays = 2,
            stages = new[] { new { purpose = "Deposit", amount = 40_000 }, new { purpose = "Final", amount = 60_000 } },
        });
        var offer = (await sent.Content.ReadFromJsonAsync<OfferDto>())!;
        var order = await OkAsync<OrderDto>(customerClient.PostAsync($"/api/v1/offers/{offer.Id}/accept", null));
        await OkAsync<OrderDto>(partnerClient.PostAsync($"/api/v1/orders/{order.Id}/request-completion", null));
        await OkAsync<OrderDto>(customerClient.PostAsync($"/api/v1/orders/{order.Id}/confirm-completion", null));

        var lines = await OkAsync<PagedResult<CommissionLineDto>>(partnerClient.GetAsync("/api/v1/me/commissions?unbilled=true"));
        var line = lines.Items.ShouldHaveSingleItem();
        line.OrderId.ShouldBe(order.Id);
        line.RatePercent.ShouldBe(12.5m);
        line.Amount.ShouldBe(12_500);
        (await OkAsync<CommissionSummaryDto>(partnerClient.GetAsync("/api/v1/me/commissions/summary"))).Unbilled.ShouldBe(12_500);
        (await ClientFor(stranger).GetAsync("/api/v1/me/commissions/summary")).StatusCode.ShouldBe(HttpStatusCode.NotFound);

        // Pretend it completed last week; the job issues the statement.
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await db.CommissionObligations.Where(o => o.OrderId == order.Id)
                .ExecuteUpdateAsync(s => s.SetProperty(o => o.CompletedAt, DateTimeOffset.UtcNow.AddDays(-8)));
        }

        (await factory.Services.GetRequiredService<CommissionJobService>().RunOnceAsync(CancellationToken.None)).ShouldNotBeNull();

        var statements = await OkAsync<PagedResult<StatementDto>>(partnerClient.GetAsync("/api/v1/me/commission-statements"));
        var statement = statements.Items.ShouldHaveSingleItem();
        statement.Total.ShouldBe(12_500);
        statement.Status.ShouldBe("Open");
        var mine = await OkAsync<StatementDetailDto>(partnerClient.GetAsync($"/api/v1/me/commission-statements/{statement.Id}"));
        mine.Lines.ShouldHaveSingleItem().OrderId.ShouldBe(order.Id);
        (await ClientFor(customer).GetAsync($"/api/v1/me/commission-statements/{statement.Id}")).StatusCode.ShouldBe(HttpStatusCode.NotFound);

        // Staff find it and record the payment.
        (await StaffClient().GetAsync("/api/v1/admin/commission-statements")).StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        var listed = await OkAsync<PagedResult<AdminStatementDto>>(finance.GetAsync($"/api/v1/admin/commission-statements?filter=Open&partnerId={profile.Id}"));
        listed.Items.ShouldHaveSingleItem().PartnerName.ShouldBe(name);
        (await finance.GetAsync($"/api/v1/admin/commission-statements/{statement.Id}")).StatusCode.ShouldBe(HttpStatusCode.OK);

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        (await StaffClient(Permissions.CommissionsView).PostAsJsonAsync($"/api/v1/admin/commission-statements/{statement.Id}/settlements", new { amount = 12_500, method = "Cash", paidOn = today }))
            .StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        (await finance.PostAsJsonAsync($"/api/v1/admin/commission-statements/{statement.Id}/settlements", new { amount = 0, method = "Cash", paidOn = today }))
            .StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var tooMuch = await finance.PostAsJsonAsync($"/api/v1/admin/commission-statements/{statement.Id}/settlements", new { amount = 20_000, method = "Cash", paidOn = today });
        tooMuch.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
        (await tooMuch.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("code").GetString().ShouldBe("commission.settlement_amount_invalid");

        var settled = await OkAsync<AdminStatementDetailDto>(finance.PostAsJsonAsync(
            $"/api/v1/admin/commission-statements/{statement.Id}/settlements", new { amount = 12_500, method = "BankTransfer", paidOn = today, reference = "TX-9" }));
        settled.Summary.Statement.Status.ShouldBe("Paid");
        settled.Settlements.ShouldHaveSingleItem().Method.ShouldBe("BankTransfer");
        (await OkAsync<PagedResult<AdminStatementDto>>(finance.GetAsync($"/api/v1/admin/commission-statements?filter=Paid&partnerId={profile.Id}"))).Items.ShouldHaveSingleItem();

        // Removing the category's own rate falls back to the default.
        var cleared = await OkAsync<CommissionRatesDto>(finance.DeleteAsync($"/api/v1/admin/commission-rates/categories/{category.Id}"));
        cleared.Categories.Single(c => c.CategoryId == category.Id).Percent.ShouldBeNull();
    }
}
