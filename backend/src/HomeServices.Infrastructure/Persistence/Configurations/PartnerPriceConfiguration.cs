using HomeServices.Domain.Catalog;
using HomeServices.Domain.Partners;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HomeServices.Infrastructure.Persistence.Configurations;

internal sealed class PartnerPriceConfiguration : IEntityTypeConfiguration<PartnerPrice>
{
    public void Configure(EntityTypeBuilder<PartnerPrice> builder)
    {
        // One price per partner and work item.
        builder.HasIndex(p => new { p.PartnerProfileId, p.WorkItemId }).IsUnique();

        // Market ranges and partner counts are worked out per work item.
        builder.HasIndex(p => p.WorkItemId);

        builder.HasOne<PartnerProfile>()
            .WithMany()
            .HasForeignKey(p => p.PartnerProfileId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<WorkItem>()
            .WithMany()
            .HasForeignKey(p => p.WorkItemId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.ToTable(t => t.HasCheckConstraint(
            "ck_partner_prices_range",
            "price_from >= 1 AND (price_to IS NULL OR price_to >= price_from)"));
    }
}
