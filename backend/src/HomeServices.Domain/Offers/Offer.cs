using HomeServices.Domain.Common;

namespace HomeServices.Domain.Offers;

/// <summary>
/// A partner's proposal on a request they received: the work (scope, price, dates, payment stages) or an
/// assessment visit. It waits for the customer until <see cref="ExpiresAt"/>; accepting it creates an order.
/// Terms never change once sent: to change them the partner withdraws the offer and sends a new one.
/// </summary>
public sealed class Offer : AuditableEntity, IAudited
{
    public const int SummaryMinLength = 10;
    public const int SummaryMaxLength = 4000;
    public const int MaxLines = 200;
    public const decimal MaxLineQuantity = 100_000m;
    public const int LineMaxLength = 200;
    public const int MaterialsNoteMaxLength = 1000;
    public const int MaxDurationDays = 3650;
    public const int MaxStages = 10;
    public const int StageTitleMaxLength = 100;
    public const int ReasonMaxLength = 500;
    public const int MaxPrice = 1_000_000_000;

    private readonly List<OfferItem> _items = [];
    private readonly List<OfferPaymentStage> _stages = [];

    private Offer()
    {
    }

    public Guid RequestId { get; private set; }

    public Guid PartnerProfileId { get; private set; }

    public OfferKind Kind { get; private set; }

    public OfferStatus Status { get; private set; }

    public string Summary { get; private set; } = string.Empty;

    /// <summary>Price in AMD: the whole job for a work offer, the visit fee (zero = free) for a visit.</summary>
    public int Price { get; private set; }

    public bool MaterialsIncluded { get; private set; }

    public string? MaterialsNote { get; private set; }

    public DateOnly? StartDate { get; private set; }

    public int? DurationDays { get; private set; }

    /// <summary>When the partner will come, for a visit offer.</summary>
    public DateTimeOffset? VisitAt { get; private set; }

    public DateTimeOffset ExpiresAt { get; private set; }

    /// <summary>When the customer accepted or rejected it, or the partner withdrew it.</summary>
    public DateTimeOffset? DecidedAt { get; private set; }

    /// <summary>The customer's optional reason for rejecting it; the partner sees it.</summary>
    public string? RejectReason { get; private set; }

    /// <summary>The order created when the offer was accepted.</summary>
    public Guid? OrderId { get; private set; }

    public IReadOnlyCollection<OfferItem> Items => _items.AsReadOnly();

    public IReadOnlyCollection<OfferPaymentStage> Stages => _stages.AsReadOnly();

    public static Offer Create(Guid requestId, Guid partnerProfileId, OfferKind kind, OfferTerms terms, DateTimeOffset expiresAt, DateTimeOffset now)
    {
        if (requestId == Guid.Empty || partnerProfileId == Guid.Empty)
        {
            throw new DomainException("offer.parties_required", "An offer needs a request and a partner.");
        }

        if (!Enum.IsDefined(kind))
        {
            throw new DomainException("offer.kind_invalid", "Unknown offer kind.");
        }

        if (expiresAt <= now)
        {
            throw new DomainException("offer.expiry_invalid", "An offer must be valid for some time.");
        }

        var offer = new Offer
        {
            RequestId = requestId,
            PartnerProfileId = partnerProfileId,
            Kind = kind,
            Status = OfferStatus.Sent,
            Summary = CleanSummary(terms.Summary),
            Price = CheckPrice(kind, terms.Price),
            MaterialsIncluded = terms.MaterialsIncluded,
            MaterialsNote = CleanMaterialsNote(terms.MaterialsNote),
            ExpiresAt = expiresAt,
        };

        if (kind == OfferKind.Visit)
        {
            if (terms.Lines.Count > 0 || terms.Stages.Count > 0 || terms.StartDate is not null || terms.DurationDays is not null)
            {
                throw new DomainException("offer.visit_terms_invalid", "A visit offer has a time and a fee only.");
            }

            if (terms.VisitAt is not { } visitAt || visitAt <= now)
            {
                throw new DomainException("offer.visit_time_invalid", "A visit needs a time in the future.");
            }

            offer.VisitAt = visitAt;
        }
        else
        {
            if (terms.VisitAt is not null)
            {
                throw new DomainException("offer.visit_time_invalid", "Only a visit offer has a visit time.");
            }

            if (terms.DurationDays is < 1 or > MaxDurationDays)
            {
                throw new DomainException("offer.duration_invalid", $"The duration must be 1–{MaxDurationDays} days.");
            }

            offer.StartDate = terms.StartDate;
            offer.DurationDays = terms.DurationDays;
            offer.AddLines(terms.Lines);
        }

        offer.AddStages(terms.Stages);
        return offer;
    }

