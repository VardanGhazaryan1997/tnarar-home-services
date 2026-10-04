using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using HomeServices.Api.Chat;
using HomeServices.Api.IntegrationTests.Infrastructure;
using HomeServices.Application.Chat;
using HomeServices.Application.Common;
using HomeServices.Application.Identity;
using HomeServices.Application.Requests;
using HomeServices.Domain.Catalog;
using HomeServices.Domain.Files;
using HomeServices.Domain.Identity;
using HomeServices.Domain.Localization;
using HomeServices.Domain.Partners;
using HomeServices.Infrastructure.Persistence;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace HomeServices.Api.IntegrationTests;

[Collection(ApiCollection.Name)]
public class ConversationsEndpointTests(ApiFactory factory) : IAsyncLifetime
{
    private readonly List<Guid> _categoryIds = [];
    private readonly List<Guid> _cityIds = [];

    private static async Task<string?> CodeAsync(HttpResponseMessage response) =>
        (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("code").GetString();

    private string TokenFor(User user) => factory.Services.GetRequiredService<ITokenService>().CreateAccessToken(user).Token;

    private HttpClient ClientFor(User user)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", TokenFor(user));
        return client;
    }

    private static User NewUser() => User.Register(PhoneNumber.Parse($"+37493{Random.Shared.Next(100_000, 999_999)}"));

    /// <summary>A customer's open request (in a category and city only this test uses) received by an approved partner.</summary>
    private async Task<(Guid RequestId, Guid PartnerId, User Customer, User Owner, User Stranger)> GivenRequestAsync()
    {
        static LocalizedText Text(string hy) => LocalizedText.Empty.With("hy", hy);
        var category = Category.Create($"chat-{Guid.NewGuid():N}"[..20], Text("Սանտեխնիկա"), 1);
        var city = City.Create($"city-{Guid.NewGuid():N}"[..20], Text("Քաղաք"), 1);
        var customer = NewUser();
        var owner = NewUser();
        var stranger = NewUser();
        var work = StoredFile.Begin(FileOwnerType.User, owner.Id.ToString(), "work.jpg", "image/jpeg", 100, DateTimeOffset.UtcNow);
        work.MarkReady(100, DateTimeOffset.UtcNow);
        var partner = PartnerProfile.Create(owner.Id, PartnerType.Specialist, "Aram");
        partner.UpdateDetails(PartnerType.Specialist, "Aram", new string('a', 60), null, null);
        partner.SetServices([category.Id]);
        partner.SetAreas([(city.Id, null)]);
        partner.AddMedia(PartnerMediaKind.WorkExample, work.Id, null);
        partner.Submit(DateTimeOffset.UtcNow);
        partner.Approve(DateTimeOffset.UtcNow);
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.AddRange(category, city, customer, owner, stranger, work, partner);
            await db.SaveChangesAsync();
        }

        _categoryIds.Add(category.Id);
        _cityIds.Add(city.Id);
        var created = await ClientFor(customer).PostAsJsonAsync("/api/v1/requests", new
        {
            kind = "Open",
            categoryId = category.Id,
            cityId = city.Id,
            description = "The kitchen tap is leaking, please help.",
        });
        var request = (await created.Content.ReadFromJsonAsync<MyRequestDto>())!;
        return (request.Id, partner.Id, customer, owner, stranger);
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

    [Fact]
    public async Task A_partner_and_a_customer_chat_about_a_request()
    {
        var (requestId, partnerId, customer, owner, stranger) = await GivenRequestAsync();
        var partnerClient = ClientFor(owner);
        var customerClient = ClientFor(customer);

        var opened = await partnerClient.PostAsync($"/api/v1/requests/{requestId}/conversation", null);
        opened.StatusCode.ShouldBe(HttpStatusCode.OK);
        var conversation = (await opened.Content.ReadFromJsonAsync<ConversationDto>())!;
        conversation.MyRole.ShouldBe("Partner");

        var sent = await partnerClient.PostAsJsonAsync($"/api/v1/conversations/{conversation.Id}/messages", new { body = "When can I come?" });
        sent.StatusCode.ShouldBe(HttpStatusCode.Created);

        var theirs = await customerClient.PostAsJsonAsync($"/api/v1/requests/{requestId}/conversation", new { partnerId });
        (await theirs.Content.ReadFromJsonAsync<ConversationDto>())!.Id.ShouldBe(conversation.Id);
        (await customerClient.GetFromJsonAsync<UnreadSummaryDto>("/api/v1/conversations/unread"))!.ShouldBe(new UnreadSummaryDto(1, 1));
        var page = await customerClient.GetFromJsonAsync<MessagePageDto>($"/api/v1/conversations/{conversation.Id}/messages?limit=10");
        page!.Items.ShouldHaveSingleItem().Body.ShouldBe("When can I come?");
        (await customerClient.GetFromJsonAsync<PagedResult<ConversationDto>>("/api/v1/conversations"))!.Items.ShouldHaveSingleItem().UnreadCount.ShouldBe(1);

        var read = await customerClient.PostAsync($"/api/v1/conversations/{conversation.Id}/read", null);
        read.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await customerClient.GetFromJsonAsync<ConversationDto>($"/api/v1/conversations/{conversation.Id}"))!.UnreadCount.ShouldBe(0);

