using HomeServices.Application.Languages;

namespace HomeServices.Api.Localization;

/// <summary>
/// Picks the request language from Accept-Language (among active languages),
/// stores it for <see cref="HttpCurrentLanguage"/>, and returns it in Content-Language.
/// </summary>
public sealed class RequestLanguageMiddleware(RequestDelegate next)
{
    internal const string ItemKey = "RequestLanguage";
    internal const string DefaultItemKey = "DefaultLanguage";

    public async Task InvokeAsync(HttpContext context, ILanguageCatalog catalog)
    {
        var languages = await catalog.GetAsync(context.RequestAborted);
        var language = LanguageNegotiator.Pick(
            context.Request.Headers.AcceptLanguage.ToString(),
            languages.ActiveCodes,
            languages.DefaultCode);

        context.Items[ItemKey] = language;
        // Texts missing in the request language fall back to this one (English for ar/fa/hi, else the default).
        context.Items[DefaultItemKey] = languages.FallbackFor(language);
        context.Response.OnStarting(() =>
        {
            context.Response.Headers.ContentLanguage = language;
            return Task.CompletedTask;
        });

        await next(context);
    }
}
