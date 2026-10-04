using HomeServices.Application.Messaging;
using HomeServices.Application.Translations;
using Microsoft.AspNetCore.Mvc;

namespace HomeServices.Api.Controllers;

/// <summary>Interface texts for the Portal and Back Office (loaded by i18next-http-backend). No sign-in.</summary>
[ApiController]
[Route("api/v1/i18n")]
public sealed class UiTextsController : ControllerBase
{
    /// <summary>
    /// The texts of <paramref name="ns"/> ("portal" or "backoffice") in <paramref name="lng"/> as nested JSON; missing
    /// texts come in the default language. Supports ETag / If-None-Match (304).
    /// </summary>
    [HttpGet("{lng}/{ns}")]
    [Produces("application/json")]
    public async Task<IActionResult> GetAsync(
        string lng,
        string ns,
        [FromServices] IQueryHandler<GetUiTexts, UiTextsDto> handler,
        CancellationToken cancellationToken)
    {
        var texts = await handler.HandleAsync(new GetUiTexts(lng, ns), cancellationToken);

        Response.Headers.ETag = texts.ETag;
        Response.Headers.CacheControl = "public, max-age=300";
        if (Request.Headers.IfNoneMatch.Any(tag => tag == texts.ETag))
        {
            return StatusCode(StatusCodes.Status304NotModified);
        }

        return Content(texts.Json, "application/json; charset=utf-8");
    }
}
