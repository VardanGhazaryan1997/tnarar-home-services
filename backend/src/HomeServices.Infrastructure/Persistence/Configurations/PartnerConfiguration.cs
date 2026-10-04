using HomeServices.Domain.Catalog;
using HomeServices.Domain.Files;
using HomeServices.Domain.Identity;
using HomeServices.Domain.Partners;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HomeServices.Infrastructure.Persistence.Configurations;

internal sealed class PartnerProfileConfiguration : IEntityTypeConfiguration<PartnerProfile>
{
    public void Configure(EntityTypeBuilder<PartnerProfile> builder)
    {
        builder.Property(p => p.Type).HasConversion<string>().HasMaxLength(16);
        builder.Property(p => p.Status).HasConversion<string>().HasMaxLength(16);
        builder.Property(p => p.DisplayName).HasMaxLength(PartnerProfile.DisplayNameMaxLength);
        builder.Property(p => p.About).HasMaxLength(PartnerProfile.AboutMaxLength);

        builder.Property(p => p.Slug).HasMaxLength(PartnerProfile.SlugMaxLength);
        builder.HasIndex(p => p.Slug).IsUnique();

        // One profile per user.
        builder.HasIndex(p => p.UserId).IsUnique();
        // Public search: approved partners, newest approval first.
        builder.HasIndex(p => new { p.Status, p.ApprovedAt });
        builder.HasOne<User>().WithMany().HasForeignKey(p => p.UserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<StoredFile>().WithMany().HasForeignKey(p => p.AvatarFileId).OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(p => p.Services).WithOne().HasForeignKey(s => s.PartnerProfileId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(p => p.Areas).WithOne().HasForeignKey(a => a.PartnerProfileId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(p => p.Media).WithOne().HasForeignKey(m => m.PartnerProfileId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(p => p.StatusChanges).WithOne().HasForeignKey(c => c.PartnerProfileId).OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class PartnerServiceConfiguration : IEntityTypeConfiguration<PartnerService>
{
    public void Configure(EntityTypeBuilder<PartnerService> builder)
    {
        builder.ToTable("PartnerServices");
        builder.HasKey(s => new { s.PartnerProfileId, s.CategoryId });

        // "Partners offering plumbing" for the public list and request matching.
        builder.HasIndex(s => s.CategoryId);
        builder.HasOne<Category>().WithMany().HasForeignKey(s => s.CategoryId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class PartnerAreaConfiguration : IEntityTypeConfiguration<PartnerArea>
{
    public void Configure(EntityTypeBuilder<PartnerArea> builder)
    {
        builder.ToTable("PartnerAreas");

        // A whole city (district null) counts once, like any district.
        builder.HasIndex(a => new { a.PartnerProfileId, a.CityId, a.DistrictId }).IsUnique().AreNullsDistinct(false);
        builder.HasIndex(a => new { a.CityId, a.DistrictId });
        builder.HasOne<City>().WithMany().HasForeignKey(a => a.CityId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<District>().WithMany().HasForeignKey(a => a.DistrictId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class PartnerMediaConfiguration : IEntityTypeConfiguration<PartnerMedia>
{
    public void Configure(EntityTypeBuilder<PartnerMedia> builder)
    {
        builder.ToTable("PartnerMedia");
        builder.Property(m => m.Kind).HasConversion<string>().HasMaxLength(16);
        builder.Property(m => m.Caption).HasMaxLength(PartnerProfile.CaptionMaxLength);
        builder.HasIndex(m => new { m.PartnerProfileId, m.FileId }).IsUnique();
        builder.HasOne<StoredFile>().WithMany().HasForeignKey(m => m.FileId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class PartnerStatusChangeConfiguration : IEntityTypeConfiguration<PartnerStatusChange>
{
    public void Configure(EntityTypeBuilder<PartnerStatusChange> builder)
    {
        builder.ToTable("PartnerStatusChanges");
        builder.Property(c => c.FromStatus).HasConversion<string>().HasMaxLength(16);
        builder.Property(c => c.ToStatus).HasConversion<string>().HasMaxLength(16);
        builder.Property(c => c.Comment).HasMaxLength(PartnerStatusChange.CommentMaxLength);
        builder.HasIndex(c => new { c.PartnerProfileId, c.Sequence }).IsUnique();
    }
}
