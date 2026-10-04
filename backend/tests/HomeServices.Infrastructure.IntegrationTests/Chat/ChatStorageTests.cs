using HomeServices.Application.Chat;
using HomeServices.Application.Files;
using HomeServices.Domain.Catalog;
using HomeServices.Domain.Chat;
using HomeServices.Domain.Files;
using HomeServices.Domain.Identity;
using HomeServices.Domain.Localization;
using HomeServices.Domain.Partners;
using HomeServices.Domain.Requests;
using HomeServices.Infrastructure.IntegrationTests.Fakes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace HomeServices.Infrastructure.IntegrationTests.Chat;

[Collection(PostgresCollection.Name)]
public class ChatStorageTests(PostgresFixture db)
{
    private sealed class NoNotifier : IChatNotifier
    {
        public Task MessageSentAsync(IReadOnlyCollection<Guid> userIds, MessageDto message, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task ConversationReadAsync(IReadOnlyCollection<Guid> userIds, Guid conversationId, string readerRole, DateTimeOffset readAt, CancellationToken cancellationToken) =>
            Task.CompletedTask;
    }

    private static LocalizedText Text(string hy) => LocalizedText.Empty.With("hy", hy);

    private static User NewUser() => User.Register(PhoneNumber.Parse($"+37492{Random.Shared.Next(100_000, 999_999)}"));

    private FileDtoFactory Files => new(new FakeFileStorage(), Options.Create(new FileSettings()), db.Clock);

    [Fact]
    public async Task Conversations_messages_and_unread_counts_work_on_PostgreSQL()
    {
        var category = Category.Create($"chat-{Guid.NewGuid():N}"[..30], Text("Սանտեխնիկա"), 1);
        var city = City.Create($"city-{Guid.NewGuid():N}"[..30], Text("Երևան"), 1);
        var customer = NewUser();
        var owner = NewUser();
        var work = StoredFile.Begin(FileOwnerType.User, owner.Id.ToString(), "work.jpg", "image/jpeg", 100, db.Clock.Now);
        work.MarkReady(100, db.Clock.Now);
        var partner = PartnerProfile.Create(owner.Id, PartnerType.Specialist, "Aram");
        partner.UpdateDetails(PartnerType.Specialist, "Aram", new string('a', 60), null, null);
        partner.SetServices([category.Id]);
        partner.SetAreas([(city.Id, null)]);
        partner.AddMedia(PartnerMediaKind.WorkExample, work.Id, null);
        partner.Submit(db.Clock.Now);
        partner.Approve(db.Clock.Now);
        var request = ServiceRequest.Create(customer.Id, RequestKind.Open, category.Id, city.Id, null, "The kitchen tap is leaking badly.", null, null, null, null);
        request.SendTo([partner.Id], RecipientSource.Matched, db.Clock.Now);
        await using (var context = db.CreateContext())
        {
            context.AddRange(category, city, customer, owner, work, partner, request);
            await context.SaveChangesAsync();
        }

        var partnerUser = new FakeCurrentUser { UserId = owner.Id.ToString() };
        var customerUser = new FakeCurrentUser { UserId = customer.Id.ToString() };
        ConversationDto conversation;
        await using (var context = db.CreateContext())
        {
            conversation = await new OpenConversationHandler(context, partnerUser, new FakeCurrentLanguage())
                .HandleAsync(new OpenConversation(request.Id, null), CancellationToken.None);
            await new SendMessageHandler(context, partnerUser, Files, new NoNotifier(), db.Clock)
                .HandleAsync(new SendMessage(conversation.Id, "When can I come?", null), CancellationToken.None);
        }

        await using (var context = db.CreateContext())
        {
            (await new GetUnreadSummaryHandler(context, customerUser).HandleAsync(new GetUnreadSummary(), CancellationToken.None)).ShouldBe(new UnreadSummaryDto(1, 1));
            var list = await new GetConversationsHandler(context, customerUser, new FakeCurrentLanguage()).HandleAsync(new GetConversations(), CancellationToken.None);
            var item = list.Items.ShouldHaveSingleItem();
            item.UnreadCount.ShouldBe(1);
            item.OtherParty.Name.ShouldBe("Aram");
            item.LastMessage!.Excerpt.ShouldBe("When can I come?");
            var page = await new GetMessagesHandler(context, customerUser, Files).HandleAsync(new GetMessages(conversation.Id), CancellationToken.None);
            page.Items.ShouldHaveSingleItem().SenderRole.ShouldBe("Partner");
            (await context.ServiceRequests.Include(r => r.Recipients).SingleAsync(r => r.Id == request.Id)).FindRecipient(partner.Id)!.Status.ShouldBe(RecipientStatus.Responded);
        }

        await using (var context = db.CreateContext())
        {
            await new MarkConversationReadHandler(context, customerUser, new NoNotifier(), db.Clock).HandleAsync(new MarkConversationRead(conversation.Id), CancellationToken.None);
            (await new GetUnreadSummaryHandler(context, customerUser).HandleAsync(new GetUnreadSummary(), CancellationToken.None)).ShouldBe(new UnreadSummaryDto(0, 0));
            (await context.Conversations.SingleAsync(c => c.Id == conversation.Id)).CustomerReadAt.ShouldBe(db.Clock.Now);
            (await context.Messages.CountAsync(m => m.ConversationId == conversation.Id && m.SenderRole == ChatRole.Partner)).ShouldBe(1);
        }
    }
}
