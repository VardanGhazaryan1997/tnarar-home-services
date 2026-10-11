using HomeServices.Domain.Catalog;
using HomeServices.Domain.Common;
using HomeServices.Domain.Files;
using HomeServices.Domain.Stores;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HomeServices.Infrastructure.Persistence.Configurations;

internal sealed class StoreConfiguration : IEntityTypeConfiguration<Store>
{
    public void Configure(EntityTypeBuilder<Store> builder)
    {
        builder.Property(s => s.Id).ValueGeneratedNever();
        builder.Property(s => s.Slug).HasMaxLength(Slug.MaxLength);
        builder.Property(s => s.Name).HasMaxLength(Store.MaxNameLength);
        builder.Property(s => s.Website).HasMaxLength(Store.MaxWebsiteLength);
        builder.Property(s => s.Phone).HasMaxLength(Store.MaxPhoneLength);
        builder.HasIndex(s => s.Slug).IsUnique();
        builder.HasIndex(s => new { s.IsActive, s.SortOrder });
        builder.HasOne<StoredFile>().WithMany().HasForeignKey(s => s.LogoFileId).OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(s => s.Branches)
            .WithOne()
            .HasForeignKey(b => b.StoreId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(s => s.Branches).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

internal sealed class StoreBranchConfiguration : IEntityTypeConfiguration<StoreBranch>
{
    public void Configure(EntityTypeBuilder<StoreBranch> builder)
    {
        builder.Property(b => b.Id).ValueGeneratedNever();
        builder.Property(b => b.Name).HasMaxLength(StoreBranch.MaxNameLength);
        builder.Property(b => b.Address).HasMaxLength(StoreBranch.MaxAddressLength);
        builder.Property(b => b.Phone).HasMaxLength(StoreBranch.MaxPhoneLength);
        builder.Property(b => b.OpeningHours).HasMaxLength(StoreBranch.MaxOpeningHoursLength);
        builder.HasIndex(b => new { b.StoreId, b.SortOrder });
        builder.HasIndex(b => b.CityId);
        builder.HasOne<City>().WithMany().HasForeignKey(b => b.CityId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<District>().WithMany().HasForeignKey(b => b.DistrictId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class StoreProductConfiguration : IEntityTypeConfiguration<StoreProduct>
{
    public void Configure(EntityTypeBuilder<StoreProduct> builder)
    {
        builder.Property(p => p.Id).ValueGeneratedNever();
        builder.Property(p => p.Grade).HasConversion<string>().HasMaxLength(16);
        builder.Property(p => p.Name).HasMaxLength(StoreProduct.MaxNameLength);
        builder.Property(p => p.Brand).HasMaxLength(StoreProduct.MaxBrandLength);
        builder.Property(p => p.Code).HasMaxLength(StoreProduct.MaxCodeLength);
        builder.Property(p => p.PackLabel).HasMaxLength(StoreProduct.MaxPackLabelLength);
        builder.Property(p => p.PackSize).HasPrecision(10, 3);

        // A store's product codes are unique; products without a code don't count.
        builder.HasIndex(p => new { p.StoreId, p.Code }).IsUnique().HasFilter("code IS NOT NULL");
        builder.HasIndex(p => p.MaterialId);

        builder.HasOne<Store>().WithMany().HasForeignKey(p => p.StoreId).OnDelete(DeleteBehavior.Cascade);

        // A material that products link to can't be deleted (staff hide it instead).
        builder.HasOne<Material>().WithMany().HasForeignKey(p => p.MaterialId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<StoredFile>().WithMany().HasForeignKey(p => p.PhotoFileId).OnDelete(DeleteBehavior.Restrict);

        builder.ToTable(t =>
        {
            t.HasCheckConstraint("ck_store_products_price", "price >= 1 AND pack_size > 0");
            t.HasCheckConstraint("ck_store_products_sale", "(sale_price IS NULL AND sale_ends_on IS NULL) OR (sale_price >= 1 AND sale_price < price AND sale_ends_on IS NOT NULL)");
            t.HasCheckConstraint("ck_store_products_grade", "material_id IS NULL OR grade IS NOT NULL");
        });
    }
}
