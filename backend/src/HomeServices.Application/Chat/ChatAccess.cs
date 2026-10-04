using HomeServices.Application.Abstractions;
using HomeServices.Application.Errors;
using HomeServices.Application.Files;
using HomeServices.Application.Orders;
using HomeServices.Application.Requests;
using HomeServices.Domain.Chat;
using HomeServices.Domain.Files;
using HomeServices.Domain.Orders;
using HomeServices.Domain.Requests;
using Microsoft.EntityFrameworkCore;

namespace HomeServices.Application.Chat;

/// <summary>Who may see a conversation, whether they may write in it, and how it looks to them.</summary>
internal static class ChatAccess
{
    private const int ExcerptLength = 100;

    public static NotFoundException NotFound() => new("Conversation not found.", "conversation.not_found");

    /// <summary>A conversation of the signed-in user (either side), tracked, with their role in it.</summary>
    public static async Task<(Conversation Conversation, ChatRole Role, Guid UserId)> LoadAsync(
        IAppDbContext db, ICurrentUser currentUser, Guid id, CancellationToken cancellationToken)
    {
        var (userId, partnerId) = await OrderViews.MeAsync(db, currentUser, cancellationToken);
        var conversation = await db.Conversations.SingleOrDefaultAsync(c => c.Id == id, cancellationToken) ?? throw NotFound();
        if (conversation.CustomerId == userId)
        {
            return (conversation, ChatRole.Customer, userId);
        }

        if (conversation.PartnerProfileId == partnerId)
        {
            return (conversation, ChatRole.Partner, userId);
        }

        throw NotFound();
    }

    /// <summary>The order between the two sides on this request (not cancelled), if any.</summary>
    public static Task<Guid?> OrderIdAsync(IAppDbContext db, Conversation conversation, CancellationToken cancellationToken) =>
        db.Orders.AsNoTracking()
            .Where(o => o.RequestId == conversation.RequestId && o.PartnerProfileId == conversation.PartnerProfileId && o.Status != OrderStatus.Cancelled)
            .OrderByDescending(o => o.CreatedAt)
            .Select(o => (Guid?)o.Id)
            .FirstOrDefaultAsync(cancellationToken);

    /// <summary>
    /// Writing is open while the request is open and the partner hasn't declined it, or while the two have an order.
    /// </summary>
    public static async Task<bool> CanSendAsync(IAppDbContext db, Conversation conversation, CancellationToken cancellationToken)
    {
        if (await OrderIdAsync(db, conversation, cancellationToken) is not null)
        {
            return true;
        }

        return await db.ServiceRequests.AsNoTracking().AnyAsync(
            r => r.Id == conversation.RequestId && r.Status == RequestStatus.Open
                && r.Recipients.Any(x => x.PartnerProfileId == conversation.PartnerProfileId && x.Status != RecipientStatus.Declined),
            cancellationToken);
    }

    /// <summary>Both sides' user ids, for notifications.</summary>
    public static async Task<IReadOnlyCollection<Guid>> PartiesAsync(IAppDbContext db, Conversation conversation, CancellationToken cancellationToken)
    {
        var partnerUserId = await db.PartnerProfiles.AsNoTracking()
            .Where(p => p.Id == conversation.PartnerProfileId)
            .Select(p => p.UserId)
            .SingleAsync(cancellationToken);
        return [conversation.CustomerId, partnerUserId];
    }

    public static async Task<IReadOnlyList<MessageDto>> MessageDtosAsync(
        IAppDbContext db, FileDtoFactory files, IReadOnlyList<Message> messages, CancellationToken cancellationToken)
    {
        var fileIds = messages.SelectMany(m => m.Attachments).Select(a => a.FileId).Distinct().ToList();
        var stored = await db.Files.AsNoTracking()
            .Where(f => fileIds.Contains(f.Id) && f.Status == FileStatus.Ready)
            .ToDictionaryAsync(f => f.Id, cancellationToken);

        var result = new List<MessageDto>();
        foreach (var message in messages)
        {
            var attachments = new List<FileDto>();
            foreach (var attachment in message.Attachments.OrderBy(a => a.SortOrder).Where(a => stored.ContainsKey(a.FileId)))
            {
                attachments.Add(await files.CreateAsync(stored[attachment.FileId], cancellationToken));
            }

            result.Add(new MessageDto(message.Id, message.ConversationId, message.SenderRole.ToString(), message.Body, attachments, message.SentAt));
        }

        return result;
    }

    /// <summary>
    /// The conversation as <paramref name="role"/> sees it. Before they agree on an order, a partner sees only the
    /// customer's first name (as in the inbox).
    /// </summary>
    public static async Task<ConversationDto> ToDtoAsync(
        IAppDbContext db, ICurrentLanguage language, Conversation conversation, ChatRole role, CancellationToken cancellationToken)
    {
        var request = await db.ServiceRequests.AsNoTracking().SingleAsync(r => r.Id == conversation.RequestId, cancellationToken);
        var lookup = await RequestLookup.LoadAsync(db, language, [request], cancellationToken);
        var orderId = await OrderIdAsync(db, conversation, cancellationToken);
        var other = role == ChatRole.Customer
            ? await db.PartnerProfiles.AsNoTracking()
                .Where(p => p.Id == conversation.PartnerProfileId)
                .Select(p => new ChatPartyDto(p.DisplayName, p.Id, p.Slug))
                .SingleAsync(cancellationToken)
            : new ChatPartyDto(CustomerName(await db.Users.AsNoTracking().Where(u => u.Id == conversation.CustomerId).Select(u => u.FullName).SingleAsync(cancellationToken), orderId is not null), null, null);

        var otherRole = role == ChatRole.Customer ? ChatRole.Partner : ChatRole.Customer;
        var readAt = conversation.ReadAtOf(role);
        var unread = await db.Messages.CountAsync(
            m => m.ConversationId == conversation.Id && m.SenderRole == otherRole && (readAt == null || m.SentAt > readAt),
            cancellationToken);
        var last = await db.Messages.AsNoTracking()
            .Where(m => m.ConversationId == conversation.Id)
            .OrderByDescending(m => m.SentAt).ThenByDescending(m => m.Id)
            .Select(m => new { m.SenderRole, m.Body, Files = m.Attachments.Count, m.SentAt })
            .FirstOrDefaultAsync(cancellationToken);

        return new ConversationDto(
            conversation.Id,
            conversation.RequestId,
            request.Status.ToString(),
            lookup.Place(request),
            role.ToString(),
            other,
            unread,
            last is null ? null : new MessageSummaryDto(last.SenderRole.ToString(), Excerpt(last.Body), last.Files, last.SentAt),
            conversation.ReadAtOf(otherRole),
            await CanSendAsync(db, conversation, cancellationToken),
            orderId);
    }

    private static string? CustomerName(string? fullName, bool agreed)
    {
        if (string.IsNullOrWhiteSpace(fullName))
        {
            return null;
        }

        return agreed ? fullName.Trim() : fullName.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries)[0];
    }

    private static string? Excerpt(string? body) =>
        body is null || body.Length <= ExcerptLength ? body : $"{body[..(ExcerptLength - 1)].TrimEnd()}…";
}
