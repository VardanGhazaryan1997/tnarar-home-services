using FluentValidation;
using HomeServices.Application.Abstractions;
using HomeServices.Application.Common;
using HomeServices.Application.Files;
using HomeServices.Application.Messaging;
using HomeServices.Application.Notifications;
using HomeServices.Application.Offers;
using HomeServices.Domain;
using HomeServices.Domain.Identity;
using HomeServices.Domain.Notifications;
using HomeServices.Domain.Requests;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace HomeServices.Application.Requests;

/// <summary>
/// Requests for the Back Office. With <see cref="NeedsAttention"/> = true this is the operator queue, waiting
/// longest first; otherwise newest first. <see cref="Search"/> matches the description, or the customer's phone.
/// </summary>
public sealed record GetAdminRequests(
    RequestStatus? Status = null,
    RequestKind? Kind = null,
    bool? NeedsAttention = null,
    Guid? CategoryId = null,
    Guid? CityId = null,
    string? Search = null,
    int Page = 1,
    int PageSize = GetAdminRequests.DefaultPageSize) : IQuery<PagedResult<AdminRequestListItemDto>>
{
    public const int DefaultPageSize = 20;
    public const int MaxPageSize = 100;
}

public sealed record GetAdminRequest(Guid Id) : IQuery<AdminRequestDto>;

/// <summary>Staff send the request to partners they chose. Each must be able to receive requests.</summary>
public sealed record AssignRequest(Guid Id, IReadOnlyList<Guid> PartnerIds) : ICommand<AdminRequestDto>;

/// <summary>Staff withdraw a request (spam, duplicate…). The reason is required.</summary>
public sealed record CancelRequestByStaff(Guid Id, string Reason) : ICommand<AdminRequestDto>;

/// <summary>Moves open requests nobody responded to in time to the operator queue. Run by the follow-up job.</summary>
public sealed record FlagUnansweredRequests : ICommand<int>;

public sealed class GetAdminRequestsValidator : AbstractValidator<GetAdminRequests>
{
    public GetAdminRequestsValidator()
    {
        RuleFor(x => x.Status).IsInEnum().WithErrorCode("status.invalid");
        RuleFor(x => x.Kind).IsInEnum().WithErrorCode("kind.invalid");
        RuleFor(x => x.Search).MaximumLength(100).WithErrorCode("search.too_long");
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1).WithErrorCode("page.invalid");
        RuleFor(x => x.PageSize).InclusiveBetween(1, GetAdminRequests.MaxPageSize).WithErrorCode("page_size.invalid");
    }
}

public sealed class AssignRequestValidator : AbstractValidator<AssignRequest>
{
    public const int MaxPartners = 20;

    public AssignRequestValidator()
    {
        RuleFor(x => x.PartnerIds).NotEmpty().WithErrorCode("partner_ids.required");
        RuleFor(x => x.PartnerIds)
            .Must(ids => ids.Distinct().Count() <= MaxPartners).WithErrorCode("partner_ids.too_many")
            .When(x => x.PartnerIds is not null);
    }
}

public sealed class CancelRequestByStaffValidator : AbstractValidator<CancelRequestByStaff>
{
    public CancelRequestByStaffValidator()
    {
        RuleFor(x => x.Reason).Must(reason => !string.IsNullOrWhiteSpace(reason)).WithErrorCode("reason.required");
        RuleFor(x => x.Reason)
            .Must(reason => reason.Trim().Length <= ServiceRequest.CancelReasonMaxLength).WithErrorCode("reason.too_long")
            .When(x => x.Reason is not null);
    }
}

