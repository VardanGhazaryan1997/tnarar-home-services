using HomeServices.Domain.Catalog;
using HomeServices.Domain.Files;
using HomeServices.Domain.Identity;
using HomeServices.Domain.Partners;
using HomeServices.Domain.Requests;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HomeServices.Infrastructure.Persistence.Configurations;

internal sealed class ServiceRequestConfiguration : IEntityTypeConfiguration<ServiceRequest>
{
    public void Configure(EntityTypeBuilder<ServiceRequest> builder)
    {
        builder.ToTable("ServiceRequests");
        builder.Property(r => r.Kind).HasConversion<string>().HasMaxLength(16);
        builder.Property(r => r.Status).HasConversion<string>().HasMaxLength(16);
        builder.Property(r => r.AttentionReason).HasConversion<string>().HasMaxLength(32);
        builder.Property(r => r.Description).HasMaxLength(ServiceRequest.DescriptionMaxLength);
        builder.Property(r => r.TimeNote).HasMaxLength(ServiceRequest.TimeNoteMaxLength);
        builder.Property(r => r.CancelReason).HasMaxLength(ServiceRequest.CancelReasonMaxLength);
        builder.Ignore(r => r.NeedsAttention);

        // "My requests", newest first.
        builder.HasIndex(r => new { r.CustomerId, r.CreatedAt });
        // The operator queue, and the follow-up job's scan of open requests.
        builder.HasIndex(r => new { r.Status, r.NeedsAttentionSince });

        builder.HasOne<User>().WithMany().HasForeignKey(r => r.CustomerId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Category>().WithMany().HasForeignKey(r => r.CategoryId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<City>().WithMany().HasForeignKey(r => r.CityId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<District>().WithMany().HasForeignKey(r => r.DistrictId).OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(r => r.Recipients).WithOne().HasForeignKey(x => x.RequestId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(r => r.Media).WithOne().HasForeignKey(m => m.RequestId).OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class RequestRecipientConfiguration : IEntityTypeConfiguration<RequestRecipient>
{
    public void Configure(EntityTypeBuilder<RequestRecipient> builder)
    {
        builder.ToTable("RequestRecipients");
        builder.Property(x => x.Source).HasConversion<string>().HasMaxLength(16);
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(16);
        builder.Property(x => x.DeclineReason).HasMaxLength(RequestRecipient.DeclineReasonMaxLength);

        // A partner receives a request once.
        builder.HasIndex(x => new { x.RequestId, x.PartnerProfileId }).IsUnique();
        // A partner's inbox, newest first.
        builder.HasIndex(x => new { x.PartnerProfileId, x.SentAt });
        builder.HasOne<PartnerProfile>().WithMany().HasForeignKey(x => x.PartnerProfileId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class RequestMediaConfiguration : IEntityTypeConfiguration<RequestMedia>
{
    public void Configure(EntityTypeBuilder<RequestMedia> builder)
    {
        builder.ToTable("RequestMedia");
        builder.HasIndex(m => new { m.RequestId, m.FileId }).IsUnique();
        builder.HasOne<StoredFile>().WithMany().HasForeignKey(m => m.FileId).OnDelete(DeleteBehavior.Restrict);
    }
}
