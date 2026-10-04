using HomeServices.Domain.Catalog;
using HomeServices.Domain.Commissions;
using HomeServices.Domain.Orders;
using HomeServices.Domain.Partners;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HomeServices.Infrastructure.Persistence.Configurations;

internal sealed class CommissionRateConfiguration : IEntityTypeConfiguration<CommissionRate>
{
    public void Configure(EntityTypeBuilder<CommissionRate> builder)
    {
        builder.ToTable("CommissionRates");
        builder.Property(r => r.Percent).HasPrecision(5, 2);

        // One rate per category; the row without a category is the default (seeded at 10%).
        builder.HasIndex(r => r.CategoryId).IsUnique();
        builder.HasOne<Category>().WithMany().HasForeignKey(r => r.CategoryId).OnDelete(DeleteBehavior.Cascade);
        builder.HasData(new { Id = CommissionRate.DefaultId, CategoryId = (Guid?)null, Percent = 10m });
    }
}

internal sealed class CommissionObligationConfiguration : IEntityTypeConfiguration<CommissionObligation>
{
    public void Configure(EntityTypeBuilder<CommissionObligation> builder)
    {
        builder.ToTable("CommissionObligations");
        builder.Property(o => o.RatePercent).HasPrecision(5, 2);

        // One charge per order; a partner's charges not on a statement yet; a statement's lines.
        builder.HasIndex(o => o.OrderId).IsUnique();
        builder.HasIndex(o => new { o.PartnerProfileId, o.StatementId });
        builder.HasIndex(o => o.StatementId);

        builder.HasOne<Order>().WithMany().HasForeignKey(o => o.OrderId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<PartnerProfile>().WithMany().HasForeignKey(o => o.PartnerProfileId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Category>().WithMany().HasForeignKey(o => o.CategoryId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<CommissionStatement>().WithMany().HasForeignKey(o => o.StatementId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class CommissionStatementConfiguration : IEntityTypeConfiguration<CommissionStatement>
{
    public void Configure(EntityTypeBuilder<CommissionStatement> builder)
    {
        builder.ToTable("CommissionStatements");
        builder.Property(s => s.Status).HasConversion<string>().HasMaxLength(16);
        builder.Ignore(s => s.Outstanding);

        // Settlements and the job update the same row: the second save fails (409).
        builder.Property<uint>("Version").IsRowVersion().HasColumnName("xmin").HasColumnType("xid");

        // One statement per partner and week; open statements by due date (overdue, pauses).
        builder.HasIndex(s => new { s.PartnerProfileId, s.PeriodStart }).IsUnique();
        builder.HasIndex(s => new { s.Status, s.DueOn });

        builder.HasOne<PartnerProfile>().WithMany().HasForeignKey(s => s.PartnerProfileId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class SettlementConfiguration : IEntityTypeConfiguration<Settlement>
{
    public void Configure(EntityTypeBuilder<Settlement> builder)
    {
        builder.ToTable("Settlements");
        builder.Property(s => s.Method).HasConversion<string>().HasMaxLength(16);
        builder.Property(s => s.Reference).HasMaxLength(Settlement.ReferenceMaxLength);
        builder.HasIndex(s => s.StatementId);
        builder.HasIndex(s => s.PartnerProfileId);

        builder.HasOne<CommissionStatement>().WithMany().HasForeignKey(s => s.StatementId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<PartnerProfile>().WithMany().HasForeignKey(s => s.PartnerProfileId).OnDelete(DeleteBehavior.Restrict);
    }
}
