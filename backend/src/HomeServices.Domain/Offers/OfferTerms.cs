namespace HomeServices.Domain.Offers;

/// <summary>
/// A line of work in an offer: included in the price, or explicitly excluded. A priced line has a
/// <see cref="UnitPrice"/> (AMD per unit) and a <see cref="Quantity"/> (1 when left out); it may answer a line of the
/// request (<see cref="RequestLineId"/>), so the customer can compare offers line by line.
/// </summary>
public sealed record OfferLine(string Title, bool Included, Guid? RequestLineId = null, decimal? Quantity = null, int? UnitPrice = null);

/// <summary>A payment the customer makes; the stages of an offer add up to its price.</summary>
public sealed record OfferStageTerms(string? Title, PaymentPurpose Purpose, int Amount);

/// <summary>
/// What the partner proposes. A work offer has a scope, a price (above zero), optionally a start date, a duration
/// and payment stages. A visit offer has a time and a fee (zero means free); it has no lines, dates or stages.
/// </summary>
public sealed record OfferTerms(
    string Summary,
    IReadOnlyList<OfferLine> Lines,
    int Price,
    bool MaterialsIncluded,
    string? MaterialsNote,
    DateOnly? StartDate,
    int? DurationDays,
    DateTimeOffset? VisitAt,
    IReadOnlyList<OfferStageTerms> Stages);
