using HomeServices.Application.Languages;
using HomeServices.Application.Messaging;
using Microsoft.AspNetCore.Mvc;

namespace HomeServices.Api.Controllers;

[ApiController]
[Route("api/v1/languages")]
public sealed class LanguagesController : ControllerBase
{
    /// <summary>Active languages for the language switcher, in display order.</summary>
    [HttpGet]
    public Task<IReadOnlyList<LanguageDto>> GetActive(
        [FromServices] IQueryHandler<GetActiveLanguages, IReadOnlyList<LanguageDto>> handler,
        CancellationToken cancellationToken) =>
        handler.HandleAsync(new GetActiveLanguages(), cancellationToken);
}
