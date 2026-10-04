using FluentValidation;
using HomeServices.Application.Abstractions;
using HomeServices.Application.Common;
using HomeServices.Application.Files;
using HomeServices.Application.Messaging;
using HomeServices.Application.Orders;
using HomeServices.Application.Requests;
using HomeServices.Domain;
using HomeServices.Domain.Chat;
using HomeServices.Domain.Files;
using HomeServices.Domain.Orders;
using HomeServices.Domain.Requests;
using Microsoft.EntityFrameworkCore;

namespace HomeServices.Application.Chat;

/// <summary>
/// Opens (or returns) the conversation about a request between its customer and one partner who received it.
/// The partner calls it without <see cref="PartnerId"/>; the customer names the partner.
/// </summary>
public sealed record OpenConversation(Guid RequestId, Guid? PartnerId) : ICommand<ConversationDto>;

/// <summary>The signed-in user's conversations (both sides), most recent first.</summary>
public sealed record GetConversations(int Page = 1, int PageSize = GetConversations.DefaultPageSize) : IQuery<PagedResult<ConversationDto>>
{
    public const int DefaultPageSize = 20;
    public const int MaxPageSize = 100;
}

public sealed record GetConversation(Guid Id) : IQuery<ConversationDto>;

/// <summary>A page of messages, newest page first: messages sent before <see cref="Before"/> (or the latest ones).</summary>
public sealed record GetMessages(Guid ConversationId, DateTimeOffset? Before = null, int Limit = GetMessages.DefaultLimit) : IQuery<MessagePageDto>
{
    public const int DefaultLimit = 50;
    public const int MaxLimit = 100;
}

/// <summary>Sends a message: text, files (uploaded with /api/v1/files first), or both.</summary>
public sealed record SendMessage(Guid ConversationId, string? Body, IReadOnlyList<Guid>? FileIds) : ICommand<MessageDto>;

/// <summary>Marks the conversation read up to now (the other side sees it as a read receipt). Returns the time.</summary>
public sealed record MarkConversationRead(Guid ConversationId) : ICommand<DateTimeOffset>;

public sealed record GetUnreadSummary : IQuery<UnreadSummaryDto>;

public sealed class GetConversationsValidator : AbstractValidator<GetConversations>
{
    public GetConversationsValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1).WithErrorCode("page.invalid");
        RuleFor(x => x.PageSize).InclusiveBetween(1, GetConversations.MaxPageSize).WithErrorCode("page_size.invalid");
    }
}

public sealed class GetMessagesValidator : AbstractValidator<GetMessages>
{
    public GetMessagesValidator() =>
        RuleFor(x => x.Limit).InclusiveBetween(1, GetMessages.MaxLimit).WithErrorCode("limit.invalid");
}

public sealed class SendMessageValidator : AbstractValidator<SendMessage>
{
    public SendMessageValidator()
    {
        RuleFor(x => x.Body)
            .Must((command, body) => !string.IsNullOrWhiteSpace(body) || command.FileIds is { Count: > 0 }).WithErrorCode("message.empty")
            .Must(body => body is null || body.Trim().Length <= Message.BodyMaxLength).WithErrorCode("body.too_long");
        RuleFor(x => x.FileIds)
            .Must(ids => ids!.Distinct().Count() <= Message.MaxAttachments).WithErrorCode("file_ids.too_many")
            .When(x => x.FileIds is not null);
    }
}

