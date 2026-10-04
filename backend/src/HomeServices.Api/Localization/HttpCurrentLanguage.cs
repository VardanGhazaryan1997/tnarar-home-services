using HomeServices.Application.Abstractions;
using HomeServices.Application.Languages;

namespace HomeServices.Api.Localization;

/// <summary>The languages <see cref="RequestLanguageMiddleware"/> chose for this request.</summary>
public sealed class HttpCurrentLanguage(IHttpContextAccessor accessor) : ICurrentLanguage
{
    public string Code => Read(RequestLanguageMiddleware.ItemKey);

    public string DefaultCode => Read(RequestLanguageMiddleware.DefaultItemKey);

    private string Read(string key) =>
        accessor.HttpContext?.Items[key] as string ?? LanguageCatalog.FallbackLanguage;
}
