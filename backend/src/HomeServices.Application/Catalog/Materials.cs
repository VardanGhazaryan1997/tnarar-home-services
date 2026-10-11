using FluentValidation;
using HomeServices.Application.Abstractions;
using HomeServices.Application.Errors;
using HomeServices.Application.Languages;
using HomeServices.Application.Messaging;
using HomeServices.Domain;
using HomeServices.Domain.Catalog;
using HomeServices.Domain.Localization;
using Microsoft.EntityFrameworkCore;

namespace HomeServices.Application.Catalog;

/// <summary>Every material (hidden ones too) with all translations, in display order, for the Back Office.</summary>
public sealed record GetAdminMaterials : IQuery<IReadOnlyList<AdminMaterialDto>>;

/// <summary>A material with its prices per unit (AMD) and <see cref="WorkItemCount"/>: how many work items use it.</summary>
public sealed record AdminMaterialDto(
    Guid Id,
    string Slug,
    IReadOnlyDictionary<string, string> Name,
    MaterialUnit Unit,
    int PriceBudget,
    int PriceStandard,
    int PricePremium,
    int SortOrder,
    bool IsActive,
    int WorkItemCount = 0)
{
    public static AdminMaterialDto From(Material material, int workItemCount = 0) =>
        new(
            material.Id,
            material.Slug,
            AdminCategoryDto.Translations(material.Name),
            material.Unit,
            material.PriceBudget,
            material.PriceStandard,
            material.PricePremium,
            material.SortOrder,
            material.IsActive,
            workItemCount);
}

/// <summary>The fields a staff member edits on a material.</summary>
public interface IMaterialFields
{
    string Slug { get; }

    IReadOnlyDictionary<string, string> Name { get; }

    MaterialUnit Unit { get; }

    int PriceBudget { get; }

    int PriceStandard { get; }

    int PricePremium { get; }

    int SortOrder { get; }

    /// <summary>Hidden materials aren't counted in estimates.</summary>
    bool IsActive { get; }
}

public sealed record CreateMaterial(
    string Slug,
    IReadOnlyDictionary<string, string> Name,
    MaterialUnit Unit,
    int PriceBudget,
    int PriceStandard,
    int PricePremium,
    int SortOrder,
    bool IsActive = true) : ICommand<AdminMaterialDto>, IMaterialFields;

public sealed record UpdateMaterial(
    Guid Id,
    string Slug,
    IReadOnlyDictionary<string, string> Name,
    MaterialUnit Unit,
    int PriceBudget,
    int PriceStandard,
    int PricePremium,
    int SortOrder,
    bool IsActive = true) : ICommand<AdminMaterialDto>, IMaterialFields;

/// <summary>Deletes a material no work item uses; 422 "material.in_use" otherwise (hide it instead).</summary>
public sealed record DeleteMaterial(Guid Id) : ICommand<bool>;

/// <summary>The materials a work item uses, in order.</summary>
public sealed record GetWorkItemMaterials(Guid WorkItemId) : IQuery<IReadOnlyList<WorkItemMaterialInput>>;

/// <summary>A material a work item uses: material units per unit of work, plus waste in percent.</summary>
public sealed record WorkItemMaterialInput(Guid MaterialId, decimal QuantityPerUnit, int WastePercent = 0);

/// <summary>Replaces the materials a work item uses with <paramref name="Items"/> (in this order).</summary>
public sealed record SetWorkItemMaterials(Guid WorkItemId, IReadOnlyList<WorkItemMaterialInput> Items)
    : ICommand<IReadOnlyList<WorkItemMaterialInput>>;

public abstract class MaterialFieldsValidator<T> : AbstractValidator<T>
    where T : IMaterialFields
{
    protected MaterialFieldsValidator(ILanguageCatalog languages)
    {
        RuleFor(x => x.Slug).ValidSlug();
        RuleFor(x => x.Name).LocalizedName(languages);
        RuleFor(x => x.Unit).IsInEnum().WithErrorCode("unit.invalid");
        RuleFor(x => x.SortOrder).ValidSortOrder();
        RuleFor(x => x)
            .Must(x => Material.ArePricesValid(x.PriceBudget, x.PriceStandard, x.PricePremium))
            .OverridePropertyName("price")
            .WithErrorCode("price.invalid");
    }
}

