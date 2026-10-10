using FluentValidation;
using HomeServices.Application.Abstractions;
using HomeServices.Application.Common;
using HomeServices.Application.Files;
using HomeServices.Application.Messaging;
using HomeServices.Application.Partners;
using HomeServices.Domain.Requests;
using Microsoft.EntityFrameworkCore;

namespace HomeServices.Application.Requests;

/// <summary>Requests the signed-in partner received, newest first. <see cref="Status"/> filters by what the partner did with them.</summary>
public sealed record GetInbox(RecipientStatus? Status = null, int Page = 1, int PageSize = GetInbox.DefaultPageSize)
    : IQuery<PagedResult<InboxItemDto>>
{
    public const int DefaultPageSize = 20;
    public const int MaxPageSize = 100;
}

/// <summary>One received request. Opening it the first time marks it viewed.</summary>
public sealed record GetInboxRequest(Guid Id) : ICommand<InboxRequestDto>;

/// <summary>The partner turns a request down, optionally saying why (only staff see the reason).</summary>
public sealed record DeclineInboxRequest(Guid Id, string? Reason) : ICommand<InboxRequestDto>;

public sealed class GetInboxValidator : AbstractValidator<GetInbox>
{
    public GetInboxValidator()
    {
        RuleFor(x => x.Status).IsInEnum().WithErrorCode("status.invalid");
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1).WithErrorCode("page.invalid");
        RuleFor(x => x.PageSize).InclusiveBetween(1, GetInbox.MaxPageSize).WithErrorCode("page_size.invalid");
    }
}

public sealed class DeclineInboxRequestValidator : AbstractValidator<DeclineInboxRequest>
{
    public DeclineInboxRequestValidator() =>
        RuleFor(x => x.Reason)
            .Must(reason => reason!.Trim().Length <= RequestRecipient.DeclineReasonMaxLength).WithErrorCode("reason.too_long")
            .When(x => x.Reason is not null);
}

public sealed class GetInboxHandler(IAppDbContext db, ICurrentUser currentUser, ICurrentLanguage language)
    : IQueryHandler<GetInbox, PagedResult<InboxItemDto>>
{
    public async Task<PagedResult<InboxItemDto>> HandleAsync(GetInbox query, CancellationToken cancellationToken)
    {
        var partnerId = await Inbox.MyPartnerIdAsync(db, currentUser, cancellationToken);
        var rows =
            from request in db.ServiceRequests.AsNoTracking()
            from recipient in request.Recipients
            where recipient.PartnerProfileId == partnerId
            select new { Request = request, Recipient = recipient };
        if (query.Status is { } status)
        {
            rows = rows.Where(r => r.Recipient.Status == status);
        }

        var total = await rows.CountAsync(cancellationToken);
        var page = await rows
            .OrderByDescending(r => r.Recipient.SentAt).ThenByDescending(r => r.Request.Id)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(r => new { r.Request, r.Recipient, MediaCount = r.Request.Media.Count })
            .ToListAsync(cancellationToken);

        var lookup = await RequestLookup.LoadAsync(db, language, page.Select(p => p.Request), cancellationToken);
        var items = page
            .Select(p => new InboxItemDto(
                p.Request.Id,
                p.Request.Kind.ToString(),
                p.Request.Status.ToString(),
                p.Recipient.Status.ToString(),
                lookup.Place(p.Request),
                RequestLookup.Excerpt(p.Request.Description),
                p.Request.PreferredDate,
                p.MediaCount,
                p.Recipient.SentAt))
            .ToList();
        return new PagedResult<InboxItemDto>(items, query.Page, query.PageSize, total);
    }
}

