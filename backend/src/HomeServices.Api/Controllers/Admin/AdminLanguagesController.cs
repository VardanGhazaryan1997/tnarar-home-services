using HomeServices.Api.Authorization;
using HomeServices.Application.Messaging;
using HomeServices.Application.Translations;
using HomeServices.Domain.Staff;
using Microsoft.AspNetCore.Mvc;

namespace HomeServices.Api.Controllers.Admin;

/// <summary>Languages of the platform (needs <c>translations.manage</c>). A new language starts hidden: translate, then activate.</summary>
[ApiController]
[Route("api/v1/admin/languages")]
[HasPermission(Permissions.TranslationsManage)]
public sealed class AdminLanguagesController : ControllerBase
{
    public sealed record CreateRequest(string Code, string Name, string NativeName, int SortOrder);

    public sealed record UpdateRequest(string Name, string NativeName, int SortOrder);

    [HttpGet]
    public Task<IReadOnlyList<AdminLanguageDto>> List(
        [FromServices] IQueryHandler<GetAdminLanguages, IReadOnlyList<AdminLanguageDto>> handler,
        CancellationToken cancellationToken) =>
        handler.HandleAsync(new GetAdminLanguages(), cancellationToken);

    /// <summary>Adds a language (inactive), e.g. <c>{ "code": "fr", "name": "French", "nativeName": "Français", "sortOrder": 4 }</c>.</summary>
    [HttpPost]
    public async Task<ActionResult<AdminLanguageDto>> CreateAsync(
        CreateRequest request,
        [FromServices] ICommandHandler<CreateLanguage, AdminLanguageDto> handler,
        CancellationToken cancellationToken) =>
        StatusCode(
            StatusCodes.Status201Created,
            await handler.HandleAsync(new CreateLanguage(request.Code, request.Name, request.NativeName, request.SortOrder), cancellationToken));

    [HttpPut("{code}")]
    public Task<AdminLanguageDto> Update(
        string code,
        UpdateRequest request,
        [FromServices] ICommandHandler<UpdateLanguage, AdminLanguageDto> handler,
        CancellationToken cancellationToken) =>
        handler.HandleAsync(new UpdateLanguage(code, request.Name, request.NativeName, request.SortOrder), cancellationToken);

    /// <summary>Shows the language in the switcher. Missing texts appear in the default language.</summary>
    [HttpPost("{code}/activate")]
    public Task<AdminLanguageDto> Activate(string code, [FromServices] ICommandHandler<SetLanguageActive, AdminLanguageDto> handler, CancellationToken cancellationToken) =>
        handler.HandleAsync(new SetLanguageActive(code, IsActive: true), cancellationToken);

    /// <summary>Hides the language. The default language can't be hidden.</summary>
    [HttpPost("{code}/deactivate")]
    public Task<AdminLanguageDto> Deactivate(string code, [FromServices] ICommandHandler<SetLanguageActive, AdminLanguageDto> handler, CancellationToken cancellationToken) =>
        handler.HandleAsync(new SetLanguageActive(code, IsActive: false), cancellationToken);
}
