using System.Text;
using System.Text.Json;
using HomeServices.Api.Authorization;
using HomeServices.Application.Common;
using HomeServices.Application.Messaging;
using HomeServices.Application.Translations;
using HomeServices.Domain.Staff;
using Microsoft.AspNetCore.Mvc;

namespace HomeServices.Api.Controllers.Admin;

/// <summary>
/// Interface texts per namespace ("portal", "backoffice") and language (needs <c>translations.manage</c>).
/// The default language's keys are the full set; other languages fall back to it.
/// </summary>
[ApiController]
[Route("api/v1/admin/translations")]
[HasPermission(Permissions.TranslationsManage)]
public sealed class AdminTranslationsController : ControllerBase
{
    public sealed record SaveRequest(Dictionary<string, string?> Values);

    /// <summary>Namespaces with their key count and each language's progress.</summary>
    [HttpGet]
    public Task<IReadOnlyList<TranslationNamespaceDto>> Namespaces(
        [FromServices] IQueryHandler<GetTranslationNamespaces, IReadOnlyList<TranslationNamespaceDto>> handler,
        CancellationToken cancellationToken) =>
        handler.HandleAsync(new GetTranslationNamespaces(), cancellationToken);

    /// <summary>Missing keys per namespace for every active language.</summary>
    [HttpGet("missing")]
    public Task<IReadOnlyList<MissingTranslationsDto>> Missing(
        [FromServices] IQueryHandler<GetMissingTranslations, IReadOnlyList<MissingTranslationsDto>> handler,
        CancellationToken cancellationToken) =>
        handler.HandleAsync(new GetMissingTranslations(), cancellationToken);

    /// <summary>Keys of a namespace with their texts. <c>missingIn=ru</c> keeps keys without a Russian text; <c>search</c> matches keys and texts.</summary>
    [HttpGet("{ns}")]
    public Task<PagedResult<TranslationRowDto>> List(
        string ns,
        [FromServices] IQueryHandler<GetTranslations, PagedResult<TranslationRowDto>> handler,
        [FromQuery] string? search,
        [FromQuery] string? missingIn,
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        CancellationToken cancellationToken) =>
        handler.HandleAsync(new GetTranslations(ns, search, missingIn, page ?? 1, pageSize ?? GetTranslations.DefaultPageSize), cancellationToken);

    /// <summary>Creates a key or changes its texts: <c>{ "values": { "hy": "…", "ru": "…" } }</c>; an empty text removes it.</summary>
    [HttpPut("{ns}/keys/{key}")]
    public Task<TranslationRowDto> Save(
        string ns,
        string key,
        SaveRequest request,
        [FromServices] ICommandHandler<SaveTranslation, TranslationRowDto> handler,
        CancellationToken cancellationToken) =>
        handler.HandleAsync(new SaveTranslation(ns, key, request.Values), cancellationToken);

    /// <summary>Removes a key in every language.</summary>
    [HttpDelete("{ns}/keys/{key}")]
    public async Task<IActionResult> DeleteAsync(
        string ns,
        string key,
        [FromServices] ICommandHandler<DeleteTranslationKey, bool> handler,
        CancellationToken cancellationToken)
    {
        await handler.HandleAsync(new DeleteTranslationKey(ns, key), cancellationToken);
        return NoContent();
    }

    /// <summary>Downloads a language's texts as an i18next JSON file. <c>withFallback=true</c> fills gaps with the default language.</summary>
    [HttpGet("{ns}/export/{lng}")]
    public async Task<IActionResult> ExportAsync(
        string ns,
        string lng,
        [FromQuery] bool? withFallback,
        [FromServices] IQueryHandler<ExportTranslations, string> handler,
        CancellationToken cancellationToken)
    {
        var json = await handler.HandleAsync(new ExportTranslations(ns, lng, withFallback ?? false), cancellationToken);
        return File(Encoding.UTF8.GetBytes(json), "application/json", $"{ns}.{lng}.json");
    }

    /// <summary>
    /// Loads an i18next JSON file (the request body). The default language creates keys; others only fill existing keys.
    /// <c>replace=true</c> also removes texts missing from the file.
    /// </summary>
    [HttpPost("{ns}/import/{lng}")]
    public Task<TranslationImportResultDto> Import(
        string ns,
        string lng,
        [FromBody] JsonElement content,
        [FromQuery] bool? replace,
        [FromServices] ICommandHandler<ImportTranslations, TranslationImportResultDto> handler,
        CancellationToken cancellationToken) =>
        handler.HandleAsync(new ImportTranslations(ns, lng, content, replace ?? false), cancellationToken);
}
