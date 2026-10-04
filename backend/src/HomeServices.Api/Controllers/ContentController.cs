using HomeServices.Application.Content;
using HomeServices.Application.Messaging;
using HomeServices.Domain.Content;
using Microsoft.AspNetCore.Mvc;

namespace HomeServices.Api.Controllers;

/// <summary>Published information pages and FAQs (no sign-in), in the request language.</summary>
[ApiController]
[Route("api/v1")]
public sealed class ContentController : ControllerBase
{
    /// <summary>Published pages: slug and title, by position. <c>showInFooter</c> marks footer links.</summary>
    [HttpGet("pages")]
    public Task<IReadOnlyList<PublicPageLinkDto>> Pages(
        [FromServices] IQueryHandler<GetPublicPages, IReadOnlyList<PublicPageLinkDto>> handler,
        CancellationToken cancellationToken) =>
        handler.HandleAsync(new GetPublicPages(), cancellationToken);

    /// <summary>A published page; <c>body</c> is Markdown. 404 "page.not_found" otherwise.</summary>
    [HttpGet("pages/{slug}")]
    public Task<PublicPageDto> Page(string slug, [FromServices] IQueryHandler<GetPublicPage, PublicPageDto> handler, CancellationToken cancellationToken) =>
        handler.HandleAsync(new GetPublicPage(slug), cancellationToken);

    /// <summary>Published questions by audience (General, Customers, Partners) and position.</summary>
    [HttpGet("faqs")]
    public Task<IReadOnlyList<PublicFaqDto>> Faqs(
        [FromQuery] FaqAudience? audience,
        [FromServices] IQueryHandler<GetPublicFaqs, IReadOnlyList<PublicFaqDto>> handler,
        CancellationToken cancellationToken) =>
        handler.HandleAsync(new GetPublicFaqs(audience), cancellationToken);
}
