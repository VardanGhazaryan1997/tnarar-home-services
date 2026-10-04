using HomeServices.Api.Authorization;
using HomeServices.Application.Common;
using HomeServices.Application.Messaging;
using HomeServices.Application.Reviews;
using HomeServices.Domain.Staff;
using Microsoft.AspNetCore.Mvc;

namespace HomeServices.Api.Controllers.Admin;

/// <summary>Review moderation: every review, hiding one from the public profile (with a reason) and restoring it.</summary>
[ApiController]
[Route("api/v1/admin/reviews")]
public sealed class AdminReviewsController : ControllerBase
{
    public sealed record HideBody(string Reason);

    /// <summary>Reviews, newest first. <c>?hidden=true</c> lists hidden ones; <c>?maxRating=2</c> the low ones.</summary>
    [HttpGet]
    [HasPermission(Permissions.ReviewsModerate)]
    public Task<PagedResult<AdminReviewDto>> List(
        [FromServices] IQueryHandler<GetAdminReviews, PagedResult<AdminReviewDto>> handler,
        [FromQuery] bool? hidden,
        [FromQuery] Guid? partnerId,
        [FromQuery] int? maxRating,
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        CancellationToken cancellationToken) =>
        handler.HandleAsync(new GetAdminReviews(hidden, partnerId, maxRating, page ?? 1, pageSize ?? GetAdminReviews.DefaultPageSize), cancellationToken);

    [HttpPost("{id:guid}/hide")]
    [HasPermission(Permissions.ReviewsModerate)]
    public Task<AdminReviewDto> Hide(Guid id, HideBody body, [FromServices] ICommandHandler<HideReview, AdminReviewDto> handler, CancellationToken cancellationToken) =>
        handler.HandleAsync(new HideReview(id, body.Reason), cancellationToken);

    [HttpPost("{id:guid}/restore")]
    [HasPermission(Permissions.ReviewsModerate)]
    public Task<AdminReviewDto> Restore(Guid id, [FromServices] ICommandHandler<RestoreReview, AdminReviewDto> handler, CancellationToken cancellationToken) =>
        handler.HandleAsync(new RestoreReview(id), cancellationToken);
}
