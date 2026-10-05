using HomeServices.Domain.Catalog;
using HomeServices.Domain.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HomeServices.Infrastructure.Persistence.Configurations;

internal sealed class WorkItemConfiguration : IEntityTypeConfiguration<WorkItem>
{
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
    }
}
