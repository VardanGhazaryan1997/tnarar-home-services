using FluentValidation;
using HomeServices.Application.Abstractions;
using HomeServices.Application.Common;
using HomeServices.Application.Errors;
using HomeServices.Application.Files;
using HomeServices.Application.Messaging;
using HomeServices.Application.Notifications;
using HomeServices.Application.Offers;
using HomeServices.Application.Partners;
using HomeServices.Domain;
using HomeServices.Domain.Files;
using HomeServices.Domain.Notifications;
using HomeServices.Domain.Requests;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace HomeServices.Application.Requests;

/// <summary>
/// The signed-in customer asks for work. A direct request (<see cref="PartnerId"/> set) goes to that partner;
/// an open request goes to matching partners (see <see cref="RequestMatching"/>), or to the operator queue
/// when nobody matches. Photos and videos are uploaded with /api/v1/files first.
/// </summary>
public sealed record CreateRequest(
    RequestKind Kind,
    Guid? PartnerId,
    Guid CategoryId,
    Guid CityId,
    Guid? DistrictId,
    string Description,
    DateOnly? PreferredDate,
    string? TimeNote,
    int? BudgetMin,
    int? BudgetMax,
    IReadOnlyList<Guid>? MediaFileIds) : ICommand<MyRequestDto>;

/// <summary>The customer's requests, newest first.</summary>
public sealed record GetMyRequests(RequestStatus? Status = null, int Page = 1, int PageSize = GetMyRequests.DefaultPageSize)
    : IQuery<PagedResult<MyRequestListItemDto>>
{
    public const int DefaultPageSize = 20;
    public const int MaxPageSize = 100;
}

public sealed record GetMyRequest(Guid Id) : IQuery<MyRequestDto>;

/// <summary>The customer withdraws an open request.</summary>
public sealed record CancelMyRequest(Guid Id, string? Reason) : ICommand<MyRequestDto>;

public sealed class CreateRequestValidator : AbstractValidator<CreateRequest>
{
    public CreateRequestValidator(IAppDbContext db, ICurrentUser currentUser, TimeProvider clock)
    {
        RuleFor(x => x.Kind).IsInEnum().WithErrorCode("kind.invalid");
        RuleFor(x => x.PartnerId).NotNull().WithErrorCode("partner_id.required").When(x => x.Kind == RequestKind.Direct);
        RuleFor(x => x.PartnerId).Null().WithErrorCode("partner_id.not_allowed").When(x => x.Kind == RequestKind.Open);

        RuleFor(x => x.CategoryId)
            .MustAsync((id, ct) => db.Categories.AnyAsync(c => c.Id == id && c.IsActive, ct))
            .WithErrorCode("category.invalid");
        RuleFor(x => x.CityId)
            .MustAsync((id, ct) => db.Cities.AnyAsync(c => c.Id == id && c.IsActive, ct))
            .WithErrorCode("city.invalid");
        RuleFor(x => x.DistrictId)
            .MustAsync((command, districtId, ct) =>
                db.Cities.AnyAsync(c => c.Id == command.CityId && c.Districts.Any(d => d.Id == districtId && d.IsActive), ct))
            .WithErrorCode("district.invalid")
            .When(x => x.DistrictId is not null);

        RuleFor(x => x.Description)
            .Must(text => !string.IsNullOrWhiteSpace(text)).WithErrorCode("description.required")
            .Must(text => text.Trim().Length is >= ServiceRequest.DescriptionMinLength and <= ServiceRequest.DescriptionMaxLength)
            .WithErrorCode("description.length")
            .When(x => !string.IsNullOrWhiteSpace(x.Description), ApplyConditionTo.CurrentValidator);
        RuleFor(x => x.TimeNote)
            .Must(note => note!.Trim().Length <= ServiceRequest.TimeNoteMaxLength).WithErrorCode("time_note.too_long")
            .When(x => x.TimeNote is not null);

        // Yesterday is allowed: the customer's day may not have ended in UTC terms.
        RuleFor(x => x.PreferredDate)
            .Must(date => date >= DateOnly.FromDateTime(clock.GetUtcNow().UtcDateTime).AddDays(-1))
            .WithErrorCode("preferred_date.past")
            .When(x => x.PreferredDate is not null);

        RuleFor(x => x.BudgetMin).InclusiveBetween(0, ServiceRequest.MaxBudget).WithErrorCode("budget.invalid");
        RuleFor(x => x.BudgetMax).InclusiveBetween(0, ServiceRequest.MaxBudget).WithErrorCode("budget.invalid");
        RuleFor(x => x.BudgetMax)
            .Must((command, max) => max >= command.BudgetMin).WithErrorCode("budget.range_invalid")
            .When(x => x.BudgetMin is not null && x.BudgetMax is not null);

        RuleFor(x => x.MediaFileIds)
            .Must(ids => ids!.Distinct().Count() <= ServiceRequest.MaxMedia).WithErrorCode("media.too_many")
            .MustAsync(async (ids, ct) => await RequestFiles.AllUsableAsync(db, currentUser, ids!, ct)).WithErrorCode("media.invalid")
            .When(x => x.MediaFileIds is { Count: > 0 });
    }
}

