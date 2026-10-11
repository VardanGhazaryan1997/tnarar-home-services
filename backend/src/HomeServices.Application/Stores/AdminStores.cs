using System.Text.RegularExpressions;
using FluentValidation;
using HomeServices.Application.Abstractions;
using HomeServices.Application.Catalog;
using HomeServices.Application.Files;
using HomeServices.Application.Messaging;
using HomeServices.Domain;
using HomeServices.Domain.Localization;
using HomeServices.Domain.Stores;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace HomeServices.Application.Stores;

/// <summary>Stores for the Back Office, in display order, hidden ones too. <c>Search</c> matches name and slug.</summary>
public sealed record GetAdminStores(string? Search = null, bool? IsActive = null) : IQuery<IReadOnlyList<AdminStoreDto>>;

/// <summary>One store with its branches.</summary>
public sealed record GetAdminStore(Guid Id) : IQuery<AdminStoreDto>;

/// <summary>The fields a staff member edits on a store.</summary>
public interface IStoreFields
{
    string Slug { get; }

    /// <summary>The brand name, the same in every language.</summary>
    string Name { get; }

    /// <summary>Optional text per language code.</summary>
    IReadOnlyDictionary<string, string>? Description { get; }

    string? Website { get; }

    string? Phone { get; }

    /// <summary>An uploaded image (see "files/uploads").</summary>
    Guid? LogoFileId { get; }

    int SortOrder { get; }

    /// <summary>Hidden stores aren't shown to customers or counted in material prices.</summary>
    bool IsActive { get; }
}

public sealed record CreateStore(
    string Slug,
    string Name,
    IReadOnlyDictionary<string, string>? Description,
    string? Website,
    string? Phone,
    Guid? LogoFileId,
    int SortOrder,
    bool IsActive = true) : ICommand<AdminStoreDto>, IStoreFields;

public sealed record UpdateStore(
    Guid Id,
    string Slug,
    string Name,
    IReadOnlyDictionary<string, string>? Description,
    string? Website,
    string? Phone,
    Guid? LogoFileId,
    int SortOrder,
    bool IsActive = true) : ICommand<AdminStoreDto>, IStoreFields;

/// <summary>Deletes a store without products; 422 "store.has_products" otherwise (hide it instead).</summary>
public sealed record DeleteStore(Guid Id) : ICommand<bool>;

/// <summary>A branch as staff enter it.</summary>
public sealed record StoreBranchInput(string Name, Guid CityId, Guid? DistrictId, string Address, string? Phone = null, string? OpeningHours = null);

/// <summary>Replaces the store's branches with <paramref name="Branches"/> (in this order) and returns the store.</summary>
public sealed record SetStoreBranches(Guid StoreId, IReadOnlyList<StoreBranchInput> Branches) : ICommand<AdminStoreDto>;

public static partial class StoreRules
{
    public const int DescriptionMaxLength = 2000;

    public static bool IsWebAddress(string? value) =>
        string.IsNullOrWhiteSpace(value)
        || (value.Trim().Length <= Store.MaxWebsiteLength
            && Uri.TryCreate(value.Trim(), UriKind.Absolute, out var uri)
            && uri.Scheme is "http" or "https");

    public static bool IsValidBranch(StoreBranchInput? branch) =>
        branch is not null
        && !string.IsNullOrWhiteSpace(branch.Name) && branch.Name.Trim().Length <= StoreBranch.MaxNameLength
        && !string.IsNullOrWhiteSpace(branch.Address) && branch.Address.Trim().Length <= StoreBranch.MaxAddressLength
        && branch.CityId != Guid.Empty
        && (branch.Phone?.Trim().Length ?? 0) <= StoreBranch.MaxPhoneLength
        && (branch.OpeningHours?.Trim().Length ?? 0) <= StoreBranch.MaxOpeningHoursLength;

    [GeneratedRegex("^[a-z]{2,3}(-[a-z0-9]{2,8})?$")]
    private static partial Regex LanguageCode();

    public static bool IsLanguageCode(string? code) => code is not null && LanguageCode().IsMatch(code.Trim().ToLowerInvariant());
}