public sealed class OpenConversationHandler(IAppDbContext db, ICurrentUser currentUser, ICurrentLanguage language)
    : ICommandHandler<OpenConversation, ConversationDto>
{
    public async Task<ConversationDto> HandleAsync(OpenConversation command, CancellationToken cancellationToken)
    {
        var (userId, myPartnerId) = await OrderViews.MeAsync(db, currentUser, cancellationToken);
        var request = await db.ServiceRequests.AsNoTracking()
            .Include(r => r.Recipients)
            .SingleOrDefaultAsync(r => r.Id == command.RequestId, cancellationToken)
            ?? throw MyRequests.NotFound();

        ChatRole role;
        Guid partnerId;
        if (command.PartnerId is { } chosen && request.CustomerId == userId)
        {
            (role, partnerId) = (ChatRole.Customer, chosen);
        }
        else if (command.PartnerId is null && myPartnerId is { } mine && request.FindRecipient(mine) is not null)
        {
            (role, partnerId) = (ChatRole.Partner, mine);
        }
        else
        {
            throw MyRequests.NotFound();
        }

        var conversation = await db.Conversations.SingleOrDefaultAsync(c => c.RequestId == request.Id && c.PartnerProfileId == partnerId, cancellationToken);
        if (conversation is null)
        {
            // A new conversation needs an open request the partner hasn't declined, or an order between the two.
            var recipient = request.FindRecipient(partnerId);
            var hasOrder = await db.Orders.AnyAsync(
                o => o.RequestId == request.Id && o.PartnerProfileId == partnerId && o.Status != OrderStatus.Cancelled, cancellationToken);
            var openToThem = request.Status == RequestStatus.Open && recipient is not null && recipient.Status != RecipientStatus.Declined;
            if (!openToThem && !hasOrder)
            {
                throw new DomainException("conversation.unavailable", "You can't start a conversation about this request with this partner.");
            }

            conversation = Conversation.Start(request.Id, request.CustomerId, partnerId);
            db.Conversations.Add(conversation);
            await db.SaveChangesAsync(cancellationToken);
        }

        return await ChatAccess.ToDtoAsync(db, language, conversation, role, cancellationToken);
    }
}

public sealed class GetConversationsHandler(IAppDbContext db, ICurrentUser currentUser, ICurrentLanguage language)
    : IQueryHandler<GetConversations, PagedResult<ConversationDto>>
{
    public async Task<PagedResult<ConversationDto>> HandleAsync(GetConversations query, CancellationToken cancellationToken)
    {
        var (userId, partnerId) = await OrderViews.MeAsync(db, currentUser, cancellationToken);
        var mine = db.Conversations.AsNoTracking().Where(c => c.CustomerId == userId || c.PartnerProfileId == partnerId);
        var total = await mine.CountAsync(cancellationToken);
        var page = await mine
            .OrderByDescending(c => c.LastMessageAt ?? c.CreatedAt).ThenByDescending(c => c.Id)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);

        var items = new List<ConversationDto>();
        foreach (var conversation in page)
        {
            var role = conversation.CustomerId == userId ? ChatRole.Customer : ChatRole.Partner;
            items.Add(await ChatAccess.ToDtoAsync(db, language, conversation, role, cancellationToken));
        }

        return new PagedResult<ConversationDto>(items, query.Page, query.PageSize, total);
    }
}

public sealed class GetConversationHandler(IAppDbContext db, ICurrentUser currentUser, ICurrentLanguage language)
    : IQueryHandler<GetConversation, ConversationDto>
{
    public async Task<ConversationDto> HandleAsync(GetConversation query, CancellationToken cancellationToken)
    {
        var (conversation, role, _) = await ChatAccess.LoadAsync(db, currentUser, query.Id, cancellationToken);
        return await ChatAccess.ToDtoAsync(db, language, conversation, role, cancellationToken);
    }
}

public sealed class GetMessagesHandler(IAppDbContext db, ICurrentUser currentUser, FileDtoFactory files)
    : IQueryHandler<GetMessages, MessagePageDto>
{
    public async Task<MessagePageDto> HandleAsync(GetMessages query, CancellationToken cancellationToken)
    {
        var (conversation, _, _) = await ChatAccess.LoadAsync(db, currentUser, query.ConversationId, cancellationToken);
        var messages = db.Messages.AsNoTracking().Include(m => m.Attachments).Where(m => m.ConversationId == conversation.Id);
        if (query.Before is { } before)
        {
            messages = messages.Where(m => m.SentAt < before);
        }

        var newest = await messages
            .OrderByDescending(m => m.SentAt).ThenByDescending(m => m.Id)
            .Take(query.Limit + 1)
            .ToListAsync(cancellationToken);
        var page = newest.Take(query.Limit).Reverse().ToList();
        return new MessagePageDto(await ChatAccess.MessageDtosAsync(db, files, page, cancellationToken), newest.Count > query.Limit);
    }
}

