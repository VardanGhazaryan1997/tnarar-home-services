using HomeServices.Domain.Files;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HomeServices.Infrastructure.Persistence.Configurations;

internal sealed class StoredFileConfiguration : IEntityTypeConfiguration<StoredFile>
{
    public void Configure(EntityTypeBuilder<StoredFile> builder)
    {
        builder.Property(f => f.OwnerType).HasConversion<string>().HasMaxLength(16);
        builder.Property(f => f.OwnerId).HasMaxLength(StoredFile.OwnerIdMaxLength);
        builder.Property(f => f.Kind).HasConversion<string>().HasMaxLength(16);
        builder.Property(f => f.Status).HasConversion<string>().HasMaxLength(16);
        builder.Property(f => f.OriginalFileName).HasMaxLength(StoredFile.FileNameMaxLength);
        builder.Property(f => f.ContentType).HasMaxLength(StoredFile.ContentTypeMaxLength);
        builder.Property(f => f.Key).HasMaxLength(StoredFile.KeyMaxLength);
        builder.Property(f => f.ThumbnailKey).HasMaxLength(StoredFile.KeyMaxLength);
        builder.Property(f => f.RejectionCode).HasMaxLength(StoredFile.RejectionCodeMaxLength);

        builder.HasIndex(f => f.Key).IsUnique();

        // "Uploads by this person in the last hour" and, later, cleanup of abandoned uploads.
        builder.HasIndex(f => new { f.OwnerType, f.OwnerId, f.CreatedAt });
        builder.HasIndex(f => new { f.Status, f.CreatedAt });
    }
}
