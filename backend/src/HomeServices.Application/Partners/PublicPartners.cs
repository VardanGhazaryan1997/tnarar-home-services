using FluentValidation;
using HomeServices.Application.Abstractions;
using HomeServices.Application.Common;
using HomeServices.Application.Errors;
using HomeServices.Application.Files;
using HomeServices.Application.Messaging;
using HomeServices.Application.Reviews;
using HomeServices.Domain.Catalog;
using HomeServices.Domain.Files;
using HomeServices.Domain.Identity;
using HomeServices.Domain.Localization;
using HomeServices.Domain.Partners;
using Microsoft.EntityFrameworkCore;

namespace HomeServices.Application.Partners;

/// <summary>
/// Approved partners for visitors. Filters are slugs: <see cref="Category"/> includes its subcategories;
/// <see cref="District"/> needs <see cref="City"/>; partners serving the whole city, or its whole region, match any district.
/// An unknown or hidden category or place gives an empty list. Newest approvals first.
/// </summary>
public sealed record SearchPartners(
    string? Category = null,
    string? City = null,
    string? District = null,
    PartnerType? Type = null,
    string? Search = null,
    int Page = 1,
    int PageSize = SearchPartners.DefaultPageSize) : IQuery<PagedResult<PublicPartnerCardDto>>
{
    public const int DefaultPageSize = 20;
    public const int MaxPageSize = 50;
}

/// <summary>An approved partner's public profile, by slug.</summary>
public sealed record GetPublicPartner(string Slug) : IQuery<PublicPartnerDto>;

/// <summary>A photo or video, with signed links that expire (fetch the profile again rather than storing them).</summary>
public sealed record PublicMediaDto(string Kind, string? Caption, string Url, string? ThumbnailUrl, int? Width, int? Height);

public sealed record PublicCategoryDto(string Slug, string Name);

/// <summary>A place a partner works: a whole region (<see cref="RegionSlug"/> only), or a town or village with an optional district.</summary>
public sealed record PublicAreaDto(
    string? CitySlug,
    string? CityName,
    string? DistrictSlug,
    string? DistrictName,
    string? RegionSlug = null,
    string? RegionName = null);

/// <summary>A partner in search results. <see cref="Cover"/> is the first work example. <see cref="Id"/> is what a direct request is sent to.</summary>
public sealed record PublicPartnerCardDto(
    Guid Id,
    string Slug,
    string DisplayName,
    string Type,
    string AboutExcerpt,
    int? YearsOfExperience,
    PublicMediaDto? Avatar,
    PublicMediaDto? Cover,
    IReadOnlyList<PublicCategoryDto> Categories,
    IReadOnlyList<string> Cities,
    int WorkExampleCount,
    double? Rating,
    int ReviewCount);

public sealed record PublicPartnerDto(
    Guid Id,
    string Slug,
    string DisplayName,
    string Type,
    string About,
    int? YearsOfExperience,
    PublicMediaDto? Avatar,
    IReadOnlyList<PublicCategoryDto> Categories,
    IReadOnlyList<PublicAreaDto> Areas,
    IReadOnlyList<PublicMediaDto> WorkExamples,
    DateTimeOffset? MemberSince,
    double? Rating,
    int ReviewCount);

public sealed class SearchPartnersValidator : AbstractValidator<SearchPartners>
{
    public SearchPartnersValidator()
    {
        RuleFor(x => x.Type).IsInEnum().WithErrorCode("type.invalid");
        RuleFor(x => x.District).Empty().WithErrorCode("district.needs_city").When(x => string.IsNullOrWhiteSpace(x.City));
        RuleFor(x => x.Search).MaximumLength(100).WithErrorCode("search.too_long");
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1).WithErrorCode("page.invalid");
        RuleFor(x => x.PageSize).InclusiveBetween(1, SearchPartners.MaxPageSize).WithErrorCode("page_size.invalid");
    }
}

