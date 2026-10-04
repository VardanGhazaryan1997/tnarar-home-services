using HomeServices.Api.Authorization;
using HomeServices.Application.Content;
using HomeServices.Application.Messaging;
using HomeServices.Domain.Content;
using HomeServices.Domain.Staff;
using Microsoft.AspNetCore.Mvc;

namespace HomeServices.Api.Controllers.Admin;

/// <summary>
/// Information pages and FAQs (needs <c>content.manage</c>). Texts are sent per language:
/// <c>{ "hy": "…", "ru": "…" }</c>; the default language is required for titles, questions and answers.
/// </summary>
[ApiController]
[Route("api/v1/admin")]
[HasPermission(Permissions.ContentManage)]
public sealed class AdminContentController : ControllerBase
{
    public sealed record PageRequest(string Slug, Dictionary<string, string> Title, Dictionary<string, string> Body, bool ShowInFooter, int SortOrder);

    public sealed record FaqRequest(Dictionary<string, string> Question, Dictionary<string, string> Answer, FaqAudience Audience, int SortOrder);

    // ---------- Pages ----------

    [HttpGet("pages")]
    public Task<IReadOnlyList<AdminPageDto>> Pages([FromServices] IQueryHandler<GetAdminPages, IReadOnlyList<AdminPageDto>> handler, CancellationToken cancellationToken) =>
        handler.HandleAsync(new GetAdminPages(), cancellationToken);

    [HttpGet("pages/{id:guid}")]
    public Task<AdminPageDto> Page(Guid id, [FromServices] IQueryHandler<GetAdminPage, AdminPageDto> handler, CancellationToken cancellationToken) =>
        handler.HandleAsync(new GetAdminPage(id), cancellationToken);

    /// <summary>Creates a draft. Body is Markdown and may stay empty until publishing.</summary>
    [HttpPost("pages")]
    public async Task<ActionResult<AdminPageDto>> CreatePageAsync(
        PageRequest request,
        [FromServices] ICommandHandler<CreatePage, AdminPageDto> handler,
        CancellationToken cancellationToken) =>
        StatusCode(
            StatusCodes.Status201Created,
            await handler.HandleAsync(new CreatePage(request.Slug, request.Title, request.Body, request.ShowInFooter, request.SortOrder), cancellationToken));

    [HttpPut("pages/{id:guid}")]
    public Task<AdminPageDto> UpdatePage(
        Guid id,
        PageRequest request,
        [FromServices] ICommandHandler<UpdatePage, AdminPageDto> handler,
        CancellationToken cancellationToken) =>
        handler.HandleAsync(new UpdatePage(id, request.Slug, request.Title, request.Body, request.ShowInFooter, request.SortOrder), cancellationToken);

    [HttpPost("pages/{id:guid}/publish")]
    public Task<AdminPageDto> PublishPage(Guid id, [FromServices] ICommandHandler<SetPagePublished, AdminPageDto> handler, CancellationToken cancellationToken) =>
        handler.HandleAsync(new SetPagePublished(id, IsPublished: true), cancellationToken);

    [HttpPost("pages/{id:guid}/unpublish")]
    public Task<AdminPageDto> UnpublishPage(Guid id, [FromServices] ICommandHandler<SetPagePublished, AdminPageDto> handler, CancellationToken cancellationToken) =>
        handler.HandleAsync(new SetPagePublished(id, IsPublished: false), cancellationToken);

    [HttpDelete("pages/{id:guid}")]
    public async Task<IActionResult> DeletePageAsync(Guid id, [FromServices] ICommandHandler<DeletePage, bool> handler, CancellationToken cancellationToken)
    {
        await handler.HandleAsync(new DeletePage(id), cancellationToken);
        return NoContent();
    }

    // ---------- FAQs ----------

    [HttpGet("faqs")]
    public Task<IReadOnlyList<AdminFaqDto>> Faqs([FromServices] IQueryHandler<GetAdminFaqs, IReadOnlyList<AdminFaqDto>> handler, CancellationToken cancellationToken) =>
        handler.HandleAsync(new GetAdminFaqs(), cancellationToken);

    /// <summary>Creates an unpublished question.</summary>
    [HttpPost("faqs")]
    public async Task<ActionResult<AdminFaqDto>> CreateFaqAsync(
        FaqRequest request,
        [FromServices] ICommandHandler<CreateFaq, AdminFaqDto> handler,
        CancellationToken cancellationToken) =>
        StatusCode(
            StatusCodes.Status201Created,
            await handler.HandleAsync(new CreateFaq(request.Question, request.Answer, request.Audience, request.SortOrder), cancellationToken));

    [HttpPut("faqs/{id:guid}")]
    public Task<AdminFaqDto> UpdateFaq(Guid id, FaqRequest request, [FromServices] ICommandHandler<UpdateFaq, AdminFaqDto> handler, CancellationToken cancellationToken) =>
        handler.HandleAsync(new UpdateFaq(id, request.Question, request.Answer, request.Audience, request.SortOrder), cancellationToken);

    [HttpPost("faqs/{id:guid}/publish")]
    public Task<AdminFaqDto> PublishFaq(Guid id, [FromServices] ICommandHandler<SetFaqPublished, AdminFaqDto> handler, CancellationToken cancellationToken) =>
        handler.HandleAsync(new SetFaqPublished(id, IsPublished: true), cancellationToken);

    [HttpPost("faqs/{id:guid}/unpublish")]
    public Task<AdminFaqDto> UnpublishFaq(Guid id, [FromServices] ICommandHandler<SetFaqPublished, AdminFaqDto> handler, CancellationToken cancellationToken) =>
        handler.HandleAsync(new SetFaqPublished(id, IsPublished: false), cancellationToken);

    [HttpDelete("faqs/{id:guid}")]
    public async Task<IActionResult> DeleteFaqAsync(Guid id, [FromServices] ICommandHandler<DeleteFaq, bool> handler, CancellationToken cancellationToken)
    {
        await handler.HandleAsync(new DeleteFaq(id), cancellationToken);
        return NoContent();
    }
}