    /// <summary>Waiting for the customer and not expired.</summary>
    public bool IsOpen(DateTimeOffset now) => Status == OfferStatus.Sent && ExpiresAt > now;

    /// <summary>The status as people should see it: an offer past its expiry is expired even before anyone marks it.</summary>
    public OfferStatus StatusAt(DateTimeOffset now) => Status == OfferStatus.Sent && ExpiresAt <= now ? OfferStatus.Expired : Status;

    /// <summary>The customer accepts; <paramref name="orderId"/> is the order it created.</summary>
    public void Accept(Guid orderId, DateTimeOffset now)
    {
        EnsureOpen(now);
        Status = OfferStatus.Accepted;
        DecidedAt = now;
        OrderId = orderId;
    }

    public void Reject(string? reason, DateTimeOffset now)
    {
        EnsureOpen(now);
        var clean = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();
        if (clean?.Length > ReasonMaxLength)
        {
            throw new DomainException("offer.reason_too_long", $"The reason must be at most {ReasonMaxLength} characters.");
        }

        Status = OfferStatus.Rejected;
        DecidedAt = now;
        RejectReason = clean;
    }

    /// <summary>The partner takes the offer back, while the customer hasn't decided.</summary>
    public void Withdraw(DateTimeOffset now)
    {
        if (Status != OfferStatus.Sent)
        {
            throw new DomainException("offer.not_open", $"The offer is {Status}.");
        }

        Status = ExpiresAt <= now ? OfferStatus.Expired : OfferStatus.Withdrawn;
        DecidedAt = now;
    }

    /// <summary>Records that a waiting offer ran out. Returns whether it did.</summary>
    public bool Expire(DateTimeOffset now)
    {
        if (Status != OfferStatus.Sent || ExpiresAt > now)
        {
            return false;
        }

        Status = OfferStatus.Expired;
        return true;
    }

    /// <summary>The request closed while this offer waited. Decided offers stay as they are.</summary>
    public void Close(DateTimeOffset now)
    {
        if (Status == OfferStatus.Sent)
        {
            Status = ExpiresAt <= now ? OfferStatus.Expired : OfferStatus.Closed;
            DecidedAt = now;
        }
    }

    private static string CleanSummary(string? summary)
    {
        var clean = (summary ?? string.Empty).Trim();
        if (clean.Length is < SummaryMinLength or > SummaryMaxLength)
        {
            throw new DomainException("offer.summary_length", $"The summary must be {SummaryMinLength}–{SummaryMaxLength} characters.");
        }

        return clean;
    }

    private static int CheckPrice(OfferKind kind, int price)
    {
        var lowest = kind == OfferKind.Work ? 1 : 0;
        if (price < lowest || price > MaxPrice)
        {
            throw new DomainException("offer.price_invalid", "The price is not valid.");
        }

        return price;
    }

    private static string? CleanMaterialsNote(string? note)
    {
        var clean = string.IsNullOrWhiteSpace(note) ? null : note.Trim();
        if (clean?.Length > MaterialsNoteMaxLength)
        {
            throw new DomainException("offer.materials_note_too_long", $"The materials note must be at most {MaterialsNoteMaxLength} characters.");
        }

        return clean;
    }

    private void AddLines(IReadOnlyList<OfferLine> lines)
    {
        if (lines.Count > MaxLines)
        {
            throw new DomainException("offer.too_many_lines", $"At most {MaxLines} lines.");
        }

        foreach (var line in lines)
        {
            var title = (line.Title ?? string.Empty).Trim();
            if (title.Length is 0 or > LineMaxLength)
            {
                throw new DomainException("offer.line_invalid", $"Each line needs a title of at most {LineMaxLength} characters.");
            }

            if (line.UnitPrice is < 0 or > MaxPrice || line.Quantity is <= 0 or > MaxLineQuantity || (!line.Included && line.UnitPrice is not null))
            {
                throw new DomainException("offer.line_invalid", "A priced line needs a unit price and a sensible quantity; excluded work has no price.");
            }

            if (line.RequestLineId is { } requestLineId && _items.Any(i => i.RequestLineId == requestLineId))
            {
                throw new DomainException("offer.line_invalid", "Each line of the request is answered once.");
            }

            var quantity = line.Quantity is null ? (decimal?)null : Math.Round(line.Quantity.Value, 2, MidpointRounding.AwayFromZero);
            int? amount = line.UnitPrice is { } unitPrice ? (int)Math.Min(Math.Round(unitPrice * (quantity ?? 1m), MidpointRounding.AwayFromZero), MaxPrice) : null;
            _items.Add(new OfferItem(Id, title, line.Included, _items.Count + 1, line.RequestLineId, quantity, line.UnitPrice, amount));
        }

        // Priced line by line: every included line has a price and they add up to the offer's price.
        if (_items.Any(i => i.Amount is not null))
        {
            if (_items.Any(i => i.Included && i.Amount is null))
            {
                throw new DomainException("offer.lines_unpriced", "Price every included line, or none.");
            }

            if (_items.Sum(i => (long)(i.Amount ?? 0)) != Price)
            {
                throw new DomainException("offer.lines_sum_mismatch", "The priced lines must add up to the price.");
            }
        }
    }

