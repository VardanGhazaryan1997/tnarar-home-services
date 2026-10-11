using HomeServices.Domain.Catalog;
using HomeServices.Domain.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HomeServices.Infrastructure.Persistence.Configurations;

internal sealed class MaterialConfiguration : IEntityTypeConfiguration<Material>
{
    private static readonly DateTimeOffset SeededAt = new(2026, 10, 11, 0, 0, 0, TimeSpan.Zero);

    public void Configure(EntityTypeBuilder<Material> builder)
    {
        builder.Property(m => m.Id).ValueGeneratedNever();
        builder.Property(m => m.Slug).HasMaxLength(Slug.MaxLength);
        builder.Property(m => m.Unit).HasConversion<string>().HasMaxLength(16);
        builder.HasIndex(m => m.Slug).IsUnique();
        builder.Property(m => m.PricesCheckedAt).HasDefaultValueSql("now()");
        builder.ToTable(t => t.HasCheckConstraint(
            "ck_materials_prices",
            "price_budget >= 1 AND price_budget <= price_standard AND price_standard <= price_premium"));

        // Starter materials with Yerevan prices.
        builder.HasData(CatalogSeed.Materials.Select((m, index) => new
        {
            m.Id,
            m.Slug,
            m.Name,
            m.Unit,
            m.PriceBudget,
            m.PriceStandard,
            m.PricePremium,
            SortOrder = index + 1,
            IsActive = true,
            PricesCheckedAt = SeededAt,
            IsPriceLocked = false,
            MarketStoreCount = 0,
        }));
    }
}

internal sealed class WorkItemMaterialConfiguration : IEntityTypeConfiguration<WorkItemMaterial>
{
    public void Configure(EntityTypeBuilder<WorkItemMaterial> builder)
    {
        builder.Property(m => m.Id).ValueGeneratedNever();
        builder.Property(m => m.QuantityPerUnit).HasPrecision(9, 3);
        builder.HasIndex(m => new { m.WorkItemId, m.MaterialId }).IsUnique();
        builder.HasIndex(m => m.MaterialId);
        builder.HasOne<WorkItem>().WithMany().HasForeignKey(m => m.WorkItemId).OnDelete(DeleteBehavior.Cascade);

        // A material in use can't be deleted (staff hide it instead).
        builder.HasOne<Material>().WithMany().HasForeignKey(m => m.MaterialId).OnDelete(DeleteBehavior.Restrict);

        var workItemIds = CatalogSeed.WorkItems.ToDictionary(w => w.Slug, w => w.Id, StringComparer.Ordinal);
        var materialIds = CatalogSeed.Materials.ToDictionary(m => m.Slug, m => m.Id, StringComparer.Ordinal);
        builder.HasData(CatalogSeed.WorkItemMaterials
            .GroupBy(m => m.WorkItemSlug)
            .SelectMany(group => group.Select((m, index) => new
            {
                m.Id,
                WorkItemId = workItemIds[m.WorkItemSlug],
                MaterialId = materialIds[m.MaterialSlug],
                m.QuantityPerUnit,
                m.WastePercent,
                SortOrder = index + 1,
            })));
    }
}
