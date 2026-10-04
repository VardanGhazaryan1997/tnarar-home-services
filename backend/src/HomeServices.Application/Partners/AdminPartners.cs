using FluentValidation;
using HomeServices.Application.Abstractions;
using HomeServices.Application.Common;
using HomeServices.Application.Errors;
using HomeServices.Application.Messaging;
using HomeServices.Application.Notifications;
using HomeServices.Domain.Identity;
using HomeServices.Domain.Notifications;
using HomeServices.Domain.Partners;
using Microsoft.EntityFrameworkCore;

namespace HomeServices.Application.Partners;

/// <summary>
/// Partner profiles for the Back Office. With <see cref="Status"/> = UnderReview this is the review
/// queue, oldest submission first; otherwise newest first. <see cref="Search"/> matches the name, or the
/// owner's phone number when it is one.
/// </summary>
public sealed record GetAdminPartners(
    PartnerStatus? Status = null,
    PartnerType? Type = null,
    string? Search = null,
    int Page = 1,
    int PageSize = GetAdminPartners.DefaultPageSize) : IQuery<PagedResult<AdminPartnerListItemDto>>
{
    public const int DefaultPageSize = 20;
    public const int MaxPageSize = 100;
}

public sealed record GetAdminPartner(Guid Id) : IQuery<AdminPartnerDto>;

/// <summary>A staff decision on a profile. <see cref="Comment"/> is required except for approving and reinstating.</summary>
public sealed record DecideOnPartner(Guid Id, PartnerDecision Decision, string? Comment = null) : ICommand<AdminPartnerDto>;

public enum PartnerDecision
{
    Approve = 1,
    RequestChanges = 2,
    Reject = 3,
    Suspend = 4,
    Reinstate = 5,
}

public sealed record AdminPartnerListItemDto(
    Guid Id,
    string DisplayName,
    string Type,
    string Status,
    Guid UserId,
    string Phone,
    string? OwnerName,
    int ServiceCount,
    int AreaCount,
    DateTimeOffset? SubmittedAt,
    DateTimeOffset CreatedAt);

public sealed record PartnerOwnerDto(Guid UserId, string Phone, string? FullName, string? Email, bool IsBlocked);

/// <summary>One step of the review history. <see cref="ActorName"/> is the staff member's or partner's name.</summary>
public sealed record PartnerStatusChangeDto(
    int Sequence,
    string FromStatus,
    string ToStatus,
    string? Comment,
    DateTimeOffset At,
    string? ActorId,
    string? ActorName);

/// <summary>Everything a reviewer needs: the profile (documents included), its owner and the review history, newest first.</summary>
public sealed record AdminPartnerDto(PartnerProfileDto Profile, PartnerOwnerDto Owner, IReadOnlyList<PartnerStatusChangeDto> History);

public sealed class GetAdminPartnersValidator : AbstractValidator<GetAdminPartners>
{
    public GetAdminPartnersValidator()
    {
        RuleFor(x => x.Status).IsInEnum().WithErrorCode("status.invalid");
        RuleFor(x => x.Type).IsInEnum().WithErrorCode("type.invalid");
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1).WithErrorCode("page.invalid");
        RuleFor(x => x.PageSize).InclusiveBetween(1, GetAdminPartners.MaxPageSize).WithErrorCode("page_size.invalid");
        RuleFor(x => x.Search).MaximumLength(100).WithErrorCode("search.too_long");
    }
}

public sealed class DecideOnPartnerValidator : AbstractValidator<DecideOnPartner>
{
    public DecideOnPartnerValidator()
    {
        RuleFor(x => x.Decision).IsInEnum().WithErrorCode("decision.invalid");
        RuleFor(x => x.Comment)
            .Must(comment => !string.IsNullOrWhiteSpace(comment)).WithErrorCode("comment.required")
            .When(x => x.Decision is PartnerDecision.RequestChanges or PartnerDecision.Reject or PartnerDecision.Suspend);
        RuleFor(x => x.Comment)
            .Must(comment => comment!.Trim().Length <= PartnerStatusChange.CommentMaxLength).WithErrorCode("comment.too_long")
            .When(x => x.Comment is not null);
    }
}

public sealed class GetAdminPartnersHandler(IAppDbContext db) : IQueryHandler<GetAdminPartners, PagedResult<AdminPartnerListItemDto>>
{
    public async Task<PagedResult<AdminPartnerListItemDto>> HandleAsync(GetAdminPartners query, CancellationToken cancellationToken)
    {
        var rows =
            from profile in db.PartnerProfiles.AsNoTracking()
            join user in db.Users.AsNoTracking() on profile.UserId equals user.Id
            select new { Profile = profile, User = user };

        if (query.Status is { } status)
        {
            rows = rows.Where(r => r.Profile.Status == status);
        }

        if (query.Type is { } type)
        {
            rows = rows.Where(r => r.Profile.Type == type);
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
                rows = rows.Where(r => r.Profile.DisplayName.ToLower().Contains(lowered));
            }
        }

        rows = query.Status == PartnerStatus.UnderReview
            ? rows.OrderBy(r => r.Profile.SubmittedAt).ThenBy(r => r.Profile.Id)
            : rows.OrderByDescending(r => r.Profile.CreatedAt).ThenByDescending(r => r.Profile.Id);

        var total = await rows.CountAsync(cancellationToken);
        var page = await rows
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(r => new
            {
                r.Profile.Id,
                r.Profile.DisplayName,
                r.Profile.Type,
                r.Profile.Status,
                r.Profile.UserId,
                r.User.Phone,
                r.User.FullName,
                ServiceCount = r.Profile.Services.Count,
                AreaCount = r.Profile.Areas.Count,
                r.Profile.SubmittedAt,
                r.Profile.CreatedAt,
            })
            .ToListAsync(cancellationToken);