public sealed class SearchPartnersHandler(IAppDbContext db, ICurrentLanguage language, FileDtoFactory files)
    : IQueryHandler<SearchPartners, PagedResult<PublicPartnerCardDto>>
{
    public const int ExcerptLength = 200;

    public async Task<PagedResult<PublicPartnerCardDto>> HandleAsync(SearchPartners query, CancellationToken cancellationToken)
    {
        var empty = new PagedResult<PublicPartnerCardDto>([], query.Page, query.PageSize, 0);
        var partners = PublicPartnerQueries.Visible(db);

        if (!string.IsNullOrWhiteSpace(query.Category))
        {
            var categoryIds = await PublicPartnerQueries.CategoryWithChildrenAsync(db, query.Category, cancellationToken);
            if (categoryIds.Count == 0)
            {
                return empty;
            }

            partners = partners.Where(p => p.Services.Any(s => categoryIds.Contains(s.CategoryId)));
        }

        if (!string.IsNullOrWhiteSpace(query.City))
        {
            var slug = query.City.Trim().ToLowerInvariant();
            var city = await db.Cities.AsNoTracking().Include(c => c.Districts).SingleOrDefaultAsync(c => c.Slug == slug && c.IsActive, cancellationToken);
            Guid? districtId = null;
            if (!string.IsNullOrWhiteSpace(query.District))
            {
                var districtSlug = query.District.Trim().ToLowerInvariant();
                districtId = city?.Districts.SingleOrDefault(d => d.Slug == districtSlug && d.IsActive)?.Id;
                if (districtId is null)
                {
                    return empty;
                }
            }

            if (city is null)
            {
                return empty;
            }

            var cityId = city.Id;
            var regionId = city.RegionId;
            partners = partners.Where(p => p.Areas.Any(a =>
                (regionId != null && a.RegionId == regionId)
                || (a.CityId == cityId && (districtId == null || a.DistrictId == null || a.DistrictId == districtId))));
        }

        if (query.Type is { } type)
        {
            partners = partners.Where(p => p.Type == type);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim().ToLowerInvariant();
            partners = partners.Where(p => p.DisplayName.ToLower().Contains(search) || p.About.ToLower().Contains(search));
        }

        var total = await partners.CountAsync(cancellationToken);
        var page = await partners
            .OrderByDescending(p => p.ApprovedAt)
            .ThenBy(p => p.Id)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Include(p => p.Services)
            .Include(p => p.Areas)
            .Include(p => p.Media)
            .ToListAsync(cancellationToken);

        var lookup = await PublicPartnerLookup.LoadAsync(db, language, page, cancellationToken);
        var ratings = await ReviewViews.RatingsAsync(db, page.Select(p => p.Id).ToList(), cancellationToken);
        var items = new List<PublicPartnerCardDto>();
        foreach (var partner in page)
        {
            var workExamples = lookup.WorkExamples(partner);
            var rating = ratings.GetValueOrDefault(partner.Id, RatingSummary.None);
            items.Add(new PublicPartnerCardDto(
                partner.Id,
                partner.Slug!,
                partner.DisplayName,
                partner.Type.ToString(),
                partner.About.Length <= ExcerptLength ? partner.About : partner.About[..ExcerptLength].TrimEnd() + "…",
                partner.YearsOfExperience,
                await lookup.AvatarAsync(files, partner, cancellationToken),
                workExamples.Count == 0 ? null : await PublicPartnerLookup.MediaAsync(files, workExamples[0].File, workExamples[0].Media, cancellationToken),
                lookup.Categories(partner),
                lookup.Areas(partner).Select(a => (a.CityName ?? a.RegionName)!).Distinct().ToList(),
                workExamples.Count,
                rating.Rating,
                rating.ReviewCount));
        }

        return new PagedResult<PublicPartnerCardDto>(items, query.Page, query.PageSize, total);
    }
}