public sealed class SendMessageHandler(IAppDbContext db, ICurrentUser currentUser, FileDtoFactory files, IChatNotifier notifier, TimeProvider clock)
    : ICommandHandler<SendMessage, MessageDto>
{
    public async Task<MessageDto> HandleAsync(SendMessage command, CancellationToken cancellationToken)
    {
        var (conversation, role, userId) = await ChatAccess.LoadAsync(db, currentUser, command.ConversationId, cancellationToken);
        if (!await ChatAccess.CanSendAsync(db, conversation, cancellationToken))
        {
            throw new DomainException("conversation.closed", "This conversation is closed: the request ended without an order.");
        }

        var fileIds = (command.FileIds ?? []).Distinct().ToList();
        var owner = userId.ToString();
        var usable = await db.Files.CountAsync(
            f => fileIds.Contains(f.Id) && f.OwnerType == FileOwnerType.User && f.OwnerId == owner && f.Status == FileStatus.Ready,
            cancellationToken);
        if (usable != fileIds.Count)
        {
            throw new DomainException("message.files_invalid", "Some files can't be sent: upload them first.");
        }

        var now = clock.GetUtcNow();
        var message = conversation.Post(userId, role, command.Body, fileIds, now);
        db.Messages.Add(message);

        // A partner asking a question has responded to the request: nobody needs to chase it.
        if (role == ChatRole.Partner)
        {
            var request = await db.ServiceRequests.Include(r => r.Recipients).SingleAsync(r => r.Id == conversation.RequestId, cancellationToken);
            if (request.Status == RequestStatus.Open && request.FindRecipient(conversation.PartnerProfileId)?.Status is RecipientStatus.New or RecipientStatus.Viewed)
            {
                request.MarkResponded(conversation.PartnerProfileId, now);
            }
        }

        await db.SaveChangesAsync(cancellationToken);
        var dto = (await ChatAccess.MessageDtosAsync(db, files, [message], cancellationToken))[0];
        await notifier.MessageSentAsync(await ChatAccess.PartiesAsync(db, conversation, cancellationToken), dto, cancellationToken);
        return dto;
    }
}

public sealed class MarkConversationReadHandler(IAppDbContext db, ICurrentUser currentUser, IChatNotifier notifier, TimeProvider clock)
    : ICommandHandler<MarkConversationRead, DateTimeOffset>
{
    public async Task<DateTimeOffset> HandleAsync(MarkConversationRead command, CancellationToken cancellationToken)
    {
        var (conversation, role, _) = await ChatAccess.LoadAsync(db, currentUser, command.ConversationId, cancellationToken);
        conversation.MarkRead(role, clock.GetUtcNow());
        await db.SaveChangesAsync(cancellationToken);

        var readAt = conversation.ReadAtOf(role)!.Value;
        await notifier.ConversationReadAsync(await ChatAccess.PartiesAsync(db, conversation, cancellationToken), conversation.Id, role.ToString(), readAt, cancellationToken);
        return readAt;
    }
}

public sealed class GetUnreadSummaryHandler(IAppDbContext db, ICurrentUser currentUser) : IQueryHandler<GetUnreadSummary, UnreadSummaryDto>
{
    public async Task<UnreadSummaryDto> HandleAsync(GetUnreadSummary query, CancellationToken cancellationToken)
    {
        var (userId, partnerId) = await OrderViews.MeAsync(db, currentUser, cancellationToken);
        var asCustomer =
            from c in db.Conversations.AsNoTracking()
            where c.CustomerId == userId
            from m in db.Messages.AsNoTracking()
            where m.ConversationId == c.Id && m.SenderRole == ChatRole.Partner && (c.CustomerReadAt == null || m.SentAt > c.CustomerReadAt)
            select m.ConversationId;
        var asPartner =
            from c in db.Conversations.AsNoTracking()
            where c.PartnerProfileId == partnerId
            from m in db.Messages.AsNoTracking()
            where m.ConversationId == c.Id && m.SenderRole == ChatRole.Customer && (c.PartnerReadAt == null || m.SentAt > c.PartnerReadAt)
            select m.ConversationId;

        var unread = (await asCustomer.ToListAsync(cancellationToken)).Concat(await asPartner.ToListAsync(cancellationToken)).ToList();
        return new UnreadSummaryDto(unread.Distinct().Count(), unread.Count);
    }
}
