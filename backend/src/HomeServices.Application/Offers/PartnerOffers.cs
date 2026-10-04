using FluentValidation;
using HomeServices.Application.Abstractions;
using HomeServices.Application.Common;
using HomeServices.Application.Messaging;
using HomeServices.Application.Notifications;
using HomeServices.Application.Requests;
using HomeServices.Domain;
using HomeServices.Domain.Notifications;
using HomeServices.Domain.Offers;
using Microsoft.EntityFrameworkCore;

namespace HomeServices.Application.Offers;

/// <summary>
/// The signed-in partner sends an offer on a request they received (and haven't declined). A work offer describes
/// the job; a visit offer proposes an assessment visit (fee 0 = free). The offer is valid for
/// <see cref="ValidDays"/> days (default 7). One waiting offer of each kind per partner and request.
/// </summary>
public sealed record SendOffer(
    Guid RequestId,
    OfferKind Kind,
    string Summary,
    IReadOnlyList<OfferLine>? Lines,
    int Price,
    bool MaterialsIncluded,
    string? MaterialsNote,
    DateOnly? StartDate,
    int? DurationDays,
    DateTimeOffset? VisitAt,
    IReadOnlyList<OfferStageTerms>? Stages,
    int? ValidDays) : ICommand<OfferDto>
{
    public const int DefaultValidDays = 7;
    public const int MaxValidDays = 30;
}

/// <summary>The partner takes back an offer the customer hasn't decided on.</summary>
public sealed record WithdrawOffer(Guid Id) : ICommand<OfferDto>;

/// <summary>Offers the signed-in partner sent, newest first.</summary>
public sealed record GetMyOffers(OfferStatus? Status = null, int Page = 1, int PageSize = GetMyOffers.DefaultPageSize)
    : IQuery<PagedResult<MyOfferListItemDto>>
{
    public const int DefaultPageSize = 20;
    public const int MaxPageSize = 100;
}

public sealed class SendOfferValidator : AbstractValidator<SendOffer>
{
    public SendOfferValidator(TimeProvider clock)
    {
        RuleFor(x => x.Kind).IsInEnum().WithErrorCode("kind.invalid");

        RuleFor(x => x.Summary)
            .Must(text => !string.IsNullOrWhiteSpace(text)).WithErrorCode("summary.required")
            .Must(text => text.Trim().Length is >= Offer.SummaryMinLength and <= Offer.SummaryMaxLength).WithErrorCode("summary.length")
            .When(x => !string.IsNullOrWhiteSpace(x.Summary), ApplyConditionTo.CurrentValidator);
        RuleFor(x => x.MaterialsNote)
            .Must(note => note!.Trim().Length <= Offer.MaterialsNoteMaxLength).WithErrorCode("materials_note.too_long")
            .When(x => x.MaterialsNote is not null);
        RuleFor(x => x.ValidDays)
            .InclusiveBetween(1, SendOffer.MaxValidDays).WithErrorCode("valid_days.invalid")
            .When(x => x.ValidDays is not null);

        When(x => x.Kind == OfferKind.Work, () =>
        {
            RuleFor(x => x.Price).InclusiveBetween(1, Offer.MaxPrice).WithErrorCode("price.invalid");
            RuleFor(x => x.Lines)
                .Must(lines => lines!.Count <= Offer.MaxLines).WithErrorCode("lines.too_many")
                .Must(lines => lines!.All(l => !string.IsNullOrWhiteSpace(l.Title) && l.Title.Trim().Length <= Offer.LineMaxLength))
                .WithErrorCode("lines.invalid")
                .When(x => x.Lines is { Count: > 0 });

            // Yesterday is allowed: the partner's day may not have ended in UTC terms.
            RuleFor(x => x.StartDate)
                .Must(date => date >= DateOnly.FromDateTime(clock.GetUtcNow().UtcDateTime).AddDays(-1)).WithErrorCode("start_date.past")
                .When(x => x.StartDate is not null);
            RuleFor(x => x.DurationDays)
                .InclusiveBetween(1, Offer.MaxDurationDays).WithErrorCode("duration_days.invalid")
                .When(x => x.DurationDays is not null);
            RuleFor(x => x.VisitAt).Null().WithErrorCode("visit_at.not_allowed");

            RuleFor(x => x.Stages)
                .Must(stages => stages!.Count <= Offer.MaxStages).WithErrorCode("stages.too_many")
                .Must(stages => stages!.All(s =>
                    s.Amount > 0 && Enum.IsDefined(s.Purpose) && (s.Title is null || s.Title.Trim().Length <= Offer.StageTitleMaxLength)))
                .WithErrorCode("stages.invalid")
                .Must(stages => stages!.Take(stages!.Count - 1).All(s => s.Purpose != PaymentPurpose.Final)).WithErrorCode("stages.final_last")
                .Must((command, stages) => stages!.Sum(s => (long)s.Amount) == command.Price).WithErrorCode("stages.sum_mismatch")
                .When(x => x.Stages is { Count: > 0 });
        });

        When(x => x.Kind == OfferKind.Visit, () =>
        {
            RuleFor(x => x.Price).InclusiveBetween(0, Offer.MaxPrice).WithErrorCode("price.invalid");
            RuleFor(x => x.VisitAt)
                .Cascade(CascadeMode.Stop)
                .NotNull().WithErrorCode("visit_at.required")
                .Must(at => at > clock.GetUtcNow()).WithErrorCode("visit_at.past");
            RuleFor(x => x.Lines).Must(lines => lines is null or { Count: 0 }).WithErrorCode("lines.not_allowed");
            RuleFor(x => x.Stages).Must(stages => stages is null or { Count: 0 }).WithErrorCode("stages.not_allowed");
            RuleFor(x => x.StartDate).Null().WithErrorCode("start_date.not_allowed");
            RuleFor(x => x.DurationDays).Null().WithErrorCode("duration_days.not_allowed");
        });
    }
}