public abstract class StoreFieldsValidator<T> : AbstractValidator<T>
    where T : IStoreFields
{
    protected StoreFieldsValidator()
    {
        RuleFor(x => x.Slug).ValidSlug();
        RuleFor(x => x.Name).Must(name => !string.IsNullOrWhiteSpace(name)).WithErrorCode("name.required");
        RuleFor(x => x.Name).Must(name => (name?.Trim().Length ?? 0) <= Store.MaxNameLength).WithErrorCode("name.too_long");
        RuleFor(x => x.Description)
            .Must(text => text is null || text.Keys.All(StoreRules.IsLanguageCode)).WithErrorCode("description.language_invalid");
        RuleFor(x => x.Description)
            .Must(text => text is null || text.Values.All(value => (value?.Trim().Length ?? 0) <= StoreRules.DescriptionMaxLength))
            .WithErrorCode("description.too_long");
        RuleFor(x => x.Website).Must(StoreRules.IsWebAddress).WithErrorCode("website.invalid");
        RuleFor(x => x.Phone).Must(phone => (phone?.Trim().Length ?? 0) <= Store.MaxPhoneLength).WithErrorCode("phone.too_long");
        RuleFor(x => x.SortOrder).ValidSortOrder();
    }
}

public sealed class CreateStoreValidator : StoreFieldsValidator<CreateStore>;

public sealed class UpdateStoreValidator : StoreFieldsValidator<UpdateStore>;

public sealed class SetStoreBranchesValidator : AbstractValidator<SetStoreBranches>
{
    public SetStoreBranchesValidator()
    {
        RuleFor(x => x.Branches).NotNull().WithErrorCode("branches.required");
        RuleFor(x => x.Branches).Must(branches => branches is null || branches.Count <= Store.MaxBranches).WithErrorCode("branches.too_many");
        RuleForEach(x => x.Branches).Must(StoreRules.IsValidBranch).WithErrorCode("branch.invalid");
    }
}

public sealed class GetAdminStoresHandler(IAppDbContext db, FileDtoFactory files, IOptions<StorePricingSettings> settings, TimeProvider clock)
    : IQueryHandler<GetAdminStores, IReadOnlyList<AdminStoreDto>>
{
    public async Task<IReadOnlyList<AdminStoreDto>> HandleAsync(GetAdminStores query, CancellationToken cancellationToken)
    {
        var stores = db.Stores.AsNoTracking();
        if (query.IsActive is { } isActive)
        {
            stores = stores.Where(s => s.IsActive == isActive);
        }

        if (query.Search?.Trim().ToLowerInvariant() is { Length: > 0 } term)
        {
            stores = stores.Where(s => s.Name.ToLower().Contains(term) || s.Slug.Contains(term));
        }

        var list = await stores.OrderBy(s => s.SortOrder).ThenBy(s => s.Name).ToListAsync(cancellationToken);
        return await StoreData.StoreDtosAsync(db, files, settings.Value, clock.GetUtcNow(), list, withBranches: false, cancellationToken);
    }
}

public sealed class GetAdminStoreHandler(IAppDbContext db, FileDtoFactory files, IOptions<StorePricingSettings> settings, TimeProvider clock)
    : IQueryHandler<GetAdminStore, AdminStoreDto>
{
    public async Task<AdminStoreDto> HandleAsync(GetAdminStore query, CancellationToken cancellationToken)
    {
        var store = await StoreData.LoadAsync(db, query.Id, withBranches: true, cancellationToken);
        return await StoreData.StoreDtoAsync(db, files, settings.Value, clock.GetUtcNow(), store, withBranches: true, cancellationToken);
    }
}

public sealed class CreateStoreHandler(IAppDbContext db, FileDtoFactory files, IOptions<StorePricingSettings> settings, TimeProvider clock)
    : ICommandHandler<CreateStore, AdminStoreDto>
{
    public async Task<AdminStoreDto> HandleAsync(CreateStore command, CancellationToken cancellationToken)
    {
        await StoreData.EnsureSlugFreeAsync(db, command.Slug, exceptId: null, cancellationToken);
        await StoreData.EnsureImageAsync(db, command.LogoFileId, cancellationToken);
        var store = Store.Create(
            command.Slug,
            command.Name,
            LocalizedText.From(command.Description ?? new Dictionary<string, string>()),
            command.Website,
            command.Phone,
            command.LogoFileId,
            command.SortOrder);
        if (!command.IsActive)
        {
            store.Deactivate();
        }

        db.Stores.Add(store);
        await db.SaveChangesAsync(cancellationToken);
        return await StoreData.StoreDtoAsync(db, files, settings.Value, clock.GetUtcNow(), store, withBranches: true, cancellationToken);
    }
}

