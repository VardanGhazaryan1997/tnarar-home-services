using HomeServices.Domain.Identity;
using HomeServices.Domain.Offers;
using HomeServices.Domain.Orders;
using HomeServices.Domain.Partners;
using HomeServices.Domain.Requests;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HomeServices.Infrastructure.Persistence.Configurations;

internal sealed class OfferConfiguration : IEntityTypeConfiguration<Offer>
{
    public void Configure(EntityTypeBuilder<Offer> builder)
    {
        builder.ToTable("Offers");
        builder.Property(o => o.Kind).HasConversion<string>().HasMaxLength(16);
        builder.Property(o => o.Status).HasConversion<string>().HasMaxLength(16);
        builder.Property(o => o.Summary).HasMaxLength(Offer.SummaryMaxLength);
        builder.Property(o => o.MaterialsNote).HasMaxLength(Offer.MaterialsNoteMaxLength);
        builder.Property(o => o.RejectReason).HasMaxLength(Offer.ReasonMaxLength);

        // PostgreSQL's xmin: two people deciding on the same offer at once → the second save fails (409).
        builder.Property<uint>("Version").IsRowVersion().HasColumnName("xmin").HasColumnType("xid");

        // The offers on a request (comparison), and a partner's offers newest first.
        builder.HasIndex(o => new { o.RequestId, o.Status });
        builder.HasIndex(o => new { o.PartnerProfileId, o.CreatedAt });

        builder.HasOne<ServiceRequest>().WithMany().HasForeignKey(o => o.RequestId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<PartnerProfile>().WithMany().HasForeignKey(o => o.PartnerProfileId).OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(o => o.Items).WithOne().HasForeignKey(i => i.OfferId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(o => o.Stages).WithOne().HasForeignKey(s => s.OfferId).OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class OfferItemConfiguration : IEntityTypeConfiguration<OfferItem>
{
    public void Configure(EntityTypeBuilder<OfferItem> builder)
    {
        builder.ToTable("OfferItems");
        builder.Property(i => i.Title).HasMaxLength(Offer.LineMaxLength);
    }
}

internal sealed class OfferPaymentStageConfiguration : IEntityTypeConfiguration<OfferPaymentStage>
{
    public void Configure(EntityTypeBuilder<OfferPaymentStage> builder)
    {
        builder.ToTable("OfferPaymentStages");
        builder.Property(s => s.Title).HasMaxLength(Offer.StageTitleMaxLength);
        builder.Property(s => s.Purpose).HasConversion<string>().HasMaxLength(16);
    }
}

internal sealed class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("Orders");
        builder.Property(o => o.Kind).HasConversion<string>().HasMaxLength(16);
        builder.Property(o => o.Status).HasConversion<string>().HasMaxLength(32);
        builder.Property(o => o.Terms).HasColumnType("jsonb");
        builder.Property(o => o.CancelledBy).HasConversion<string>().HasMaxLength(16);
        builder.Property(o => o.CancelReason).HasMaxLength(Order.ReasonMaxLength);
        builder.Ignore(o => o.IsOpen);
        builder.Ignore(o => o.PendingChange);
        builder.Property<uint>("Version").IsRowVersion().HasColumnName("xmin").HasColumnType("xid");

        // One order per accepted offer.
        builder.HasIndex(o => o.OfferId).IsUnique();
        // "My orders" for customers and for partners, newest first.
        builder.HasIndex(o => new { o.CustomerId, o.CreatedAt });
        builder.HasIndex(o => new { o.PartnerProfileId, o.CreatedAt });
        builder.HasIndex(o => o.RequestId);
        // Back Office: orders by status, and the ones that need attention.
        builder.HasIndex(o => new { o.Status, o.CreatedAt });
        builder.HasIndex(o => o.NeedsAttentionSince).HasFilter("needs_attention_since IS NOT NULL");
        // The auto-complete job looks for orders waiting for the customer.
        builder.HasIndex(o => o.AutoCompleteAt).HasFilter("status = 'CompletionRequested'");

        builder.HasOne<ServiceRequest>().WithMany().HasForeignKey(o => o.RequestId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Offer>().WithMany().HasForeignKey(o => o.OfferId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<User>().WithMany().HasForeignKey(o => o.CustomerId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<PartnerProfile>().WithMany().HasForeignKey(o => o.PartnerProfileId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Order>().WithMany().HasForeignKey(o => o.ParentOrderId).OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(o => o.Stages).WithOne().HasForeignKey(s => s.OrderId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(o => o.StatusChanges).WithOne().HasForeignKey(c => c.OrderId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(o => o.ChangeRequests).WithOne().HasForeignKey(c => c.OrderId).OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class OrderChangeRequestConfiguration : IEntityTypeConfiguration<OrderChangeRequest>
{
    public void Configure(EntityTypeBuilder<OrderChangeRequest> builder)
    {
        builder.ToTable("OrderChangeRequests");
        builder.Property(c => c.Kind).HasConversion<string>().HasMaxLength(16);
        builder.Property(c => c.Status).HasConversion<string>().HasMaxLength(16);
        builder.Property(c => c.ProposedBy).HasConversion<string>().HasMaxLength(16);
        builder.Property(c => c.Title).HasMaxLength(Offer.StageTitleMaxLength);
        builder.Property(c => c.Description).HasMaxLength(Order.ReasonMaxLength);
        builder.Property(c => c.ResponseNote).HasMaxLength(Order.ReasonMaxLength);
        builder.HasIndex(c => new { c.OrderId, c.ProposedAt });
        // At most one waiting change per order.
        builder.HasIndex(c => c.OrderId).IsUnique().HasFilter("status = 'Pending'").HasDatabaseName("ix_order_change_requests_one_pending");
    }
}

internal sealed class OrderStageConfiguration : IEntityTypeConfiguration<OrderStage>
{
    public void Configure(EntityTypeBuilder<OrderStage> builder)
    {
        builder.ToTable("OrderStages");
        builder.Property(s => s.Title).HasMaxLength(Offer.StageTitleMaxLength);
        builder.Property(s => s.Purpose).HasConversion<string>().HasMaxLength(16);
    }
}

internal sealed class OrderStatusChangeConfiguration : IEntityTypeConfiguration<OrderStatusChange>
{
    public void Configure(EntityTypeBuilder<OrderStatusChange> builder)
    {
        builder.ToTable("OrderStatusChanges");
        builder.Property(c => c.Status).HasConversion<string>().HasMaxLength(32);
        builder.Property(c => c.ChangedBy).HasConversion<string>().HasMaxLength(16);
        builder.Property(c => c.Note).HasMaxLength(Order.ReasonMaxLength);
        builder.HasIndex(c => new { c.OrderId, c.Sequence }).IsUnique();
    }
}
