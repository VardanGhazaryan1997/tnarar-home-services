using HomeServices.Domain.Common;

namespace HomeServices.Domain.Catalog;

/// <summary>
/// How much of a material one unit of a work item uses, e.g. wall putty: 1.2 kg of finishing putty per m², plus
/// <see cref="WastePercent"/> for offcuts and spills.
/// </summary>
public sealed class WorkItemMaterial : Entity
{
    public const decimal MaxQuantityPerUnit = 1000m;
    public const int MaxWastePercent = 100;

    private WorkItemMaterial()
    {
    }

    public Guid WorkItemId { get; private set; }

    public Guid MaterialId { get; private set; }

    /// <summary>Material units per unit of work.</summary>
    public decimal QuantityPerUnit { get; private set; }

    public int WastePercent { get; private set; }

    public int SortOrder { get; private set; }

    public static WorkItemMaterial Create(Guid workItemId, Guid materialId, decimal quantityPerUnit, int wastePercent, int sortOrder)
    {
        if (!IsValid(quantityPerUnit, wastePercent))
        {
            throw new DomainException(
                "work_item_material.invalid",
                $"An amount is above 0 and up to {MaxQuantityPerUnit} per unit; waste is 0–{MaxWastePercent}%.");
        }

        return new WorkItemMaterial
        {
            WorkItemId = workItemId,
            MaterialId = materialId,
            QuantityPerUnit = quantityPerUnit,
            WastePercent = wastePercent,
            SortOrder = sortOrder,
        };
    }

    public static bool IsValid(decimal quantityPerUnit, int wastePercent) =>
        quantityPerUnit is > 0 and <= MaxQuantityPerUnit && wastePercent is >= 0 and <= MaxWastePercent;

    /// <summary>The material needed for <paramref name="workQuantity"/> units of work, waste included.</summary>
    public decimal AmountFor(decimal workQuantity) => workQuantity * QuantityPerUnit * (100 + WastePercent) / 100m;
}