public sealed class UpdateStoreHandler(IAppDbContext db, FileDtoFactory files, IOptions<StorePricingSettings> settings, TimeProvider clock)
    : ICommandHandler<UpdateStore, AdminStoreDto>
{
    public async Task<AdminStoreDto> HandleAsync(UpdateStore command, CancellationToken cancellationToken)
    {
        var store = await StoreData.LoadAsync(db, command.Id, withBranches: true, cancellationToken);
        await StoreData.EnsureSlugFreeAsync(db, command.Slug, store.Id, cancellationToken);
        await StoreData.EnsureImageAsync(db, command.LogoFileId, cancellationToken);
        var wasActive = store.IsActive;
        store.Update(
            command.Slug,
            command.Name,
            LocalizedText.From(command.Description ?? new Dictionary<string, string>()),
            command.Website,
            command.Phone,
            command.LogoFileId,
            command.SortOrder);
        if (command.IsActive)
        {
            store.Activate();
        }
        else
        {
            store.Deactivate();
        }

        await db.SaveChangesAsync(cancellationToken);
        if (store.IsActive != wasActive)
        {
            // Hidden stores' products stop counting in material prices (and count again when shown).
            var materialIds = await db.StoreProducts.Where(p => p.StoreId == store.Id).Select(p => p.MaterialId).Distinct().ToListAsync(cancellationToken);
            await StoreData.RefreshMaterialPricesAsync(db, settings.Value, clock, materialIds, cancellationToken);
        }

        return await StoreData.StoreDtoAsync(db, files, settings.Value, clock.GetUtcNow(), store, withBranches: true, cancellationToken);
    }
}

public sealed class DeleteStoreHandler(IAppDbContext db) : ICommandHandler<DeleteStore, bool>
{
    public async Task<bool> HandleAsync(DeleteStore command, CancellationToken cancellationToken)
    {
        var store = await StoreData.LoadAsync(db, command.Id, withBranches: true, cancellationToken);
        if (await db.StoreProducts.AnyAsync(p => p.StoreId == store.Id, cancellationToken))
        {
            throw new DomainException("store.has_products", "This store has products. Hide it instead.");
        }

        db.Stores.Remove(store);
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }
}

public sealed class SetStoreBranchesHandler(IAppDbContext db, FileDtoFactory files, IOptions<StorePricingSettings> settings, TimeProvider clock)
    : ICommandHandler<SetStoreBranches, AdminStoreDto>
{
    public async Task<AdminStoreDto> HandleAsync(SetStoreBranches command, CancellationToken cancellationToken)
    {
        var store = await StoreData.LoadAsync(db, command.StoreId, withBranches: true, cancellationToken);

        var cityIds = command.Branches.Select(b => b.CityId).Distinct().ToList();
        if (await db.Cities.CountAsync(c => cityIds.Contains(c.Id), cancellationToken) != cityIds.Count)
        {
            throw new DomainException("store_branch.city_not_found", "Some of the towns no longer exist.");
        }

        var districtIds = command.Branches.Select(b => b.DistrictId).OfType<Guid>().Distinct().ToList();
        var cityOfDistrict = districtIds.Count == 0
            ? new Dictionary<Guid, Guid>()
            : await db.Districts.AsNoTracking().Where(d => districtIds.Contains(d.Id)).ToDictionaryAsync(d => d.Id, d => d.CityId, cancellationToken);
        if (command.Branches.Any(b => b.DistrictId is { } district && (!cityOfDistrict.TryGetValue(district, out var city) || city != b.CityId)))
        {
            throw new DomainException("store_branch.district_invalid", "A district isn't in the branch's town.");
        }

        store.SetBranches(command.Branches.Select(b => new BranchInput(b.Name, b.CityId, b.DistrictId, b.Address, b.Phone, b.OpeningHours)));
        await db.SaveChangesAsync(cancellationToken);
        return await StoreData.StoreDtoAsync(db, files, settings.Value, clock.GetUtcNow(), store, withBranches: true, cancellationToken);
    }
}