public sealed class GetMyOffersValidator : AbstractValidator<GetMyOffers>
{
    public GetMyOffersValidator()
    {
        RuleFor(x => x.Status).IsInEnum().WithErrorCode("status.invalid");
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1).WithErrorCode("page.invalid");
        RuleFor(x => x.PageSize).InclusiveBetween(1, GetMyOffers.MaxPageSize).WithErrorCode("page_size.invalid");
    }
}

public sealed class SendOfferHandler(IAppDbContext db, ICurrentUser currentUser, TimeProvider clock)
    : ICommandHandler<SendOffer, OfferDto>
{
    public async Task<OfferDto> HandleAsync(SendOffer command, CancellationToken cancellationToken)
    {
        var now = clock.GetUtcNow();
        var (request, partnerId) = await Inbox.LoadAsync(db, currentUser, command.RequestId, cancellationToken);
        if (!await RequestMatching.Available(db).AnyAsync(p => p.Id == partnerId, cancellationToken))
        {
            throw new DomainException("offer.partner_unavailable", "Your partner profile can't send offers right now.");
        }

        var mine = await db.Offers
            .Where(o => o.RequestId == request.Id && o.PartnerProfileId == partnerId && o.Kind == command.Kind)
            .ToListAsync(cancellationToken);
        foreach (var stale in mine)
        {
            stale.Expire(now);
        }

        if (mine.Any(o => o.Status == OfferStatus.Sent))
        {
            throw new DomainException("offer.already_sent", "You already have a waiting offer of this kind; withdraw it to send a new one.");
        }

        if (command.Kind == OfferKind.Visit && mine.Any(o => o.Status == OfferStatus.Accepted))
        {
            throw new DomainException("offer.visit_already_agreed", "The customer already accepted your visit.");
        }

        var terms = new OfferTerms(
            command.Summary,
            command.Lines ?? [],
            command.Price,
            command.MaterialsIncluded,
            command.MaterialsNote,
            command.StartDate,
            command.DurationDays,
            command.VisitAt,
            command.Stages ?? []);
        var offer = Offer.Create(request.Id, partnerId, command.Kind, terms, now.AddDays(command.ValidDays ?? SendOffer.DefaultValidDays), now);
        request.MarkResponded(partnerId, now);

        db.Offers.Add(offer);
        var partnerName = await db.PartnerProfiles.AsNoTracking().Where(p => p.Id == partnerId).Select(p => p.DisplayName).SingleAsync(cancellationToken);
        Notifier.Add(
            db,
            request.CustomerId,
            NotificationType.OfferReceived,
            $"/requests/{request.Id}",
            new Dictionary<string, string?>
            {
                ["name"] = partnerName,
                ["kind"] = offer.Kind.ToString(),
                ["price"] = offer.Price.ToString(System.Globalization.CultureInfo.InvariantCulture),
            },
            now);
        await db.SaveChangesAsync(cancellationToken);
        return await OfferViews.ToDtoAsync(db, offer, now, cancellationToken);
    }
}