public sealed class GetMyRequestsValidator : AbstractValidator<GetMyRequests>
{
    public GetMyRequestsValidator()
    {
        RuleFor(x => x.Status).IsInEnum().WithErrorCode("status.invalid");
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1).WithErrorCode("page.invalid");
        RuleFor(x => x.PageSize).InclusiveBetween(1, GetMyRequests.MaxPageSize).WithErrorCode("page_size.invalid");
    }
}

public sealed class CancelMyRequestValidator : AbstractValidator<CancelMyRequest>
{
    public CancelMyRequestValidator() =>
        RuleFor(x => x.Reason)
            .Must(reason => reason!.Trim().Length <= ServiceRequest.CancelReasonMaxLength).WithErrorCode("reason.too_long")
            .When(x => x.Reason is not null);
}

public sealed class CreateRequestHandler(
    IAppDbContext db,
    ICurrentUser currentUser,
    ICurrentLanguage language,
    FileDtoFactory files,
    IOptions<RequestSettings> settings,
    TimeProvider clock)
    : ICommandHandler<CreateRequest, MyRequestDto>
{
    public async Task<MyRequestDto> HandleAsync(CreateRequest command, CancellationToken cancellationToken)
    {
        var customerId = MyPartnerProfile.UserId(currentUser);
        var now = clock.GetUtcNow();
        var request = ServiceRequest.Create(
            customerId,
            command.Kind,
            command.CategoryId,
            command.CityId,
            command.DistrictId,
            command.Description,
            command.PreferredDate,
            command.TimeNote,
            command.BudgetMin,
            command.BudgetMax);
        foreach (var fileId in command.MediaFileIds ?? [])
        {
            request.AddMedia(fileId);
        }

        if (command.Kind == RequestKind.Direct)
        {
            var partner = await RequestMatching.Receiving(db).SingleOrDefaultAsync(p => p.Id == command.PartnerId, cancellationToken)
                ?? throw new DomainException("request.partner_unavailable", "This partner can't receive requests right now.");
            if (partner.UserId == customerId)
            {
                throw new DomainException("request.own_partner", "You can't send a request to your own partner profile.");
            }

            request.SendTo([partner.Id], RecipientSource.Direct, now);
        }
        else
        {
            var matches = await RequestMatching.MatchAsync(db, request, settings.Value.MaxRecipients, cancellationToken);
            if (request.SendTo(matches, RecipientSource.Matched, now) == 0)
            {
                request.FlagForOperator(AttentionReason.NoMatchingPartners, now);
            }
        }

        db.ServiceRequests.Add(request);
        await Notifier.ToPartnersAsync(
            db, request.Recipients.Select(r => r.PartnerProfileId).ToList(), NotificationType.RequestReceived, _ => $"/inbox/{request.Id}", null, now, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        return await MyRequests.ToDtoAsync(db, language, files, request, cancellationToken);
    }
}

public sealed class GetMyRequestsHandler(IAppDbContext db, ICurrentUser currentUser, ICurrentLanguage language)
    : IQueryHandler<GetMyRequests, PagedResult<MyRequestListItemDto>>
{
    public async Task<PagedResult<MyRequestListItemDto>> HandleAsync(GetMyRequests query, CancellationToken cancellationToken)
    {
        var customerId = MyPartnerProfile.UserId(currentUser);
        var requests = db.ServiceRequests.AsNoTracking().Where(r => r.CustomerId == customerId);
        if (query.Status is { } status)
        {
            requests = requests.Where(r => r.Status == status);
        }

        var total = await requests.CountAsync(cancellationToken);
        var page = await requests
            .Include(r => r.Recipients)
            .OrderByDescending(r => r.CreatedAt).ThenByDescending(r => r.Id)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);

        var lookup = await RequestLookup.LoadAsync(db, language, page, cancellationToken);
        var items = page
            .Select(r => new MyRequestListItemDto(
                r.Id,
                r.Kind.ToString(),
                r.Status.ToString(),
                lookup.Place(r),
                RequestLookup.Excerpt(r.Description),
                r.PreferredDate,
                r.Recipients.Count,
                r.Recipients.Count(x => x.Status == RecipientStatus.Responded),
                r.NeedsAttention,
                r.CreatedAt))
            .ToList();
        return new PagedResult<MyRequestListItemDto>(items, query.Page, query.PageSize, total);
    }
}

