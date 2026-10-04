using HomeServices.Domain.Catalog;
using HomeServices.Domain.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HomeServices.Infrastructure.Persistence.Configurations;

internal sealed class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    private static readonly DateTimeOffset SeededAt = new(2026, 10, 1, 0, 0, 0, TimeSpan.Zero);

    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.Property(c => c.Slug).HasMaxLength(Slug.MaxLength);
        builder.Property(c => c.Icon).HasMaxLength(Category.IconMaxLength);

        // Unique among categories that aren't deleted, so a deleted category's slug can be reused.
        builder.HasIndex(c => c.Slug).IsUnique().HasFilter("is_deleted = false");

        builder.HasOne<Category>()
            .WithMany()
            .HasForeignKey(c => c.ParentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasData(CatalogSeed.Categories.Select((c, index) => new
        {
            c.Id,
            c.Slug,
            c.Icon,
            c.Name,
            SortOrder = index + 1,
            IsActive = true,
            IsDeleted = false,
            CreatedAt = SeededAt,
        }));
    }
}
