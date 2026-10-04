using HomeServices.Application.Common;
using HomeServices.Application.Messaging;
using HomeServices.Application.Partners;
using HomeServices.Application.Reviews;
using HomeServices.Domain.Partners;
using Microsoft.AspNetCore.Mvc;

namespace HomeServices.Api.Controllers;

/// <summary>Approved partners for visitors (no sign-in). Names come in the request language (Accept-Language).</summary>
[ApiController]
[Route("api/v1/partners")]
public sealed class PartnersController : ControllerBase
{
    /// <summary>
    /// Search by category, city and district slugs (e.g. <c>?category=plumbing&amp;city=yerevan&amp;district=kentron</c>).
    /// A category includes its subcategories; partners serving a whole city match every district.
    /// <paramref name="pageSize"/> is at most 50.
    /// </summary>
    [HttpGet]
    public Task<PagedResult<PublicPartnerCardDto>> Search(
        [FromServices] IQueryHandler<SearchPartners, PagedResult<PublicPartnerCardDto>> handler,
        [FromQuery] string? category,
        [FromQuery] string? city,
        [FromQuery] string? district,
        [FromQuery] PartnerType? type,
        [FromQuery] string? search,
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        CancellationToken cancellationToken) =>
        handler.HandleAsync(
            new SearchPartners(category, city, district, type, search, page ?? 1, pageSize ?? SearchPartners.DefaultPageSize),
            cancellationToken);

    /// <summary>A public profile, or 404 "partner.not_found" when there's no approved partner with this slug.</summary>
    [HttpGet("{slug}")]
    public Task<PublicPartnerDto> Get(
        string slug,
        [FromServices] IQueryHandler<GetPublicPartner, PublicPartnerDto> handler,
        CancellationToken cancellationToken) =>
        handler.HandleAsync(new GetPublicPartner(slug), cancellationToken);

    /// <summary>The partner's visible reviews, newest first (customers' first names only). <paramref name="pageSize"/> is at most 50.</summary>
    [HttpGet("{slug}/reviews")]
    public Task<PagedResult<PublicReviewDto>> Reviews(
        string slug,
        [FromServices] IQueryHandler<GetPartnerReviews, PagedResult<PublicReviewDto>> handler,
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        CancellationToken cancellationToken) =>
        handler.HandleAsync(new GetPartnerReviews(slug, page ?? 1, pageSize ?? GetPartnerReviews.DefaultPageSize), cancellationToken);
}
