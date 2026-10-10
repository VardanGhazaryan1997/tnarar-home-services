using HomeServices.Application.Abstractions;
using HomeServices.Application.Files;
using HomeServices.Domain.Catalog;
using HomeServices.Domain.Files;
using HomeServices.Domain.Localization;
using HomeServices.Domain.Requests;
using Microsoft.EntityFrameworkCore;

namespace HomeServices.Application.Requests;

/// <summary>Category and place names (in the request language) for a set of requests, loaded in two queries.</summary>
internal sealed class RequestLookup
{
    public const int ExcerptLength = 160;

    private readonly Dictionary<Guid, Category> _categories;
    private readonly Dictionary<Guid, City> _cities;
    private readonly ICurrentLanguage _language;

    private RequestLookup(Dictionary<Guid, Category> categories, Dictionary<Guid, City> cities, ICurrentLanguage language)
    {
        _categories = categories;
        _cities = cities;
        _language = language;
    }

    public static async Task<RequestLookup> LoadAsync(IAppDbContext db, ICurrentLanguage language, IEnumerable<ServiceRequest> requests, CancellationToken cancellationToken)
    {
        var list = requests.ToList();
        var categoryIds = list.Select(r => r.CategoryId).Distinct().ToList();
        var cityIds = list.Select(r => r.CityId).Distinct().ToList();

        // Hidden categories and places still name old requests.
        var categories = await db.Categories.AsNoTracking().IgnoreQueryFilters()
            .Where(c => categoryIds.Contains(c.Id))
            .ToDictionaryAsync(c => c.Id, cancellationToken);
        var cities = await db.Cities.AsNoTracking()
            .Include(c => c.Districts)
            .Where(c => cityIds.Contains(c.Id))
            .ToDictionaryAsync(c => c.Id, cancellationToken);
        return new RequestLookup(categories, cities, language);
    }

    public static string Excerpt(string description) =>
        description.Length <= ExcerptLength ? description : $"{description[..(ExcerptLength - 1)].TrimEnd()}…";

    /// <summary>The request's photos and videos that are still available, with fresh links.</summary>
    public static async Task<IReadOnlyList<FileDto>> MediaAsync(IAppDbContext db, FileDtoFactory files, ServiceRequest request, CancellationToken cancellationToken)
    {
        var ids = request.Media.OrderBy(m => m.SortOrder).Select(m => m.FileId).ToList();
        var stored = await db.Files.AsNoTracking()
            .Where(f => ids.Contains(f.Id) && f.Status == FileStatus.Ready)
            .ToDictionaryAsync(f => f.Id, cancellationToken);

        var result = new List<FileDto>();
        foreach (var id in ids.Where(stored.ContainsKey))
        {
            result.Add(await files.CreateAsync(stored[id], cancellationToken));
        }

        return result;
    }

    /// <summary>
    /// The request's lines in order, work named in the request language (removed work keeps its name). The estimated
    /// ranges only when <paramref name="withEstimate"/> (the customer's view).
    /// </summary>
    public static async Task<IReadOnlyList<RequestLineDto>> LinesAsync(
        IAppDbContext db, ICurrentLanguage language, ServiceRequest request, bool withEstimate, CancellationToken cancellationToken)
    {
        if (request.Lines.Count == 0)
        {
            return [];
        }

        var ids = request.Lines.Select(l => l.WorkItemId).Distinct().ToList();
        var names = await db.WorkItems.AsNoTracking().IgnoreQueryFilters()
            .Where(w => ids.Contains(w.Id))
            .ToDictionaryAsync(w => w.Id, w => w.Name, cancellationToken);
        return request.Lines
            .OrderBy(l => l.SortOrder)
            .Select(l => new RequestLineDto(
                l.Id,
                l.RoomName,
                l.WorkItemId,
                names.GetValueOrDefault(l.WorkItemId)?.Get(language.Code, language.DefaultCode) ?? string.Empty,
                l.Unit,
                l.Quantity,
                withEstimate ? l.EstimateMin : null,
                withEstimate ? l.EstimateMax : null))
            .ToList();
    }

    public RequestPlaceDto Place(ServiceRequest request)
    {
        var city = _cities.GetValueOrDefault(request.CityId);
        var district = request.DistrictId is { } districtId ? city?.Districts.FirstOrDefault(d => d.Id == districtId) : null;
        return new RequestPlaceDto(
            request.CategoryId,
            Localize(_categories.GetValueOrDefault(request.CategoryId)?.Name),
            request.CityId,
            Localize(city?.Name),
            request.DistrictId,
            district is null ? null : Localize(district.Name));
    }

    private string Localize(LocalizedText? text) => text?.Get(_language.Code, _language.DefaultCode) ?? string.Empty;
}
