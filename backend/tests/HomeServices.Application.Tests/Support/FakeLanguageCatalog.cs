using HomeServices.Application.Languages;

namespace HomeServices.Application.Tests.Support;

public sealed class FakeLanguageCatalog(string defaultCode = "hy", params string[] activeCodes) : ILanguageCatalog
{
    public Task<LanguageCatalog> GetAsync(CancellationToken cancellationToken) =>
        Task.FromResult(new LanguageCatalog(activeCodes.Length > 0 ? activeCodes : ["hy", "ru", "en"], defaultCode));

    public int Invalidations { get; private set; }

    public void Invalidate() => Invalidations++;
}
