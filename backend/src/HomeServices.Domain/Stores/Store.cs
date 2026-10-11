using HomeServices.Domain.Common;
using HomeServices.Domain.Localization;

namespace HomeServices.Domain.Stores;

/// <summary>
/// One branch of a store as staff enter it; <see cref="Store.SetBranches"/> turns the list into <see cref="StoreBranch"/> rows.
/// </summary>
public sealed record BranchInput(string Name, Guid CityId, Guid? DistrictId, string Address, string? Phone, string? OpeningHours);

/// <summary>
/// A building-materials store or chain whose prices customers compare. Staff keep the store, its branches and its
/// products in the Back Office; a store has one price list (the chain price) and a list of branches.
/// </summary>
public sealed class Store : AuditableEntity, IAudited
{
    public const int MaxNameLength = 100;
    public const int MaxWebsiteLength = 300;
    public const int MaxPhoneLength = 30;
    public const int MaxBranches = 50;

    private readonly List<StoreBranch> _branches = [];

    private Store()
    {
    }

    public string Slug { get; private set; } = string.Empty;

    /// <summary>The store's brand name: the same in every language.</summary>
    public string Name { get; private set; } = string.Empty;

    public LocalizedText Description { get; private set; } = LocalizedText.Empty;

    /// <summary>The store's web address (http or https), shown as plain information.</summary>
    public string? Website { get; private set; }

    public string? Phone { get; private set; }

    public Guid? LogoFileId { get; private set; }

    public int SortOrder { get; private set; }

    /// <summary>Hidden stores and their products aren't shown to customers or counted in material prices.</summary>
    public bool IsActive { get; private set; }

    public IReadOnlyCollection<StoreBranch> Branches => _branches.AsReadOnly();

    public static Store Create(string slug, string name, LocalizedText description, string? website, string? phone, Guid? logoFileId, int sortOrder)
    {
        var store = new Store { IsActive = true };
        store.Update(slug, name, description, website, phone, logoFileId, sortOrder);
        return store;
    }

    /// <summary>Changes every edited field at once. The caller checks that the slug is free and that the logo file exists.</summary>
    public void Update(string slug, string name, LocalizedText description, string? website, string? phone, Guid? logoFileId, int sortOrder)
    {
        var trimmedName = name?.Trim() ?? string.Empty;
        if (trimmedName.Length == 0)
        {
            throw new DomainException("store.name_required", "A store needs a name.");
        }

        if (trimmedName.Length > MaxNameLength)
        {
            throw new DomainException("store.invalid", $"A store name is at most {MaxNameLength} characters.");
        }

        var cleanWebsite = Clean(website);
        if (cleanWebsite is not null && (cleanWebsite.Length > MaxWebsiteLength || !IsWebAddress(cleanWebsite)))
        {
            throw new DomainException("store.website_invalid", $"The website is an http or https address of at most {MaxWebsiteLength} characters.");
        }

        var cleanPhone = Clean(phone);
        if (cleanPhone is { Length: > MaxPhoneLength })
        {
            throw new DomainException("store.invalid", $"A phone number is at most {MaxPhoneLength} characters.");
        }

        Slug = Common.Slug.Normalize(slug, "store.slug_invalid");
        Name = trimmedName;
        Description = description;
        Website = cleanWebsite;
        Phone = cleanPhone;
        LogoFileId = logoFileId;
        SortOrder = sortOrder;
    }

    /// <summary>
    /// Replaces the whole branch list. The caller checks that the towns and districts exist (and that each district is in
    /// its town).
    /// </summary>
    public void SetBranches(IEnumerable<BranchInput> branches)
    {
        var created = new List<StoreBranch>();
        foreach (var input in branches)
        {
            created.Add(StoreBranch.Create(Id, input, created.Count + 1));
        }

        if (created.Count > MaxBranches)
        {
            throw new DomainException("store.too_many_branches", $"A store has at most {MaxBranches} branches.");
        }

        _branches.Clear();
        _branches.AddRange(created);
    }

    public void Activate() => IsActive = true;

    public void Deactivate() => IsActive = false;

    internal static string? Clean(string? value)
    {
        var trimmed = value?.Trim();
        return string.IsNullOrEmpty(trimmed) ? null : trimmed;
    }

    private static bool IsWebAddress(string value) =>
        Uri.TryCreate(value, UriKind.Absolute, out var uri) && uri.Scheme is "http" or "https";
}