public sealed class CreateMaterialValidator(ILanguageCatalog languages) : MaterialFieldsValidator<CreateMaterial>(languages);

public sealed class UpdateMaterialValidator(ILanguageCatalog languages) : MaterialFieldsValidator<UpdateMaterial>(languages);

public sealed class SetWorkItemMaterialsValidator : AbstractValidator<SetWorkItemMaterials>
{
    /// <summary>More than any work needs; stops oversized requests.</summary>
    public const int MaxItems = 30;

    public SetWorkItemMaterialsValidator()
    {
        RuleFor(x => x.Items).NotNull().WithErrorCode("items.required")
            .Must(items => items is null || items.Count <= MaxItems).WithErrorCode("items.too_many")
            .Must(items => items is null || items.Select(i => i.MaterialId).Distinct().Count() == items.Count).WithErrorCode("items.duplicate");
        RuleForEach(x => x.Items)
            .Must(item => WorkItemMaterial.IsValid(item.QuantityPerUnit, item.WastePercent))
            .WithErrorCode("work_item_material.invalid");
    }
}

public sealed class GetAdminMaterialsHandler(IAppDbContext db) : IQueryHandler<GetAdminMaterials, IReadOnlyList<AdminMaterialDto>>
{
    public async Task<IReadOnlyList<AdminMaterialDto>> HandleAsync(GetAdminMaterials query, CancellationToken cancellationToken)
    {
        var materials = await db.Materials.AsNoTracking().OrderBy(m => m.SortOrder).ThenBy(m => m.Slug).ToListAsync(cancellationToken);
        var counts = await MaterialData.WorkItemCountsAsync(db, cancellationToken);
        return materials.Select(m => AdminMaterialDto.From(m, counts.GetValueOrDefault(m.Id))).ToList();
    }
}

public sealed class CreateMaterialHandler(IAppDbContext db) : ICommandHandler<CreateMaterial, AdminMaterialDto>
{
    public async Task<AdminMaterialDto> HandleAsync(CreateMaterial command, CancellationToken cancellationToken)
    {
        await MaterialData.EnsureSlugFreeAsync(db, command.Slug, exceptId: null, cancellationToken);
        var material = Material.Create(
            command.Slug, LocalizedText.From(command.Name), command.Unit, command.PriceBudget, command.PriceStandard, command.PricePremium, command.SortOrder);
        if (!command.IsActive)
        {
            material.Deactivate();
        }

        db.Materials.Add(material);
        await db.SaveChangesAsync(cancellationToken);
        return AdminMaterialDto.From(material);
    }
}

public sealed class UpdateMaterialHandler(IAppDbContext db) : ICommandHandler<UpdateMaterial, AdminMaterialDto>
{
    public async Task<AdminMaterialDto> HandleAsync(UpdateMaterial command, CancellationToken cancellationToken)
    {
        var material = await MaterialData.LoadAsync(db, command.Id, cancellationToken);
        await MaterialData.EnsureSlugFreeAsync(db, command.Slug, material.Id, cancellationToken);
        material.Update(
            command.Slug, LocalizedText.From(command.Name), command.Unit, command.PriceBudget, command.PriceStandard, command.PricePremium, command.SortOrder);
        if (command.IsActive)
        {
            material.Activate();
        }
        else
        {
            material.Deactivate();
        }

        await db.SaveChangesAsync(cancellationToken);
        return AdminMaterialDto.From(material, await db.WorkItemMaterials.CountAsync(m => m.MaterialId == material.Id, cancellationToken));
    }
}

