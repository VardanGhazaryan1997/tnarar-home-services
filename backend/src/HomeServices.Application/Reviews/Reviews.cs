using FluentValidation;
using HomeServices.Application.Abstractions;
using HomeServices.Application.Common;
using HomeServices.Application.Errors;
using HomeServices.Application.Messaging;
using HomeServices.Application.Notifications;
using HomeServices.Application.Orders;
using HomeServices.Application.Partners;
using HomeServices.Domain;
using HomeServices.Domain.Notifications;
using HomeServices.Domain.Orders;
using HomeServices.Domain.Reviews;
using Microsoft.EntityFrameworkCore;

namespace HomeServices.Application.Reviews;

/// <summary>The review of an order, as its customer, its partner or staff see it.</summary>
public sealed record OrderReviewDto(
    Guid Id,
    int Rating,
    string? Text,
    DateTimeOffset SubmittedAt,
    string? Reply,
    DateTimeOffset? RepliedAt,
    bool IsHidden,
    string? HiddenReason);

/// <summary>A review on a partner's public profile. <see cref="CustomerName"/>: the customer's first name only.</summary>
public sealed record PublicReviewDto(Guid Id, int Rating, string? Text, string? CustomerName, DateTimeOffset SubmittedAt, string? Reply, DateTimeOffset? RepliedAt);

/// <summary>A review in the Back Office.</summary>
public sealed record AdminReviewDto(
    Guid Id,
    Guid OrderId,
    int Rating,
    string? Text,
    DateTimeOffset SubmittedAt,
    string? Reply,
    DateTimeOffset? RepliedAt,
    bool IsHidden,
    string? HiddenReason,
    DateTimeOffset? HiddenAt,
    Guid CustomerId,
    string? CustomerName,
    Guid PartnerId,
    string PartnerName,
    string? PartnerSlug);

/// <summary>A partner's average rating (one decimal) and how many visible reviews it is based on.</summary>
public sealed record RatingSummary(double? Rating, int ReviewCount)
{
    public static readonly RatingSummary None = new(null, 0);
}

/// <summary>The customer reviews their completed order, once: 1–5 stars and optional text. Public right away.</summary>
public sealed record SubmitReview(Guid OrderId, int Rating, string? Text) : ICommand<OrderDto>;

/// <summary>The partner answers the review of their order, once.</summary>
public sealed record ReplyToReview(Guid OrderId, string Text) : ICommand<OrderDto>;

/// <summary>An approved partner's visible reviews, newest first.</summary>
public sealed record GetPartnerReviews(string Slug, int Page = 1, int PageSize = GetPartnerReviews.DefaultPageSize) : IQuery<PagedResult<PublicReviewDto>>
{
    public const int DefaultPageSize = 10;
    public const int MaxPageSize = 50;
}

/// <summary>Reviews for the Back Office, newest first. <see cref="MaxRating"/>: e.g. 2 lists the low ones.</summary>
public sealed record GetAdminReviews(
    bool? Hidden = null,
    Guid? PartnerId = null,
    int? MaxRating = null,
    int Page = 1,
    int PageSize = GetAdminReviews.DefaultPageSize) : IQuery<PagedResult<AdminReviewDto>>
{
    public const int DefaultPageSize = 20;
    public const int MaxPageSize = 100;
}

/// <summary>Staff hide a review from the public profile, saying why.</summary>
public sealed record HideReview(Guid Id, string Reason) : ICommand<AdminReviewDto>;

/// <summary>Staff show a hidden review again.</summary>
public sealed record RestoreReview(Guid Id) : ICommand<AdminReviewDto>;

public sealed class SubmitReviewValidator : AbstractValidator<SubmitReview>
{
    public SubmitReviewValidator()
    {
        RuleFor(x => x.Rating).InclusiveBetween(1, 5).WithErrorCode("rating.invalid");
        RuleFor(x => x.Text)
            .Must(text => text!.Trim().Length <= Review.TextMaxLength).WithErrorCode("text.too_long")
            .When(x => x.Text is not null);
    }
}

public sealed class ReplyToReviewValidator : AbstractValidator<ReplyToReview>
{
    public ReplyToReviewValidator() =>
        RuleFor(x => x.Text)
            .Must(text => !string.IsNullOrWhiteSpace(text)).WithErrorCode("text.required")
            .Must(text => text is null || text.Trim().Length <= Review.TextMaxLength).WithErrorCode("text.too_long");
}

public sealed class GetPartnerReviewsValidator : AbstractValidator<GetPartnerReviews>
{
    public GetPartnerReviewsValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1).WithErrorCode("page.invalid");
        RuleFor(x => x.PageSize).InclusiveBetween(1, GetPartnerReviews.MaxPageSize).WithErrorCode("page_size.invalid");
    }
}