public sealed class WithdrawOfferHandler(IAppDbContext db, ICurrentUser currentUser, TimeProvider clock)
    : ICommandHandler<WithdrawOffer, OfferDto>
{
    public async Task<OfferDto> HandleAsync(WithdrawOffer command, CancellationToken cancellationToken)
    {
        var partnerId = await Inbox.MyPartnerIdAsync(db, currentUser, cancellationToken);
        var offer = await OfferViews.LoadAsync(db, command.Id, cancellationToken);
        if (offer.PartnerProfileId != partnerId)
        {
            throw OfferViews.NotFound();
        }

        var now = clock.GetUtcNow();
        offer.Withdraw(now);
        await db.SaveChangesAsync(cancellationToken);
        return await OfferViews.ToDtoAsync(db, offer, now, cancellationToken);
    }
}

public sealed class GetMyOffersHandler(IAppDbContext db, ICurrentUser currentUser, ICurrentLanguage language, TimeProvider clock)
    : IQueryHandler<GetMyOffers, PagedResult<MyOfferListItemDto>>
{
    public async Task<PagedResult<MyOfferListItemDto>> HandleAsync(GetMyOffers query, CancellationToken cancellationToken)
    {
        var partnerId = await Inbox.MyPartnerIdAsync(db, currentUser, cancellationToken);
        var now = clock.GetUtcNow();
        var offers = db.Offers.AsNoTracking().Where(o => o.PartnerProfileId == partnerId);
        offers = query.Status switch
        {
            null => offers,
            OfferStatus.Sent => offers.Where(o => o.Status == OfferStatus.Sent && o.ExpiresAt > now),
            OfferStatus.Expired => offers.Where(o => o.Status == OfferStatus.Expired || (o.Status == OfferStatus.Sent && o.ExpiresAt <= now)),
            var status => offers.Where(o => o.Status == status),
        };

        var total = await offers.CountAsync(cancellationToken);
        var page = await offers
            .OrderByDescending(o => o.CreatedAt).ThenByDescending(o => o.Id)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);

        var requestIds = page.Select(o => o.RequestId).Distinct().ToList();
        var requests = await db.ServiceRequests.AsNoTracking()
            .Where(r => requestIds.Contains(r.Id))
            .ToDictionaryAsync(r => r.Id, cancellationToken);
        var lookup = await RequestLookup.LoadAsync(db, language, requests.Values, cancellationToken);

        var items = page
            .Select(o => new MyOfferListItemDto(
                o.Id,
                o.RequestId,
                o.Kind.ToString(),
                o.StatusAt(now).ToString(),
                lookup.Place(requests[o.RequestId]),
                RequestLookup.Excerpt(requests[o.RequestId].Description),
                o.Price,
                o.VisitAt,
                o.ExpiresAt,
                o.CreatedAt,
                o.OrderId))
            .ToList();
        return new PagedResult<MyOfferListItemDto>(items, query.Page, query.PageSize, total);
    }
}
