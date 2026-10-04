using HomeServices.Application.Offers;
using HomeServices.Application.Payments;
using HomeServices.Application.Requests;
using HomeServices.Application.Reviews;

namespace HomeServices.Application.Orders;

/// <summary>Which side of an order the signed-in user is on.</summary>
public enum OrderRole
{
    Customer = 1,
    Partner = 2,
}

/// <summary>
/// The accepted offer's terms, frozen when the order was created (stored as JSON on the order).
/// <see cref="Version"/> is the shape of this record, for reading old orders after it changes.
/// </summary>
public sealed record OrderTermsDto(
    int Version,
    string Summary,
    IReadOnlyList<OfferLineDto> Lines,
    int Price,
    bool MaterialsIncluded,
    string? MaterialsNote,
    DateOnly? StartDate,
    int? DurationDays,
    DateTimeOffset? VisitAt,
    IReadOnlyList<PaymentStageDto> Stages,
    DateTimeOffset OfferSentAt);

/// <summary>The customer, as the partner of a confirmed order sees them: contact details are shared from here on.</summary>
public sealed record OrderCustomerDto(Guid UserId, string? FullName, string Phone);

/// <summary>The partner, as the customer of a confirmed order sees them.</summary>
public sealed record OrderPartnerDto(Guid PartnerId, string DisplayName, string? Slug, string Phone);

public sealed record OrderStageDto(Guid Id, string? Title, string Purpose, int Amount);

/// <summary>A history row. <see cref="By"/>: Customer, Partner, Staff or System (null on old rows); <see cref="Note"/>: the reason given.</summary>
public sealed record OrderStatusChangeDto(string Status, DateTimeOffset ChangedAt, string? By, string? Note);

/// <summary>A proposed change. <see cref="Mine"/>: the viewer proposed it (so they can withdraw it, not answer it).</summary>
public sealed record OrderChangeRequestDto(
    Guid Id,
    string Kind,
    string Status,
    string ProposedBy,
    bool Mine,
    string? Title,
    string? Description,
    int? Amount,
    DateOnly? NewStartDate,
    int? NewDurationDays,
    DateTimeOffset? NewVisitAt,
    string? ResponseNote,
    DateTimeOffset ProposedAt,
    DateTimeOffset? DecidedAt);

/// <summary>What the viewer can do next, so screens don't repeat the rules. Values of <see cref="OrderDto.Actions"/>.</summary>
public static class OrderActions
{
    public const string Start = "start";
    public const string RequestCompletion = "requestCompletion";
    public const string ConfirmCompletion = "confirmCompletion";
    public const string RejectCompletion = "rejectCompletion";
    public const string Cancel = "cancel";
    public const string ProposeChange = "proposeChange";
    public const string AnswerChange = "answerChange";
    public const string WithdrawChange = "withdrawChange";
    public const string Resolve = "resolve";
    public const string RecordPayment = "recordPayment";
    public const string Review = "review";
    public const string ReplyReview = "replyReview";
}

/// <summary>
/// An order as either party (or staff) sees it. <see cref="MyRole"/>: Customer, Partner or Staff. <see cref="Price"/>,
/// <see cref="StartDate"/>, <see cref="DurationDays"/> and <see cref="VisitAt"/> are current (accepted changes included);
/// <see cref="Terms"/> stays as first agreed. <see cref="AutoCompleteAt"/>: when a done order completes without the customer.
/// <see cref="PaidAmount"/>: the confirmed payments' total. <see cref="Review"/>: the customer's review, once there is one.
/// </summary>
public sealed record OrderDto(
    Guid Id,
    string Kind,
    string Status,
    string MyRole,
    Guid RequestId,
    Guid OfferId,
    Guid? ParentOrderId,
    RequestPlaceDto Place,
    int Price,
    OrderTermsDto Terms,
    IReadOnlyList<OrderStageDto> Stages,
    OrderCustomerDto Customer,
    OrderPartnerDto Partner,
    IReadOnlyList<OrderStatusChangeDto> History,
    DateTimeOffset CreatedAt,
    DateOnly? StartDate,
    int? DurationDays,
    DateTimeOffset? VisitAt,
    DateTimeOffset? StartedAt,
    DateTimeOffset? CompletionRequestedAt,
    DateTimeOffset? AutoCompleteAt,
    DateTimeOffset? CompletedAt,
    DateTimeOffset? CancelledAt,
    string? CancelledBy,
    string? CancelReason,
    DateTimeOffset? NeedsAttentionSince,
    IReadOnlyList<OrderChangeRequestDto> ChangeRequests,
    IReadOnlyList<PaymentDto> Payments,
    int PaidAmount,
    OrderReviewDto? Review,
    IReadOnlyList<string> Actions);

/// <summary>An order in "my orders". <see cref="OtherParty"/>: the partner's name for the customer, the customer's name for the partner.</summary>
public sealed record OrderListItemDto(
    Guid Id,
    string Kind,
    string Status,
    string MyRole,
    RequestPlaceDto Place,
    string Summary,
    int Price,
    string? OtherParty,
    DateOnly? StartDate,
    DateTimeOffset? VisitAt,
    DateTimeOffset CreatedAt);

/// <summary>An order in the Back Office list. <see cref="PendingChange"/>: a change is waiting for an answer.</summary>
public sealed record AdminOrderListItemDto(
    Guid Id,
    string Kind,
    string Status,
    RequestPlaceDto Place,
    string Summary,
    int Price,
    Guid CustomerId,
    string? CustomerName,
    string CustomerPhone,
    Guid PartnerId,
    string PartnerName,
    bool PendingChange,
    DateTimeOffset? NeedsAttentionSince,
    DateTimeOffset CreatedAt);