        var hidden = await ClientFor(stranger).GetAsync($"/api/v1/conversations/{conversation.Id}");
        hidden.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await CodeAsync(hidden)).ShouldBe("conversation.not_found");
    }

    [Fact]
    public async Task Empty_messages_are_refused()
    {
        var (requestId, _, _, owner, _) = await GivenRequestAsync();
        var client = ClientFor(owner);
        var conversation = (await (await client.PostAsync($"/api/v1/requests/{requestId}/conversation", null)).Content.ReadFromJsonAsync<ConversationDto>())!;

        var empty = await client.PostAsJsonAsync($"/api/v1/conversations/{conversation.Id}/messages", new { body = " " });

        empty.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await empty.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("errors").GetProperty("body")[0].GetString().ShouldBe("message.empty");
    }

    [Fact]
    public async Task The_chat_hub_needs_a_portal_token_which_browsers_send_in_the_query_string()
    {
        var (_, _, customer, _, _) = await GivenRequestAsync();
        var client = factory.CreateClient();

        (await client.PostAsync($"{ChatHub.Path}/negotiate?negotiateVersion=1", null)).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        var negotiated = await client.PostAsync($"{ChatHub.Path}/negotiate?negotiateVersion=1&access_token={TokenFor(customer)}", null);
        negotiated.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await negotiated.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("connectionToken").GetString().ShouldNotBeNullOrEmpty();
    }

    [Fact]
    public async Task The_notifier_sends_events_to_each_users_group()
    {
        var clients = new RecordingHubClients();
        var notifier = new SignalRChatNotifier(new FakeHubContext(clients), NullLogger<SignalRChatNotifier>.Instance);
        var (a, b) = (Guid.NewGuid(), Guid.NewGuid());
        var message = new MessageDto(Guid.NewGuid(), Guid.NewGuid(), "Partner", "Hi", [], DateTimeOffset.UtcNow);

        await notifier.MessageSentAsync([a, b, a], message, CancellationToken.None);
        await notifier.ConversationReadAsync([a], message.ConversationId, "Customer", DateTimeOffset.UtcNow, CancellationToken.None);
        clients.Fail = true;
        await notifier.MessageSentAsync([a], message, CancellationToken.None);

        clients.Calls.Select(c => (string.Join(",", c.Groups), c.Method)).ShouldBe(new[]
        {
            ($"{ChatHub.UserGroup(a)},{ChatHub.UserGroup(b)}", SignalRChatNotifier.MessageReceived),
            (ChatHub.UserGroup(a), SignalRChatNotifier.ConversationRead),
        });
        clients.Calls[0].Payload.ShouldBe(message);
    }

    private sealed class FakeHubContext(IHubClients clients) : IHubContext<ChatHub>
    {
        public IHubClients Clients { get; } = clients;

        public IGroupManager Groups => throw new NotSupportedException();
    }

    private sealed class RecordingHubClients : IHubClients
    {
        public List<(IReadOnlyList<string> Groups, string Method, object? Payload)> Calls { get; } = [];

        public bool Fail { get; set; }

        public IClientProxy All => throw new NotSupportedException();

        public IClientProxy AllExcept(IReadOnlyList<string> excludedConnectionIds) => throw new NotSupportedException();

        public IClientProxy Client(string connectionId) => throw new NotSupportedException();

        public IClientProxy Clients(IReadOnlyList<string> connectionIds) => throw new NotSupportedException();

        public IClientProxy Group(string groupName) => Groups([groupName]);

        public IClientProxy GroupExcept(string groupName, IReadOnlyList<string> excludedConnectionIds) => throw new NotSupportedException();

        public IClientProxy Groups(IReadOnlyList<string> groupNames) => new Proxy(this, groupNames);

        public IClientProxy User(string userId) => throw new NotSupportedException();

        public IClientProxy Users(IReadOnlyList<string> userIds) => throw new NotSupportedException();

        private sealed class Proxy(RecordingHubClients owner, IReadOnlyList<string> groups) : IClientProxy
        {
            public Task SendCoreAsync(string method, object?[] args, CancellationToken cancellationToken = default)
            {
                if (owner.Fail)
                {
                    throw new InvalidOperationException("Hub down");
                }

                owner.Calls.Add((groups, method, args.FirstOrDefault()));
                return Task.CompletedTask;
            }
        }
    }
}