public sealed class GetInboxRequestHandler(IAppDbContext db, ICurrentUser currentUser, ICurrentLanguage language, FileDtoFactory files, TimeProvider clock)
    : ICommandHandler<GetInboxRequest, InboxRequestDto>
{
    public async Task<InboxRequestDto> HandleAsync(GetInboxRequest command, CancellationToken cancellationToken)
    {
        var (request, partnerId) = await Inbox.LoadAsync(db, currentUser, command.Id, cancellationToken);
        if (request.FindRecipient(partnerId)!.Status == RecipientStatus.New)
        {
            request.MarkViewed(partnerId, clock.GetUtcNow());
            await db.SaveChangesAsync(cancellationToken);
        }

        return await Inbox.ToDtoAsync(db, language, files, request, partnerId, cancellationToken);
    }
}

public sealed class DeclineInboxRequestHandler(IAppDbContext db, ICurrentUser currentUser, ICurrentLanguage language, FileDtoFactory files, TimeProvider clock)
    : ICommandHandler<DeclineInboxRequest, InboxRequestDto>
{
    public async Task<InboxRequestDto> HandleAsync(DeclineInboxRequest command, CancellationToken cancellationToken)
    {
        var (request, partnerId) = await Inbox.LoadAsync(db, currentUser, command.Id, cancellationToken);
        request.Decline(partnerId, command.Reason, clock.GetUtcNow());
        await db.SaveChangesAsync(cancellationToken);
        return await Inbox.ToDtoAsync(db, language, files, request, partnerId, cancellationToken);
    }
}

internal static class Inbox
{
    /// <summary>The signed-in user's partner profile id; users without one have no inbox.</summary>
    public static async Task<Guid> MyPartnerIdAsync(IAppDbContext db, ICurrentUser currentUser, CancellationToken cancellationToken)
    {
        var userId = MyPartnerProfile.UserId(currentUser);
        var partnerId = await db.PartnerProfiles.AsNoTracking()
            .Where(p => p.UserId == userId)
            .Select(p => (Guid?)p.Id)
            .SingleOrDefaultAsync(cancellationToken);
        return partnerId ?? throw MyPartnerProfile.NotFound();
    }

    /// <summary>A request the signed-in partner received; any other request is "not found".</summary>
    public static async Task<(ServiceRequest Request, Guid PartnerId)> LoadAsync(IAppDbContext db, ICurrentUser currentUser, Guid id, CancellationToken cancellationToken)
    {
        var partnerId = await MyPartnerIdAsync(db, currentUser, cancellationToken);
        var request = await db.ServiceRequests
            .Include(r => r.Recipients)
            .Include(r => r.Media)
            .Include(r => r.Lines)
            .SingleOrDefaultAsync(r => r.Id == id && r.Recipients.Any(x => x.PartnerProfileId == partnerId), cancellationToken)
            ?? throw MyRequests.NotFound();
        return (request, partnerId);
    }

    public static async Task<InboxRequestDto> ToDtoAsync(
        IAppDbContext db,
        ICurrentLanguage language,
        FileDtoFactory files,
        ServiceRequest request,
        Guid partnerId,
        CancellationToken cancellationToken)
    {
        var lookup = await RequestLookup.LoadAsync(db, language, [request], cancellationToken);
        var recipient = request.FindRecipient(partnerId)!;
        var customerName = await db.Users.AsNoTracking()
            .Where(u => u.Id == request.CustomerId)
            .Select(u => u.FullName)
            .SingleOrDefaultAsync(cancellationToken);

        return new InboxRequestDto(
            request.Id,
            request.Kind.ToString(),
            request.Status.ToString(),
            recipient.Status.ToString(),
            lookup.Place(request),
            request.Description,
            request.PreferredDate,
            request.TimeNote,
            await RequestLookup.MediaAsync(db, files, request, cancellationToken),
            FirstName(customerName),
            recipient.SentAt,
            request.CreatedAt,
            await RequestLookup.LinesAsync(db, language, request, withEstimate: false, cancellationToken));
    }

    private static string? FirstName(string? fullName) =>
        string.IsNullOrWhiteSpace(fullName) ? null : fullName.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries)[0];
}
