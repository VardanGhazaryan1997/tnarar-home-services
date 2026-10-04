using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using HomeServices.Api.IntegrationTests.Infrastructure;
using HomeServices.Application.Common;
using HomeServices.Application.Identity;
using HomeServices.Application.Offers;
using HomeServices.Application.Orders;
using HomeServices.Application.Requests;
using HomeServices.Domain.Catalog;
using HomeServices.Domain.Files;
using HomeServices.Domain.Identity;
using HomeServices.Domain.Localization;
using HomeServices.Domain.Partners;
using HomeServices.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HomeServices.Api.IntegrationTests;

[Collection(ApiCollection.Name)]
public class OffersEndpointTests(ApiFactory factory) : IAsyncLifetime
{
    private readonly List<Guid> _categoryIds = [];
    private readonly List<Guid> _cityIds = [];

    private static async Task<string?> CodeAsync(HttpResponseMessage response) =>
        (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("code").GetString();

    private HttpClient ClientFor(User user)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer", factory.Services.GetRequiredService<ITokenService>().CreateAccessToken(user).Token);
        return client;
    }

    private static User NewUser() => User.Register(PhoneNumber.Parse($"+37495{Random.Shared.Next(100_000, 999_999)}"));

    /// <summary>
    /// A category and city only this test uses, a customer with an open request, and two approved partners who received it.
    /// </summary>
    private async Task<(Guid RequestId, HttpClient Customer, HttpClient Aram, HttpClient Gor, HttpClient Stranger)> GivenRequestAsync()
    {
        static LocalizedText Text(string hy) => LocalizedText.Empty.With("hy", hy);
        var category = Category.Create($"off-{Guid.NewGuid():N}"[..20], Text("Սանտեխնիկա"), 1);
        var city = City.Create($"city-{Guid.NewGuid():N}"[..20], Text("Քաղաք"), 1);
        var customer = NewUser();
        var stranger = NewUser();
        var entities = new List<object> { category, city, customer, stranger };
        var owners = new List<User>();
        foreach (var name in new[] { "Aram", "Gor" })
        {
            var owner = NewUser();
            var work = StoredFile.Begin(FileOwnerType.User, owner.Id.ToString(), "work.jpg", "image/jpeg", 100, DateTimeOffset.UtcNow);
            work.MarkReady(100, DateTimeOffset.UtcNow);
            var partner = PartnerProfile.Create(owner.Id, PartnerType.Specialist, name);
            partner.UpdateDetails(PartnerType.Specialist, name, new string('a', 60), null, null);
            partner.SetServices([category.Id]);
            partner.SetAreas([(city.Id, null)]);
            partner.AddMedia(PartnerMediaKind.WorkExample, work.Id, null);
            partner.Submit(DateTimeOffset.UtcNow);
            partner.Approve(DateTimeOffset.UtcNow);
            entities.AddRange([owner, work, partner]);
            owners.Add(owner);
        }

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.AddRange(entities);
            await db.SaveChangesAsync();
        }

        _categoryIds.Add(category.Id);
        _cityIds.Add(city.Id);

        var customerClient = ClientFor(customer);
        var created = await customerClient.PostAsJsonAsync("/api/v1/requests", new
        {
            kind = "Open",
            categoryId = category.Id,
            cityId = city.Id,
            description = "The kitchen tap is leaking, please help.",
            budgetMax = 30_000,
        });
        created.StatusCode.ShouldBe(HttpStatusCode.Created);
        var request = (await created.Content.ReadFromJsonAsync<MyRequestDto>())!;
        request.SentTo.ShouldBe(2);
        return (request.Id, customerClient, ClientFor(owners[0]), ClientFor(owners[1]), ClientFor(stranger));
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

    private static object WorkBody(int price) => new
    {
        kind = "Work",
        summary = "Replace the kitchen tap and the pipes under the sink.",
        lines = new[] { new { title = "Remove the old tap", included = true }, new { title = "Tiling", included = false } },
        price,
        materialsIncluded = true,
        durationDays = 2,
        stages = new[] { new { title = (string?)"Deposit", purpose = "Deposit", amount = price / 2 }, new { title = (string?)null, purpose = "Final", amount = price - (price / 2) } },
    };

    private static async Task<OfferDto> SendAsync(HttpClient partner, Guid requestId, object body)
    {
        var response = await partner.PostAsJsonAsync($"/api/v1/requests/{requestId}/offers", body);
        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        return (await response.Content.ReadFromJsonAsync<OfferDto>())!;
    }