public sealed class GetAdminReviewsValidator : AbstractValidator<GetAdminReviews>
{
    public GetAdminReviewsValidator()
    {
        RuleFor(x => x.MaxRating).InclusiveBetween(1, 5).WithErrorCode("max_rating.invalid").When(x => x.MaxRating is not null);
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1).WithErrorCode("page.invalid");
        RuleFor(x => x.PageSize).InclusiveBetween(1, GetAdminReviews.MaxPageSize).WithErrorCode("page_size.invalid");
    }
}

public sealed class HideReviewValidator : AbstractValidator<HideReview>
{
    public HideReviewValidator() =>
        RuleFor(x => x.Reason)
            .Must(value => !string.IsNullOrWhiteSpace(value)).WithErrorCode("reason.required")
            .Must(value => value is null || value.Trim().Length <= Review.ReasonMaxLength).WithErrorCode("reason.too_long");
}

internal static class ReviewViews
{
    public static NotFoundException NotFound() => new("Review not found.", "review.not_found");

    public static OrderReviewDto ToDto(Review r) => new(r.Id, r.Rating, r.Text, r.SubmittedAt, r.Reply, r.RepliedAt, r.IsHidden, r.HiddenReason);

    /// <summary>"Anna Petrosyan" → "Anna": public reviews don't show full names.</summary>
    public static string? FirstName(string? fullName) =>
        string.IsNullOrWhiteSpace(fullName) ? null : fullName.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries)[0];

    /// <summary>Average rating and count of visible reviews for these partners.</summary>
    public static async Task<Dictionary<Guid, RatingSummary>> RatingsAsync(IAppDbContext db, IReadOnlyCollection<Guid> partnerIds, CancellationToken cancellationToken)
    {
        var rows = await db.Reviews.AsNoTracking()
            .Where(r => partnerIds.Contains(r.PartnerProfileId) && !r.IsHidden)
            .GroupBy(r => r.PartnerProfileId)
            .Select(g => new { PartnerId = g.Key, Average = g.Average(r => (double)r.Rating), Count = g.Count() })
            .ToListAsync(cancellationToken);
        return rows.ToDictionary(r => r.PartnerId, r => new RatingSummary(Math.Round(r.Average, 1), r.Count));
    }
}

/// <summary>Loads the signed-in user's order with its review, for the review commands.</summary>
internal static class ReviewCommand
{
    public static async Task<(Order Order, OrderParty Party, Review? Review)> LoadAsync(
        IAppDbContext db, ICurrentUser currentUser, Guid orderId, CancellationToken cancellationToken)
    {
        var (userId, partnerId) = await OrderViews.MeAsync(db, currentUser, cancellationToken);
        var order = await db.Orders.WithDetails()
            .SingleOrDefaultAsync(o => o.Id == orderId && (o.CustomerId == userId || o.PartnerProfileId == partnerId), cancellationToken)
            ?? throw OrderViews.NotFound();
        var review = await db.Reviews.SingleOrDefaultAsync(r => r.OrderId == order.Id, cancellationToken);
        return (order, order.CustomerId == userId ? OrderParty.Customer : OrderParty.Partner, review);
    }
}

