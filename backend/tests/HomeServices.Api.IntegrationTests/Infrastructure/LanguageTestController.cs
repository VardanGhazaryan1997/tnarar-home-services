using HomeServices.Application.Abstractions;
using Microsoft.AspNetCore.Mvc;

namespace HomeServices.Api.IntegrationTests.Infrastructure;

/// <summary>Test-only endpoint echoing the language the API picked for the request.</summary>
[ApiController]
[Route("api/v1/test/language")]
public sealed class LanguageTestController(ICurrentLanguage currentLanguage) : ControllerBase
{
    [HttpGet]
    public string Get() => currentLanguage.Code;
}