public sealed class GetPublicPartnerHandler(IAppDbContext db, ICurrentLanguage language, FileDtoFactory files)
    : IQueryHandler<GetPublicPartner, PublicPartnerDto>
{
    public async Task<PublicPartnerDto> HandleAsync(GetPublicPartner query, CancellationToken cancellationToken)
    {
        var slug = query.Slug.Trim().ToLowerInvariant();
        var partner = await PublicPartnerQueries.Visible(db)
            .Include(p => p.Services)
            .Include(p => p.Areas)
            .Include(p => p.Media)
            .SingleOrDefaultAsync(p => p.Slug == slug, cancellationToken)
            ?? throw new NotFoundException("Partner not found.", "partner.not_found");

        var lookup = await PublicPartnerLookup.LoadAsync(db, language, [partner], cancellationToken);
        var workExamples = new List<PublicMediaDto>();
        foreach (var (media, file) in lookup.WorkExamples(partner))
        {
            workExamples.Add(await PublicPartnerLookup.MediaAsync(files, file, media, cancellationToken));
        }

        var rating = (await ReviewViews.RatingsAsync(db, [partner.Id], cancellationToken)).GetValueOrDefault(partner.Id, RatingSummary.None);
        return new PublicPartnerDto(
            partner.Id,
            partner.Slug!,
            partner.DisplayName,
            partner.Type.ToString(),
            partner.About,
            partner.YearsOfExperience,
            await lookup.AvatarAsync(files, partner, cancellationToken),
            lookup.Categories(partner),
            lookup.Areas(partner),
            workExamples,
            partner.ApprovedAt,
            rating.Rating,
            rating.ReviewCount);
    }
}

internal static class PublicPartnerQueries
{
    /// <summary>Approved profiles whose owner isn't blocked.</summary>
    public static IQueryable<PartnerProfile> Visible(IAppDbContext db) =>
        db.PartnerProfiles.AsNoTracking()
            .Where(p => p.Status == PartnerStatus.Approved && p.Slug != null)
            .Where(p => db.Users.Any(u => u.Id == p.UserId && u.Status == UserStatus.Active));

    /// <summary>The active category with this slug and its active subcategories; empty when there's none.</summary>
    public static async Task<List<Guid>> CategoryWithChildrenAsync(IAppDbContext db, string slug, CancellationToken cancellationToken)
    {
        var normalized = slug.Trim().ToLowerInvariant();
        var category = await db.Categories.AsNoTracking().SingleOrDefaultAsync(c => c.Slug == normalized && c.IsActive, cancellationToken);
        if (category is null)
        {
            return [];
        }

        var children = await db.Categories.AsNoTracking()
            .Where(c => c.ParentId == category.Id && c.IsActive)
            .Select(c => c.Id)
            .ToListAsync(cancellationToken);
        return [category.Id, .. children];
    }
}

/// <summary>Catalog names (in the request language) and ready files for a page of partners, loaded in a few queries.</summary>
internal sealed class PublicPartnerLookup
{
    private readonly Dictionary<Guid, Category> _categories;
    private readonly Dictionary<Guid, City> _cities;
    private readonly Dictionary<Guid, Region> _regions;
    private readonly Dictionary<Guid, StoredFile> _files;
    private readonly ICurrentLanguage _language;

    private PublicPartnerLookup(
        Dictionary<Guid, Category> categories,
        Dictionary<Guid, City> cities,
        Dictionary<Guid, Region> regions,
        Dictionary<Guid, StoredFile> files,
        ICurrentLanguage language)
    {
        _categories = categories;
        _cities = cities;
        _regions = regions;
        _files = files;
        _language = language;
    }

