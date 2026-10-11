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

        // Prices that existed before confirmations count as confirmed when the column was added.
        builder.Property(p => p.ConfirmedAt).HasDefaultValueSql("now()");

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

internal sealed class RegionalMarketPriceConfiguration : IEntityTypeConfiguration<RegionalMarketPrice>
{
    public void Configure(EntityTypeBuilder<RegionalMarketPrice> builder)
    {
        builder.Property(p => p.Id).ValueGeneratedNever();
        builder.Ignore(p => p.Range);
        builder.HasIndex(p => new { p.WorkItemId, p.RegionId }).IsUnique();
        builder.HasIndex(p => p.RegionId);
        builder.HasOne<WorkItem>().WithMany().HasForeignKey(p => p.WorkItemId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<Region>().WithMany().HasForeignKey(p => p.RegionId).OnDelete(DeleteBehavior.Cascade);
    }
}
