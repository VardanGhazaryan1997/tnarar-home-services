using HomeServices.Domain.Catalog;
using HomeServices.Domain.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HomeServices.Infrastructure.Persistence.Configurations;

internal sealed class WorkItemConfiguration : IEntityTypeConfiguration<WorkItem>
{
    private static readonly DateTimeOffset SeededAt = new(2026, 10, 6, 0, 0, 0, TimeSpan.Zero);

    public void Configure(EntityTypeBuilder<WorkItem> builder)
    {
        builder.Property(w => w.Slug).HasMaxLength(Slug.MaxLength);
        builder.Property(w => w.Unit).HasConversion<string>().HasMaxLength(16);
        builder.Property(w => w.Surface).HasConversion<string>().HasMaxLength(16);

        // Unique among work items that aren't deleted, so a deleted item's slug can be reused.
        builder.HasIndex(w => w.Slug).IsUnique().HasFilter("is_deleted = false");
        builder.HasIndex(w => new { w.CategoryId, w.SortOrder });

        builder.HasOne<Category>()
            .WithMany()
            .HasForeignKey(w => w.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Ignore(w => w.Price);

        // Starter work items with Yerevan labour prices, numbered within their subcategory.
        var subcategoryIds = CatalogSeed.Subcategories.ToDictionary(c => c.Slug, c => c.Id, StringComparer.Ordinal);
        builder.HasData(CatalogSeed.WorkItems
            .GroupBy(w => w.CategorySlug)
            .SelectMany(group => group.Select((w, index) => new
            {
                w.Id,
                CategoryId = subcategoryIds[w.CategorySlug],
                w.Slug,
                w.Name,
                w.Unit,
                w.Surface,
                SortOrder = index + 1,
                IsActive = true,
                PriceMin = (int?)w.PriceMin,
                PriceTypical = (int?)w.PriceTypical,
                PriceMax = (int?)w.PriceMax,
                IsPriceLocked = false,
                IsDeleted = false,
                CreatedAt = SeededAt,
            })));
    }
}
