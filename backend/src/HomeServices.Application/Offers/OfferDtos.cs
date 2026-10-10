using HomeServices.Application.Requests;

namespace HomeServices.Application.Offers;

public sealed record OfferPartnerDto(Guid PartnerId, string DisplayName, string? Slug);

/// <summary>
/// A line of an offer. Priced lines have a <see cref="UnitPrice"/> and <see cref="Amount"/>; <see cref="RequestLineId"/>
/// points at the request line it answers.
/// </summary>
public sealed record OfferLineDto(string Title, bool Included, Guid? RequestLineId = null, decimal? Quantity = null, int? UnitPrice = null, int? Amount = null);

/// <summary><see cref="Purpose"/>: Deposit, Stage or Final.</summary>
public sealed record PaymentStageDto(string? Title, string Purpose, int Amount);

/// <summary>
/// An offer as the customer of the request or the partner who sent it sees it. <see cref="Status"/>: Sent, Accepted,
/// Rejected, Withdrawn, Expired or Closed (the request closed while it waited). <see cref="OrderId"/> once accepted.
/// </summary>
public sealed record OfferDto(
    Guid Id,
    Guid RequestId,
    string Kind,
    string Status,
    OfferPartnerDto Partner,
    string Summary,
    IReadOnlyList<OfferLineDto> Lines,
    int Price,
    bool MaterialsIncluded,
    string? MaterialsNote,
    DateOnly? StartDate,
    int? DurationDays,
    DateTimeOffset? VisitAt,
    IReadOnlyList<PaymentStageDto> Stages,
    DateTimeOffset ExpiresAt,
    DateTimeOffset SentAt,
    DateTimeOffset? DecidedAt,
    string? RejectReason,
    Guid? OrderId);

/// <summary>An offer in the partner's list of sent offers.</summary>
public sealed record MyOfferListItemDto(
    Guid Id,
    Guid RequestId,
    string Kind,
    string Status,
    RequestPlaceDto Place,
    string RequestExcerpt,
    int Price,
    DateTimeOffset? VisitAt,
    DateTimeOffset ExpiresAt,
    DateTimeOffset SentAt,
    Guid? OrderId);
