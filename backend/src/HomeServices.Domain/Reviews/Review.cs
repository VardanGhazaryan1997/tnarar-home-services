using HomeServices.Domain.Common;
using HomeServices.Domain.Orders;

namespace HomeServices.Domain.Reviews;

/// <summary>
/// The customer's review of a completed order: 1–5 stars and optional text, public on the partner's profile right
/// away. The partner can reply once; staff can hide a review (with a reason) and restore it.
/// </summary>
public sealed class Review : AuditableEntity, IAudited
{
    public const int TextMaxLength = 2000;
    public const int ReasonMaxLength = 500;

    private Review()
    {
    }

    public Guid OrderId { get; private set; }

    public Guid PartnerProfileId { get; private set; }

    public Guid CustomerId { get; private set; }

    public int Rating { get; private set; }

    public string? Text { get; private set; }

    public DateTimeOffset SubmittedAt { get; private set; }

    public string? Reply { get; private set; }

    public DateTimeOffset? RepliedAt { get; private set; }

    public bool IsHidden { get; private set; }

    public string? HiddenReason { get; private set; }

    public DateTimeOffset? HiddenAt { get; private set; }

    /// <summary>The customer reviews <paramref name="order"/> once it is completed.</summary>
    public static Review Submit(Order order, OrderParty by, int rating, string? text, DateTimeOffset now)
    {
        if (by != OrderParty.Customer)
        {
            throw new DomainException("review.wrong_party", "Only the customer reviews an order.");
        }

        if (order.Status != OrderStatus.Completed)
        {
            throw new DomainException("review.order_not_completed", "An order is reviewed once it is completed.");
        }

        if (rating is < 1 or > 5)
        {
            throw new DomainException("review.rating_invalid", "The rating is 1 to 5 stars.");
        }

        return new Review
        {
            OrderId = order.Id,
            PartnerProfileId = order.PartnerProfileId,
            CustomerId = order.CustomerId,
            Rating = rating,
            Text = Clean(text, TextMaxLength, "review.text_too_long"),
            SubmittedAt = now,
        };
    }

    /// <summary>The partner answers the review, once.</summary>
    public void AddReply(OrderParty by, string? text, DateTimeOffset now)
    {
        if (by != OrderParty.Partner)
        {
            throw new DomainException("review.wrong_party", "Only the partner replies to a review.");
        }

        if (Reply is not null)
        {
            throw new DomainException("review.already_replied", "You already replied to this review.");
        }

        Reply = Clean(text, TextMaxLength, "review.reply_too_long") ?? throw new DomainException("review.reply_required", "Write a reply.");
        RepliedAt = now;
    }

    /// <summary>Staff hide the review from the public profile.</summary>
    public void Hide(string? reason, DateTimeOffset now)
    {
        if (IsHidden)
        {
            throw new DomainException("review.already_hidden", "This review is already hidden.");
        }

        HiddenReason = Clean(reason, ReasonMaxLength, "review.reason_too_long") ?? throw new DomainException("review.reason_required", "Say why.");
        IsHidden = true;
        HiddenAt = now;
    }

    public void Restore()
    {
        if (!IsHidden)
        {
            throw new DomainException("review.not_hidden", "This review isn't hidden.");
        }

        IsHidden = false;
        HiddenReason = null;
        HiddenAt = null;
    }

    private static string? Clean(string? text, int max, string tooLongCode)
    {
        var clean = string.IsNullOrWhiteSpace(text) ? null : text.Trim();
        return clean?.Length > max ? throw new DomainException(tooLongCode, $"At most {max} characters.") : clean;
    }
}