        var items = page
            .Select(p => new AdminPartnerListItemDto(
                p.Id, p.DisplayName, p.Type.ToString(), p.Status.ToString(), p.UserId, p.Phone.Value, p.FullName,
                p.ServiceCount, p.AreaCount, p.SubmittedAt, p.CreatedAt))
            .ToList();
        return new PagedResult<AdminPartnerListItemDto>(items, query.Page, query.PageSize, total);
    }
}

public sealed class GetAdminPartnerHandler(IAppDbContext db, PartnerProfileDtoFactory dtos) : IQueryHandler<GetAdminPartner, AdminPartnerDto>
{
    public async Task<AdminPartnerDto> HandleAsync(GetAdminPartner query, CancellationToken cancellationToken)
    {
        var profile = await AdminPartnerLoader.LoadAsync(db, query.Id, cancellationToken);
        return await AdminPartnerLoader.ToDtoAsync(db, dtos, profile, cancellationToken);
    }
}

public sealed class DecideOnPartnerHandler(IAppDbContext db, PartnerProfileDtoFactory dtos, TimeProvider clock)
    : ICommandHandler<DecideOnPartner, AdminPartnerDto>
{
    public async Task<AdminPartnerDto> HandleAsync(DecideOnPartner command, CancellationToken cancellationToken)
    {
        var profile = await AdminPartnerLoader.LoadAsync(db, command.Id, cancellationToken);
        switch (command.Decision)
        {
            case PartnerDecision.Approve:
                profile.Approve(clock.GetUtcNow());
                break;
            case PartnerDecision.RequestChanges:
                profile.RequestChanges(command.Comment!);
                break;
            case PartnerDecision.Reject:
                profile.Reject(command.Comment!);
                break;
            case PartnerDecision.Suspend:
                profile.Suspend(command.Comment!);
                break;
            default:
                profile.Reinstate();
                break;
        }

        NotificationType? notice = command.Decision switch
        {
            PartnerDecision.Approve => NotificationType.PartnerApproved,
            PartnerDecision.RequestChanges => NotificationType.PartnerNeedsChanges,
            PartnerDecision.Reject => NotificationType.PartnerRejected,
            _ => null,
        };
        if (notice is { } type)
        {
            Notifier.Add(db, profile.UserId, type, "/partner", null, clock.GetUtcNow());
        }

        await db.SaveChangesAsync(cancellationToken);
        return await AdminPartnerLoader.ToDtoAsync(db, dtos, profile, cancellationToken);
    }
}

internal static class AdminPartnerLoader
{
    public static async Task<PartnerProfile> LoadAsync(IAppDbContext db, Guid id, CancellationToken cancellationToken) =>
        await db.PartnerProfiles
            .Include(p => p.Services)
            .Include(p => p.Areas)
            .Include(p => p.Media)
            .Include(p => p.StatusChanges)
            .SingleOrDefaultAsync(p => p.Id == id, cancellationToken)
        ?? throw new NotFoundException("Partner profile not found.", "partner.not_found");

    public static async Task<AdminPartnerDto> ToDtoAsync(IAppDbContext db, PartnerProfileDtoFactory dtos, PartnerProfile profile, CancellationToken cancellationToken)
    {
        var profileDto = await MyPartnerProfile.ToDtoAsync(db, dtos, profile, cancellationToken);
        var owner = await db.Users.AsNoTracking().SingleAsync(u => u.Id == profile.UserId, cancellationToken);
        var names = await ActorNamesAsync(db, profile.StatusChanges.Select(c => c.CreatedBy), cancellationToken);

        var history = profile.StatusChanges
            .OrderByDescending(c => c.Sequence)
            .Select(c => new PartnerStatusChangeDto(
                c.Sequence,
                c.FromStatus.ToString(),
                c.ToStatus.ToString(),
                c.Comment,
                c.CreatedAt,
                c.CreatedBy,
                c.CreatedBy is not null && names.TryGetValue(c.CreatedBy, out var name) ? name : null))
            .ToList();

        return new AdminPartnerDto(
            profileDto,
            new PartnerOwnerDto(owner.Id, owner.Phone.Value, owner.FullName, owner.Email, owner.IsBlocked),
            history);
    }

    // Actors are staff members (decisions) or the partner (submitting).
    private static async Task<Dictionary<string, string>> ActorNamesAsync(IAppDbContext db, IEnumerable<string?> actorIds, CancellationToken cancellationToken)
    {
        var ids = actorIds
            .Select(id => Guid.TryParse(id, out var guid) ? guid : Guid.Empty)
            .Where(id => id != Guid.Empty)
            .Distinct()
            .ToList();
        var names = new Dictionary<string, string>();
        if (ids.Count == 0)
        {
            return names;
        }

        var staff = await db.StaffUsers.AsNoTracking().Where(s => ids.Contains(s.Id)).Select(s => new { s.Id, s.FullName }).ToListAsync(cancellationToken);
        staff.ForEach(s => names[s.Id.ToString()] = s.FullName);
        var users = await db.Users.AsNoTracking().Where(u => ids.Contains(u.Id) && u.FullName != null).Select(u => new { u.Id, u.FullName }).ToListAsync(cancellationToken);
        users.ForEach(u => names.TryAdd(u.Id.ToString(), u.FullName!));
        return names;
    }
}
