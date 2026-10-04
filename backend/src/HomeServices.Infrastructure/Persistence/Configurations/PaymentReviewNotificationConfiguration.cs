using HomeServices.Domain.Identity;
using HomeServices.Domain.Notifications;
using HomeServices.Domain.Orders;
using HomeServices.Domain.Partners;
using HomeServices.Domain.Payments;
using HomeServices.Domain.Reviews;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HomeServices.Infrastructure.Persistence.Configurations;

internal sealed class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.ToTable("Payments");
        builder.Property(p => p.Method).HasConversion<string>().HasMaxLength(16);
        builder.Property(p => p.Status).HasConversion<string>().HasMaxLength(16);
        builder.Property(p => p.RecordedBy).HasConversion<string>().HasMaxLength(16);
        builder.Property(p => p.Note).HasMaxLength(Payment.NoteMaxLength);
        builder.Property(p => p.DisputeReason).HasMaxLength(Payment.ReasonMaxLength);
        builder.Property(p => p.ResolutionNote).HasMaxLength(Payment.ReasonMaxLength);
        builder.Ignore(p => p.Counts);

        // Both sides answering the same record at once → the second save fails (409).
        builder.Property<uint>("Version").IsRowVersion().HasColumnName("xmin").HasColumnType("xid");

        // The payments of an order; the Back Office's dispute queue.
        builder.HasIndex(p => new { p.OrderId, p.RecordedAt });
        builder.HasIndex(p => new { p.Status, p.AnsweredAt });

        builder.HasOne<Order>().WithMany().HasForeignKey(p => p.OrderId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<OrderStage>().WithMany().HasForeignKey(p => p.StageId).OnDelete(DeleteBehavior.SetNull);
        builder.HasOne<User>().WithMany().HasForeignKey(p => p.RecordedByUserId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class ReviewConfiguration : IEntityTypeConfiguration<Review>
{
    public void Configure(EntityTypeBuilder<Review> builder)
    {
        builder.ToTable("Reviews");
        builder.Property(r => r.Text).HasMaxLength(Review.TextMaxLength);
        builder.Property(r => r.Reply).HasMaxLength(Review.TextMaxLength);
        builder.Property(r => r.HiddenReason).HasMaxLength(Review.ReasonMaxLength);

        // One review per order.
        builder.HasIndex(r => r.OrderId).IsUnique();
        // A partner's public reviews, newest first (and their average).
        builder.HasIndex(r => new { r.PartnerProfileId, r.IsHidden, r.SubmittedAt });
        builder.HasIndex(r => r.SubmittedAt);

        builder.HasOne<Order>().WithMany().HasForeignKey(r => r.OrderId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<PartnerProfile>().WithMany().HasForeignKey(r => r.PartnerProfileId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<User>().WithMany().HasForeignKey(r => r.CustomerId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.ToTable("Notifications");
        builder.Property(n => n.Type).HasConversion<string>().HasMaxLength(32);
        builder.Property(n => n.Link).HasMaxLength(Notification.LinkMaxLength);
        builder.Property(n => n.Params).HasColumnType("jsonb");

        // A user's list, newest first; the unread badge; the delivery job's queue.
        builder.HasIndex(n => new { n.UserId, n.CreatedAt });
        builder.HasIndex(n => n.UserId).HasFilter("read_at IS NULL").HasDatabaseName("ix_notifications_unread");
        builder.HasIndex(n => n.CreatedAt).HasFilter("delivered_at IS NULL").HasDatabaseName("ix_notifications_undelivered");

        builder.HasOne<User>().WithMany().HasForeignKey(n => n.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}