public sealed class GetMyRequestHandler(IAppDbContext db, ICurrentUser currentUser, ICurrentLanguage language, FileDtoFactory files)
    : IQueryHandler<GetMyRequest, MyRequestDto>
{
    public async Task<MyRequestDto> HandleAsync(GetMyRequest query, CancellationToken cancellationToken)
    {
        var request = await MyRequests.LoadAsync(db, currentUser, query.Id, cancellationToken);
        return await MyRequests.ToDtoAsync(db, language, files, request, cancellationToken);
    }
}

public sealed class CancelMyRequestHandler(IAppDbContext db, ICurrentUser currentUser, ICurrentLanguage language, FileDtoFactory files, TimeProvider clock)
    : ICommandHandler<CancelMyRequest, MyRequestDto>
{
    public async Task<MyRequestDto> HandleAsync(CancelMyRequest command, CancellationToken cancellationToken)
    {
        var request = await MyRequests.LoadAsync(db, currentUser, command.Id, cancellationToken);
        var now = clock.GetUtcNow();
        request.Cancel(command.Reason, now);
        await WaitingOffers.CloseAsync(db, request.Id, now, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        return await MyRequests.ToDtoAsync(db, language, files, request, cancellationToken);
    }
}

internal static class MyRequests
{
    /// <summary>The signed-in customer's request; someone else's is "not found", not "forbidden", so ids reveal nothing.</summary>
    public static async Task<ServiceRequest> LoadAsync(IAppDbContext db, ICurrentUser currentUser, Guid id, CancellationToken cancellationToken)
    {
        var customerId = MyPartnerProfile.UserId(currentUser);
        return await db.ServiceRequests
            .Include(r => r.Recipients)
            .Include(r => r.Media)
            .SingleOrDefaultAsync(r => r.Id == id && r.CustomerId == customerId, cancellationToken)
            ?? throw NotFound();
    }

    public static NotFoundException NotFound() => new("Request not found.", "request.not_found");

    public static async Task<MyRequestDto> ToDtoAsync(IAppDbContext db, ICurrentLanguage language, FileDtoFactory files, ServiceRequest request, CancellationToken cancellationToken)
    {
        var lookup = await RequestLookup.LoadAsync(db, language, [request], cancellationToken);

        // The customer sees the partner they chose, and partners who responded; not who declined an open request.
        var shown = request.Recipients
            .Where(r => (request.Kind == RequestKind.Direct && r.Source == RecipientSource.Direct) || r.Status == RecipientStatus.Responded)
            .ToList();
        var partnerIds = shown.Select(r => r.PartnerProfileId).ToList();
        var partners = await db.PartnerProfiles.AsNoTracking()
            .Where(p => partnerIds.Contains(p.Id))
            .Select(p => new { p.Id, p.DisplayName, p.Slug })
            .ToDictionaryAsync(p => p.Id, cancellationToken);

        return new MyRequestDto(
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
            request.Recipients.Count,
            shown
                .Where(r => partners.ContainsKey(r.PartnerProfileId))
                .Select(r => new MyRequestPartnerDto(r.PartnerProfileId, partners[r.PartnerProfileId].DisplayName, partners[r.PartnerProfileId].Slug, r.Status.ToString()))
                .ToList(),
            request.NeedsAttention,
            request.CreatedAt,
            request.CancelledAt,
            request.CancelReason);
    }
}

internal static class RequestFiles
{
    /// <summary>Every file is a ready photo or video uploaded by the signed-in Portal user.</summary>
    public static async Task<bool> AllUsableAsync(IAppDbContext db, ICurrentUser currentUser, IReadOnlyList<Guid> fileIds, CancellationToken cancellationToken)
    {
        if (currentUser.IsStaff || currentUser.UserId is not { } userId)
        {
            return false;
        }

        var wanted = fileIds.Distinct().ToList();
        var usable = await db.Files.AsNoTracking().CountAsync(
            f => wanted.Contains(f.Id)
                && f.OwnerType == FileOwnerType.User
                && f.OwnerId == userId
                && f.Status == FileStatus.Ready
                && (f.Kind == FileKind.Image || f.Kind == FileKind.Video),
            cancellationToken);
        return usable == wanted.Count;
    }
}