public sealed class SubmitReviewHandler(IAppDbContext db, ICurrentUser currentUser, ICurrentLanguage language, TimeProvider clock)
    : ICommandHandler<SubmitReview, OrderDto>
{
    public async Task<OrderDto> HandleAsync(SubmitReview command, CancellationToken cancellationToken)
    {
        var (order, party, existing) = await ReviewCommand.LoadAsync(db, currentUser, command.OrderId, cancellationToken);
        if (existing is not null)
        {
            throw new DomainException("review.already_submitted", "You already reviewed this order.");
        }

        var now = clock.GetUtcNow();
        var review = Review.Submit(order, party, command.Rating, command.Text, now);
        db.Reviews.Add(review);
        await Notifier.ToOrderAsync(
            db,
            order,
            party,
            NoticeTo.Partner,
            NotificationType.ReviewReceived,
            new Dictionary<string, string?> { ["rating"] = review.Rating.ToString(System.Globalization.CultureInfo.InvariantCulture) },
            now,
            cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        return await OrderViews.ToDtoAsync(db, language, order, party, cancellationToken);
    }
}

public sealed class ReplyToReviewHandler(IAppDbContext db, ICurrentUser currentUser, ICurrentLanguage language, TimeProvider clock)
    : ICommandHandler<ReplyToReview, OrderDto>
{
    public async Task<OrderDto> HandleAsync(ReplyToReview command, CancellationToken cancellationToken)
    {
        var (order, party, review) = await ReviewCommand.LoadAsync(db, currentUser, command.OrderId, cancellationToken);
        if (review is null)
        {
            throw ReviewViews.NotFound();
        }

        var now = clock.GetUtcNow();
        review.AddReply(party, command.Text, now);
        await Notifier.ToOrderAsync(db, order, party, NoticeTo.Customer, NotificationType.ReviewReplied, null, now, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        return await OrderViews.ToDtoAsync(db, language, order, party, cancellationToken);
    }
}

public sealed class GetPartnerReviewsHandler(IAppDbContext db) : IQueryHandler<GetPartnerReviews, PagedResult<PublicReviewDto>>
{
    public async Task<PagedResult<PublicReviewDto>> HandleAsync(GetPartnerReviews query, CancellationToken cancellationToken)
    {
        var slug = query.Slug.Trim().ToLowerInvariant();
        var partnerId = await PublicPartnerQueries.Visible(db).Where(p => p.Slug == slug).Select(p => (Guid?)p.Id).SingleOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("Partner not found.", "partner.not_found");

        var reviews = db.Reviews.AsNoTracking().Where(r => r.PartnerProfileId == partnerId && !r.IsHidden);
        var total = await reviews.CountAsync(cancellationToken);
        var page = await (
            from review in reviews
            join customer in db.Users.AsNoTracking() on review.CustomerId equals customer.Id
            orderby review.SubmittedAt descending, review.Id descending
            select new { Review = review, customer.FullName })
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);

        var items = page
            .Select(r => new PublicReviewDto(r.Review.Id, r.Review.Rating, r.Review.Text, ReviewViews.FirstName(r.FullName), r.Review.SubmittedAt, r.Review.Reply, r.Review.RepliedAt))
            .ToList();
        return new PagedResult<PublicReviewDto>(items, query.Page, query.PageSize, total);
    }
}

internal static class AdminReviews
{
    public static async Task<List<AdminReviewDto>> LoadAsync(IAppDbContext db, IReadOnlyList<Guid> ids, CancellationToken cancellationToken)
    {
        var rows = await (
            from review in db.Reviews.AsNoTracking()
            join customer in db.Users.AsNoTracking() on review.CustomerId equals customer.Id
            join partner in db.PartnerProfiles.AsNoTracking() on review.PartnerProfileId equals partner.Id
            where ids.Contains(review.Id)
            select new { Review = review, customer.FullName, partner.DisplayName, partner.Slug })
            .ToListAsync(cancellationToken);
        return ids
            .Select(id => rows.Single(r => r.Review.Id == id))
            .Select(r => new AdminReviewDto(
                r.Review.Id,
                r.Review.OrderId,
                r.Review.Rating,
                r.Review.Text,
                r.Review.SubmittedAt,
                r.Review.Reply,
                r.Review.RepliedAt,
                r.Review.IsHidden,
                r.Review.HiddenReason,
                r.Review.HiddenAt,
                r.Review.CustomerId,
                r.FullName,
                r.Review.PartnerProfileId,
                r.DisplayName,
                r.Slug))
            .ToList();
    }
}

public sealed class GetAdminReviewsHandler(IAppDbContext db) : IQueryHandler<GetAdminReviews, PagedResult<AdminReviewDto>>
{
    public async Task<PagedResult<AdminReviewDto>> HandleAsync(GetAdminReviews query, CancellationToken cancellationToken)
    {
        var reviews = db.Reviews.AsNoTracking();
        if (query.Hidden is { } hidden)
        {
            reviews = reviews.Where(r => r.IsHidden == hidden);
        }

        if (query.PartnerId is { } partnerId)
        {
            reviews = reviews.Where(r => r.PartnerProfileId == partnerId);
        }

        if (query.MaxRating is { } maxRating)
        {
            reviews = reviews.Where(r => r.Rating <= maxRating);
        }

        var total = await reviews.CountAsync(cancellationToken);
        var ids = await reviews
            .OrderByDescending(r => r.SubmittedAt).ThenByDescending(r => r.Id)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(r => r.Id)
            .ToListAsync(cancellationToken);
        return new PagedResult<AdminReviewDto>(await AdminReviews.LoadAsync(db, ids, cancellationToken), query.Page, query.PageSize, total);
    }
}

public sealed class HideReviewHandler(IAppDbContext db, TimeProvider clock) : ICommandHandler<HideReview, AdminReviewDto>
{
    public async Task<AdminReviewDto> HandleAsync(HideReview command, CancellationToken cancellationToken)
    {
        var review = await db.Reviews.SingleOrDefaultAsync(r => r.Id == command.Id, cancellationToken) ?? throw ReviewViews.NotFound();
        review.Hide(command.Reason, clock.GetUtcNow());
        await db.SaveChangesAsync(cancellationToken);
        return (await AdminReviews.LoadAsync(db, [review.Id], cancellationToken))[0];
    }
}

public sealed class RestoreReviewHandler(IAppDbContext db) : ICommandHandler<RestoreReview, AdminReviewDto>
{
    public async Task<AdminReviewDto> HandleAsync(RestoreReview command, CancellationToken cancellationToken)
    {
        var review = await db.Reviews.SingleOrDefaultAsync(r => r.Id == command.Id, cancellationToken) ?? throw ReviewViews.NotFound();
        review.Restore();
        await db.SaveChangesAsync(cancellationToken);
        return (await AdminReviews.LoadAsync(db, [review.Id], cancellationToken))[0];
    }
}