    /// <summary>Stages must add up to the price; none means one final payment of the whole price (if any).</summary>
    private void AddStages(IReadOnlyList<OfferStageTerms> stages)
    {
        if (stages.Count == 0)
        {
            if (Price > 0)
            {
                _stages.Add(new OfferPaymentStage(Id, null, PaymentPurpose.Final, Price, 1));
            }

            return;
        }

        if (stages.Count > MaxStages)
        {
            throw new DomainException("offer.too_many_stages", $"At most {MaxStages} payment stages.");
        }

        foreach (var stage in stages)
        {
            var title = string.IsNullOrWhiteSpace(stage.Title) ? null : stage.Title.Trim();
            if (stage.Amount <= 0 || !Enum.IsDefined(stage.Purpose) || title?.Length > StageTitleMaxLength)
            {
                throw new DomainException("offer.stage_invalid", "Each payment stage needs an amount and a purpose.");
            }

            _stages.Add(new OfferPaymentStage(Id, title, stage.Purpose, stage.Amount, _stages.Count + 1));
        }

        if (stages.Take(stages.Count - 1).Any(s => s.Purpose == PaymentPurpose.Final))
        {
            throw new DomainException("offer.final_stage_last", "Only the last payment can be the final one.");
        }

        if (stages.Sum(s => (long)s.Amount) != Price)
        {
            throw new DomainException("offer.stages_sum_mismatch", "The payment stages must add up to the price.");
        }
    }

    private void EnsureOpen(DateTimeOffset now)
    {
        if (Status != OfferStatus.Sent)
        {
            throw new DomainException("offer.not_open", $"The offer is {Status}.");
        }

        if (ExpiresAt <= now)
        {
            throw new DomainException("offer.expired", "The offer has expired.");
        }
    }
}

/// <summary>A line of work in an offer, included in the price or excluded.</summary>
public sealed class OfferItem : Entity, IAudited
{
    private OfferItem()
    {
    }

    internal OfferItem(Guid offerId, string title, bool included, int sortOrder, Guid? requestLineId = null, decimal? quantity = null, int? unitPrice = null, int? amount = null)
    {
        OfferId = offerId;
        Title = title;
        Included = included;
        SortOrder = sortOrder;
        RequestLineId = requestLineId;
        Quantity = quantity;
        UnitPrice = unitPrice;
        Amount = amount;
    }

    public Guid OfferId { get; private set; }

    public string Title { get; private set; } = string.Empty;

    public bool Included { get; private set; }

    public int SortOrder { get; private set; }

    /// <summary>The request line this answers, for comparing offers line by line.</summary>
    public Guid? RequestLineId { get; private set; }

    public decimal? Quantity { get; private set; }

    /// <summary>AMD per unit; null for a line without its own price.</summary>
    public int? UnitPrice { get; private set; }

    /// <summary>Unit price × quantity (1 when not given), rounded to whole drams.</summary>
    public int? Amount { get; private set; }
}

/// <summary>One payment in an offer's plan.</summary>
public sealed class OfferPaymentStage : Entity, IAudited
{
    private OfferPaymentStage()
    {
    }

    internal OfferPaymentStage(Guid offerId, string? title, PaymentPurpose purpose, int amount, int sortOrder)
    {
        OfferId = offerId;
        Title = title;
        Purpose = purpose;
        Amount = amount;
        SortOrder = sortOrder;
    }

    public Guid OfferId { get; private set; }

    public string? Title { get; private set; }

    public PaymentPurpose Purpose { get; private set; }

    public int Amount { get; private set; }

    public int SortOrder { get; private set; }
}