public sealed class GetAdminRequestsHandler(IAppDbContext db, ICurrentLanguage language)
    : IQueryHandler<GetAdminRequests, PagedResult<AdminRequestListItemDto>>
{
    public async Task<PagedResult<AdminRequestListItemDto>> HandleAsync(GetAdminRequests query, CancellationToken cancellationToken)
    {
        var rows =
            from request in db.ServiceRequests.AsNoTracking()
            join user in db.Users.AsNoTracking() on request.CustomerId equals user.Id
            select new { Request = request, User = user };

        if (query.Status is { } status)
        {
            rows = rows.Where(r => r.Request.Status == status);
        }

        if (query.Kind is { } kind)
        {
            rows = rows.Where(r => r.Request.Kind == kind);
        }

        if (query.NeedsAttention is { } needsAttention)
        {
            rows = needsAttention
                ? rows.Where(r => r.Request.NeedsAttentionSince != null)
                : rows.Where(r => r.Request.NeedsAttentionSince == null);
        }

        if (query.CategoryId is { } categoryId)
        {
            rows = rows.Where(r => r.Request.CategoryId == categoryId);
        }

        if (query.CityId is { } cityId)
        {
            rows = rows.Where(r => r.Request.CityId == cityId);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            if (PhoneNumber.TryParse(search, out var phone))
            {
                rows = rows.Where(r => r.User.Phone == phone);
            }
            else
            {
                var lowered = search.ToLowerInvariant();
                rows = rows.Where(r => r.Request.Description.ToLower().Contains(lowered));
            }
        }

        rows = query.NeedsAttention == true
            ? rows.OrderBy(r => r.Request.NeedsAttentionSince).ThenBy(r => r.Request.Id)
            : rows.OrderByDescending(r => r.Request.CreatedAt).ThenByDescending(r => r.Request.Id);

        var total = await rows.CountAsync(cancellationToken);
        var page = await rows
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(r => new { r.Request, r.User.Phone, r.User.FullName })
            .ToListAsync(cancellationToken);

        var ids = page.Select(p => p.Request.Id).ToList();
        var counts = await db.ServiceRequests.AsNoTracking()
            .Where(r => ids.Contains(r.Id))
            .Select(r => new
            {
                r.Id,
                SentTo = r.Recipients.Count,
                Responded = r.Recipients.Count(x => x.Status == RecipientStatus.Responded),
                Declined = r.Recipients.Count(x => x.Status == RecipientStatus.Declined),
            })
            .ToDictionaryAsync(r => r.Id, cancellationToken);

        var lookup = await RequestLookup.LoadAsync(db, language, page.Select(p => p.Request), cancellationToken);
        var items = page
            .Select(p => new AdminRequestListItemDto(
                p.Request.Id,
                p.Request.Kind.ToString(),
                p.Request.Status.ToString(),
                lookup.Place(p.Request),
                RequestLookup.Excerpt(p.Request.Description),
                p.Request.CustomerId,
                p.Phone.Value,
                p.FullName,
                counts[p.Request.Id].SentTo,
                counts[p.Request.Id].Responded,
                counts[p.Request.Id].Declined,
                p.Request.AttentionReason?.ToString(),
                p.Request.NeedsAttentionSince,
                p.Request.CreatedAt))
            .ToList();
        return new PagedResult<AdminRequestListItemDto>(items, query.Page, query.PageSize, total);
    }
}

public sealed class GetAdminRequestHandler(IAppDbContext db, ICurrentLanguage language, FileDtoFactory files)
    : IQueryHandler<GetAdminRequest, AdminRequestDto>
{
    public async Task<AdminRequestDto> HandleAsync(GetAdminRequest query, CancellationToken cancellationToken)
    {
        var request = await AdminRequestLoader.LoadAsync(db, query.Id, cancellationToken);
        return await AdminRequestLoader.ToDtoAsync(db, language, files, request, cancellationToken);
    }
}

