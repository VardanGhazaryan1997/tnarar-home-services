using HomeServices.Application.Files;

namespace HomeServices.Application.Requests;

/// <summary>What and where, with names in the request language.</summary>
public sealed record RequestPlaceDto(Guid CategoryId, string CategoryName, Guid CityId, string CityName, Guid? DistrictId, string? DistrictName);

/// <summary>A request in the customer's list. <see cref="FindingPartners"/>: no partner could take it yet and an operator is on it.</summary>
public sealed record MyRequestListItemDto(
    Guid Id,
    string Kind,
    string Status,
    RequestPlaceDto Place,
    string Excerpt,
    DateOnly? PreferredDate,
    int SentTo,
    int Responded,
    bool FindingPartners,
    DateTimeOffset CreatedAt);

/// <summary>A partner as the customer sees them on a request: the chosen partner of a direct request, or one who responded.</summary>
public sealed record MyRequestPartnerDto(Guid PartnerId, string DisplayName, string? Slug, string Status);

public sealed record MyRequestDto(
    Guid Id,
    string Kind,
    string Status,
    RequestPlaceDto Place,
    string Description,
    DateOnly? PreferredDate,
    string? TimeNote,
    int? BudgetMin,
    int? BudgetMax,
    IReadOnlyList<FileDto> Media,
    int SentTo,
    IReadOnlyList<MyRequestPartnerDto> Partners,
    bool FindingPartners,
    DateTimeOffset CreatedAt,
    DateTimeOffset? CancelledAt,
    string? CancelReason);

/// <summary>A request in a partner's inbox. <see cref="MyStatus"/>: New, Viewed, Declined or Responded.</summary>
public sealed record InboxItemDto(
    Guid Id,
    string Kind,
    string RequestStatus,
    string MyStatus,
    RequestPlaceDto Place,
    string Excerpt,
    DateOnly? PreferredDate,
    int MediaCount,
    DateTimeOffset SentAt);

/// <summary>
/// A request as a partner who received it sees it. The customer is shown by first name only until they agree on work,
/// and the customer's budget is never shown to partners.
/// </summary>
public sealed record InboxRequestDto(
    Guid Id,
    string Kind,
    string RequestStatus,
    string MyStatus,
    RequestPlaceDto Place,
    string Description,
    DateOnly? PreferredDate,
    string? TimeNote,
    IReadOnlyList<FileDto> Media,
    string? CustomerFirstName,
    DateTimeOffset SentAt,
    DateTimeOffset CreatedAt);

public sealed record AdminRequestListItemDto(
    Guid Id,
    string Kind,
    string Status,
    RequestPlaceDto Place,
    string Excerpt,
    Guid CustomerId,
    string CustomerPhone,
    string? CustomerName,
    int SentTo,
    int Responded,
    int Declined,
    string? AttentionReason,
    DateTimeOffset? NeedsAttentionSince,
    DateTimeOffset CreatedAt);

public sealed record RequestCustomerDto(Guid UserId, string Phone, string? FullName, string? Email, bool IsBlocked);

public sealed record AdminRecipientDto(
    Guid PartnerId,
    string DisplayName,
    string PartnerStatus,
    string Source,
    string Status,
    DateTimeOffset SentAt,
    DateTimeOffset? ViewedAt,
    DateTimeOffset? DeclinedAt,
    string? DeclineReason,
    DateTimeOffset? RespondedAt);

public sealed record AdminRequestDto(
    Guid Id,
    string Kind,
    string Status,
    RequestPlaceDto Place,
    string Description,
    DateOnly? PreferredDate,
    string? TimeNote,
    int? BudgetMin,
    int? BudgetMax,
    IReadOnlyList<FileDto> Media,
    RequestCustomerDto Customer,
    IReadOnlyList<AdminRecipientDto> Recipients,
    string? AttentionReason,
    DateTimeOffset? NeedsAttentionSince,
    DateTimeOffset CreatedAt,
    DateTimeOffset? CancelledAt,
    string? CancelReason);
