using HomeServices.Domain.Catalog;
using HomeServices.Domain.Estimates;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HomeServices.Infrastructure.Persistence.Configurations;

internal sealed class RoomTemplateConfiguration : IEntityTypeConfiguration<RoomTemplate>
{
    public void Configure(EntityTypeBuilder<RoomTemplate> builder)
    {
        builder.Property(t => t.RoomType).HasConversion<string>().HasMaxLength(16);
        builder.Property(t => t.Quantity).HasPrecision(7, 2);
        builder.Property(t => t.QuantityPerSquareMeter).HasPrecision(6, 3);
        builder.HasIndex(t => new { t.RoomType, t.WorkItemId }).IsUnique();
        builder.HasOne<WorkItem>().WithMany().HasForeignKey(t => t.WorkItemId).OnDelete(DeleteBehavior.Cascade);

        var workItemIds = CatalogSeed.WorkItems.ToDictionary(w => w.Slug, w => w.Id, StringComparer.Ordinal);
        builder.HasData(CatalogSeed.RoomTemplates.SelectMany(template => template.Items.Select((item, index) => new
        {
            Id = CatalogSeed.StableId($"room-template/{template.Type}/{item.Slug}"),
            RoomType = template.Type,
            WorkItemId = workItemIds[item.Slug],
            SortOrder = index + 1,
            item.Quantity,
            QuantityPerSquareMeter = item.PerSquareMeter,
        })));
    }
}