public sealed class AssignRequestHandler(IAppDbContext db, ICurrentLanguage language, FileDtoFactory files, TimeProvider clock)
    : ICommandHandler<AssignRequest, AdminRequestDto>
{
    public async Task<AdminRequestDto> HandleAsync(AssignRequest command, CancellationToken cancellationToken)
    {
        var request = await AdminRequestLoader.LoadAsync(db, command.Id, cancellationToken);
        var wanted = command.PartnerIds.Distinct().ToList();
        var available = await RequestMatching.Receiving(db)
            .Where(p => wanted.Contains(p.Id) && p.UserId != request.CustomerId)
            .Select(p => p.Id)
            .ToListAsync(cancellationToken);
        if (available.Count != wanted.Count)
        {
            throw new DomainException("request.partner_unavailable", "Some of these partners can't receive requests.");
        }

        var before = request.Recipients.Select(r => r.PartnerProfileId).ToHashSet();
        var now = clock.GetUtcNow();
        request.SendTo(available, RecipientSource.Manual, now);
        var added = request.Recipients.Select(r => r.PartnerProfileId).Where(id => !before.Contains(id)).ToList();
        await Notifier.ToPartnersAsync(db, added, NotificationType.RequestReceived, _ => $"/inbox/{request.Id}", null, now, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        return await AdminRequestLoader.ToDtoAsync(db, language, files, request, cancellationToken);
    }
}

public sealed class CancelRequestByStaffHandler(IAppDbContext db, ICurrentLanguage language, FileDtoFactory files, TimeProvider clock)
    : ICommandHandler<CancelRequestByStaff, AdminRequestDto>
{
    public async Task<AdminRequestDto> HandleAsync(CancelRequestByStaff command, CancellationToken cancellationToken)
    {
        var request = await AdminRequestLoader.LoadAsync(db, command.Id, cancellationToken);
        var now = clock.GetUtcNow();
        request.Cancel(command.Reason, now);
        await WaitingOffers.CloseAsync(db, request.Id, now, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        return await AdminRequestLoader.ToDtoAsync(db, language, files, request, cancellationToken);
    }
}

public sealed class FlagUnansweredRequestsHandler(IAppDbContext db, IOptions<RequestSettings> settings, TimeProvider clock)
    : ICommandHandler<FlagUnansweredRequests, int>
{
    public async Task<int> HandleAsync(FlagUnansweredRequests command, CancellationToken cancellationToken)
    {
        var now = clock.GetUtcNow();
        var window = settings.Value.ResponseWindow;
        var cutoff = now - window;

        // Cheap pre-filter in the database; the entity decides (it also checks the newest recipient's time).
        var candidates = await db.ServiceRequests
            .Include(r => r.Recipients)
            .Where(r => r.Status == RequestStatus.Open && r.NeedsAttentionSince == null)
            .Where(r => r.Recipients.Any() && r.Recipients.All(x => x.Status != RecipientStatus.Responded && x.SentAt <= cutoff))
            .ToListAsync(cancellationToken);

        var flagged = candidates.Count(r => r.FlagIfUnanswered(now, window));
        if (flagged > 0)
        {
            await db.SaveChangesAsync(cancellationToken);
        }

        return flagged;
    }
}

internal static class AdminRequestLoader
{
    public static async Task<ServiceRequest> LoadAsync(IAppDbContext db, Guid id, CancellationToken cancellationToken) =>
        await db.ServiceRequests
            .Include(r => r.Recipients)
            .Include(r => r.Media)
            .SingleOrDefaultAsync(r => r.Id == id, cancellationToken)
        ?? throw MyRequests.NotFound();

    public static async Task<AdminRequestDto> ToDtoAsync(IAppDbContext db, ICurrentLanguage language, FileDtoFactory files, ServiceRequest request, CancellationToken cancellationToken)
    {
        var lookup = await RequestLookup.LoadAsync(db, language, [request], cancellationToken);
        var customer = await db.Users.AsNoTracking().SingleAsync(u => u.Id == request.CustomerId, cancellationToken);
        var partnerIds = request.Recipients.Select(r => r.PartnerProfileId).ToList();
        var partners = await db.PartnerProfiles.AsNoTracking()
            .Where(p => partnerIds.Contains(p.Id))
            .Select(p => new { p.Id, p.DisplayName, p.Status })
            .ToDictionaryAsync(p => p.Id, cancellationToken);

        var recipients = request.Recipients
            .OrderBy(r => r.SentAt).ThenBy(r => r.Id)
            .Select(r => new AdminRecipientDto(
                r.PartnerProfileId,
                partners[r.PartnerProfileId].DisplayName,
                partners[r.PartnerProfileId].Status.ToString(),
                r.Source.ToString(),
                r.Status.ToString(),
                r.SentAt,
                r.ViewedAt,
                r.DeclinedAt,
                r.DeclineReason,
                r.RespondedAt))
            .ToList();

        return new AdminRequestDto(
            request.Id,
            request.Kind.ToString(),
            request.Status.ToString(),
            lookup.Place(request),
            request.Description,
            request.PreferredDate,
            request.TimeNote,
            request.BudgetMin,
            request.BudgetMax,
            await RequestLookup.MediaAsync(db, files, request, cancellationToken),
            new RequestCustomerDto(customer.Id, customer.Phone.Value, customer.FullName, customer.Email, customer.IsBlocked),
            recipients,
            request.AttentionReason?.ToString(),
            request.NeedsAttentionSince,
            request.CreatedAt,
            request.CancelledAt,
            request.CancelReason);
    }
}