    [Fact]
    public async Task Partners_offer_the_customer_compares_and_accepts_one()
    {
        var (requestId, customer, aram, gor, _) = await GivenRequestAsync();

        var inbox = await aram.GetFromJsonAsync<JsonElement>($"/api/v1/requests/inbox/{requestId}");
        inbox.TryGetProperty("budgetMax", out _).ShouldBeFalse();

        var aramsOffer = await SendAsync(aram, requestId, WorkBody(100_000));
        aramsOffer.Status.ShouldBe("Sent");
        aramsOffer.Stages.Select(s => s.Purpose).ShouldBe(new[] { "Deposit", "Final" });
        var gorsOffer = await SendAsync(gor, requestId, WorkBody(80_000));

        var offers = await customer.GetFromJsonAsync<List<OfferDto>>($"/api/v1/requests/{requestId}/offers");
        offers!.Select(o => o.Id).ShouldBe(new[] { gorsOffer.Id, aramsOffer.Id });
        (await aram.GetFromJsonAsync<List<OfferDto>>($"/api/v1/requests/{requestId}/offers"))!.ShouldHaveSingleItem().Id.ShouldBe(aramsOffer.Id);

        var accepted = await customer.PostAsync($"/api/v1/offers/{aramsOffer.Id}/accept", null);
        accepted.StatusCode.ShouldBe(HttpStatusCode.OK);
        var order = (await accepted.Content.ReadFromJsonAsync<OrderDto>())!;
        order.Kind.ShouldBe("Work");
        order.Status.ShouldBe("Confirmed");
        order.Terms.Lines.Count.ShouldBe(2);
        order.Partner.DisplayName.ShouldBe("Aram");

        var again = await customer.PostAsync($"/api/v1/offers/{aramsOffer.Id}/accept", null);
        (await again.Content.ReadFromJsonAsync<OrderDto>())!.Id.ShouldBe(order.Id);

        (await gor.GetFromJsonAsync<OfferDto>($"/api/v1/offers/{gorsOffer.Id}"))!.Status.ShouldBe("Closed");
        var asPartner = await aram.GetFromJsonAsync<OrderDto>($"/api/v1/orders/{order.Id}");
        asPartner!.MyRole.ShouldBe("Partner");
        asPartner.Customer.Phone.ShouldStartWith("+37495");
        (await aram.GetFromJsonAsync<PagedResult<OrderListItemDto>>("/api/v1/orders?as=Partner"))!.Items.ShouldHaveSingleItem().Id.ShouldBe(order.Id);
        (await aram.GetFromJsonAsync<PagedResult<MyOfferListItemDto>>("/api/v1/offers/mine?status=Accepted"))!.Items.ShouldHaveSingleItem().OrderId.ShouldBe(order.Id);
    }

    [Fact]
    public async Task A_visit_is_offered_rejected_and_withdrawn_offers_stay_private()
    {
        var (requestId, customer, aram, gor, stranger) = await GivenRequestAsync();
        var visit = await SendAsync(aram, requestId, new
        {
            kind = "Visit",
            summary = "I'll come and measure the bathroom.",
            price = 0,
            visitAt = DateTimeOffset.UtcNow.AddDays(1),
            validDays = 3,
        });
        visit.Kind.ShouldBe("Visit");
        var gorsOffer = await SendAsync(gor, requestId, WorkBody(90_000));

        var rejected = await customer.PostAsJsonAsync($"/api/v1/offers/{gorsOffer.Id}/reject", new { reason = "Too expensive" });
        rejected.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await gor.GetFromJsonAsync<OfferDto>($"/api/v1/offers/{gorsOffer.Id}"))!.RejectReason.ShouldBe("Too expensive");

        var withdrawn = await aram.PostAsync($"/api/v1/offers/{visit.Id}/withdraw", null);
        (await withdrawn.Content.ReadFromJsonAsync<OfferDto>())!.Status.ShouldBe("Withdrawn");
        (await customer.GetFromJsonAsync<List<OfferDto>>($"/api/v1/requests/{requestId}/offers"))!.ShouldHaveSingleItem().Id.ShouldBe(gorsOffer.Id);

        var hidden = await stranger.GetAsync($"/api/v1/offers/{gorsOffer.Id}");
        hidden.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await CodeAsync(hidden)).ShouldBe("offer.not_found");
        (await stranger.GetAsync($"/api/v1/requests/{requestId}/offers")).StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await stranger.PostAsync($"/api/v1/offers/{gorsOffer.Id}/accept", null)).StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await factory.CreateClient().GetAsync("/api/v1/orders")).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Invalid_offers_get_field_errors_and_rule_breaks_get_codes()
    {
        var (requestId, customer, aram, _, _) = await GivenRequestAsync();

        var invalid = await aram.PostAsJsonAsync($"/api/v1/requests/{requestId}/offers", new
        {
            kind = "Work",
            summary = "short",
            price = 100,
            stages = new[] { new { purpose = "Deposit", amount = 10 } },
        });
        invalid.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var errors = (await invalid.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("errors");
        errors.GetProperty("summary")[0].GetString().ShouldBe("summary.length");
        errors.GetProperty("stages")[0].GetString().ShouldBe("stages.sum_mismatch");

        await SendAsync(aram, requestId, WorkBody(100_000));
        var twice = await aram.PostAsJsonAsync($"/api/v1/requests/{requestId}/offers", WorkBody(90_000));
        twice.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
        (await CodeAsync(twice)).ShouldBe("offer.already_sent");

        var notAPartner = await customer.PostAsJsonAsync($"/api/v1/requests/{requestId}/offers", WorkBody(100_000));
        notAPartner.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await CodeAsync(notAPartner)).ShouldBe("partner.not_found");
    }
}