    public static async Task<PublicPartnerLookup> LoadAsync(IAppDbContext db, ICurrentLanguage language, IReadOnlyCollection<PartnerProfile> partners, CancellationToken cancellationToken)
    {
        var categoryIds = partners.SelectMany(p => p.Services.Select(s => s.CategoryId)).Distinct().ToList();
        var cityIds = partners.SelectMany(p => p.Areas.Where(a => a.CityId != null).Select(a => a.CityId!.Value)).Distinct().ToList();
        var regionIds = partners.SelectMany(p => p.Areas.Where(a => a.RegionId != null).Select(a => a.RegionId!.Value)).Distinct().ToList();

        // Only what visitors may see: the avatar and work examples, never documents.
        var fileIds = partners
            .SelectMany(p => p.Media.Where(m => m.Kind == PartnerMediaKind.WorkExample).Select(m => m.FileId)
                .Concat(p.AvatarFileId is { } avatar ? new[] { avatar } : Array.Empty<Guid>()))
            .Distinct()
            .ToList();

        var categories = await db.Categories.AsNoTracking()
            .Where(c => categoryIds.Contains(c.Id) && c.IsActive)
            .ToDictionaryAsync(c => c.Id, cancellationToken);
        var cities = await db.Cities.AsNoTracking()
            .Include(c => c.Districts)
            .Where(c => cityIds.Contains(c.Id) && c.IsActive)
            .ToDictionaryAsync(c => c.Id, cancellationToken);
        var regions = regionIds.Count == 0
            ? new Dictionary<Guid, Region>()
            : await db.Regions.AsNoTracking().Where(r => regionIds.Contains(r.Id)).ToDictionaryAsync(r => r.Id, cancellationToken);
        var files = await db.Files.AsNoTracking()
            .Where(f => fileIds.Contains(f.Id) && f.Status == FileStatus.Ready)
            .ToDictionaryAsync(f => f.Id, cancellationToken);
        return new PublicPartnerLookup(categories, cities, regions, files, language);
    }

    public static async Task<PublicMediaDto> MediaAsync(FileDtoFactory files, StoredFile file, PartnerMedia? media, CancellationToken cancellationToken)
    {
        var dto = await files.CreateAsync(file, cancellationToken);
        return new PublicMediaDto(dto.Kind, media?.Caption, dto.Url!, dto.ThumbnailUrl, dto.Width, dto.Height);
    }

    public async Task<PublicMediaDto?> AvatarAsync(FileDtoFactory files, PartnerProfile partner, CancellationToken cancellationToken) =>
        partner.AvatarFileId is { } id && _files.TryGetValue(id, out var file)
            ? await MediaAsync(files, file, media: null, cancellationToken)
            : null;

    /// <summary>Work examples with ready files, in the partner's order.</summary>
    public IReadOnlyList<(PartnerMedia Media, StoredFile File)> WorkExamples(PartnerProfile partner) =>
        partner.Media
            .Where(m => m.Kind == PartnerMediaKind.WorkExample && _files.ContainsKey(m.FileId))
            .OrderBy(m => m.SortOrder)
            .Select(m => (m, _files[m.FileId]))
            .ToList();

    public IReadOnlyList<PublicCategoryDto> Categories(PartnerProfile partner) =>
        partner.Services
            .Select(s => _categories.GetValueOrDefault(s.CategoryId))
            .OfType<Category>()
            .OrderBy(c => c.SortOrder)
            .Select(c => new PublicCategoryDto(c.Slug, Localize(c.Name)))
            .ToList();

    /// <summary>Whole regions first, then towns and villages with their districts.</summary>
    public IReadOnlyList<PublicAreaDto> Areas(PartnerProfile partner) =>
    [
        .. partner.Areas
            .Select(a => a.RegionId is { } regionId ? _regions.GetValueOrDefault(regionId) : null)
            .OfType<Region>()
            .OrderBy(r => r.SortOrder)
            .Select(r => new PublicAreaDto(null, null, null, null, r.Slug, Localize(r.Name))),
        .. CityAreas(partner),
    ];

    private IEnumerable<PublicAreaDto> CityAreas(PartnerProfile partner) =>
        partner.Areas
            .Where(a => a.CityId is not null)
            .Select(a => (Area: a, City: _cities.GetValueOrDefault(a.CityId!.Value)))
            .Where(x => x.City is not null)
            .Select(x => (x.Area, City: x.City!, District: x.City!.Districts.FirstOrDefault(d => d.Id == x.Area.DistrictId && d.IsActive)))
            .Where(x => x.Area.DistrictId is null || x.District is not null)
            .OrderBy(x => x.City.SortOrder)
            .ThenBy(x => x.District?.SortOrder ?? -1)
            .Select(x => new PublicAreaDto(x.City.Slug, Localize(x.City.Name), x.District?.Slug, x.District is null ? null : Localize(x.District.Name)));

    private string Localize(LocalizedText text) => text.Get(_language.Code, _language.DefaultCode);
}