public sealed class DeleteMaterialHandler(IAppDbContext db) : ICommandHandler<DeleteMaterial, bool>
{
    public async Task<bool> HandleAsync(DeleteMaterial command, CancellationToken cancellationToken)
    {
        var material = await MaterialData.LoadAsync(db, command.Id, cancellationToken);
        if (await db.WorkItemMaterials.AnyAsync(m => m.MaterialId == material.Id, cancellationToken))
        {
            throw new DomainException("material.in_use", "Work items use this material. Hide it instead.");
        }

        db.Materials.Remove(material);
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }
}

public sealed class GetWorkItemMaterialsHandler(IAppDbContext db) : IQueryHandler<GetWorkItemMaterials, IReadOnlyList<WorkItemMaterialInput>>
{
    public async Task<IReadOnlyList<WorkItemMaterialInput>> HandleAsync(GetWorkItemMaterials query, CancellationToken cancellationToken)
    {
        if (!await db.WorkItems.AnyAsync(w => w.Id == query.WorkItemId, cancellationToken))
        {
            throw new NotFoundException("The work item does not exist.", "work_item.not_found");
        }

        var links = await db.WorkItemMaterials.AsNoTracking()
            .Where(m => m.WorkItemId == query.WorkItemId)
            .OrderBy(m => m.SortOrder)
            .ToListAsync(cancellationToken);
        return MaterialData.Inputs(links);
    }
}

public sealed class SetWorkItemMaterialsHandler(IAppDbContext db) : ICommandHandler<SetWorkItemMaterials, IReadOnlyList<WorkItemMaterialInput>>
{
    public async Task<IReadOnlyList<WorkItemMaterialInput>> HandleAsync(SetWorkItemMaterials command, CancellationToken cancellationToken)
    {
        if (!await db.WorkItems.AnyAsync(w => w.Id == command.WorkItemId, cancellationToken))
        {
            throw new NotFoundException("The work item does not exist.", "work_item.not_found");
        }

        var ids = command.Items.Select(i => i.MaterialId).ToList();
        if (await db.Materials.CountAsync(m => ids.Contains(m.Id), cancellationToken) != ids.Count)
        {
            throw new DomainException("work_item_material.material_not_found", "Some of the materials no longer exist.");
        }

        var created = command.Items
            .Select((item, index) => WorkItemMaterial.Create(command.WorkItemId, item.MaterialId, item.QuantityPerUnit, item.WastePercent, index + 1))
            .ToList();
        db.WorkItemMaterials.RemoveRange(await db.WorkItemMaterials.Where(m => m.WorkItemId == command.WorkItemId).ToListAsync(cancellationToken));
        db.WorkItemMaterials.AddRange(created);
        await db.SaveChangesAsync(cancellationToken);
        return MaterialData.Inputs(created);
    }
}

internal static class MaterialData
{
    public static async Task<Material> LoadAsync(IAppDbContext db, Guid id, CancellationToken cancellationToken) =>
        await db.Materials.SingleOrDefaultAsync(m => m.Id == id, cancellationToken)
        ?? throw new NotFoundException("The material does not exist.", "material.not_found");

    public static async Task EnsureSlugFreeAsync(IAppDbContext db, string slug, Guid? exceptId, CancellationToken cancellationToken)
    {
        var normalized = slug.Trim().ToLowerInvariant();
        if (await db.Materials.AnyAsync(m => m.Slug == normalized && m.Id != exceptId, cancellationToken))
        {
            throw new ConflictException($"Another material already uses '{normalized}'.", "material.slug_taken");
        }
    }

    /// <summary>How many work items use each material.</summary>
    public static Task<Dictionary<Guid, int>> WorkItemCountsAsync(IAppDbContext db, CancellationToken cancellationToken) =>
        db.WorkItemMaterials.AsNoTracking()
            .GroupBy(m => m.MaterialId)
            .Select(g => new { MaterialId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(g => g.MaterialId, g => g.Count, cancellationToken);

    public static List<WorkItemMaterialInput> Inputs(IEnumerable<WorkItemMaterial> links) =>
        links.OrderBy(m => m.SortOrder).Select(m => new WorkItemMaterialInput(m.MaterialId, m.QuantityPerUnit, m.WastePercent)).ToList();
}
