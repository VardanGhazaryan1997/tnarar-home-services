using HomeServices.Domain.Catalog;
using HomeServices.Domain.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HomeServices.Infrastructure.Persistence.Configurations;

internal sealed class RegionConfiguration : IEntityTypeConfiguration<Region>
{
    public void Configure(EntityTypeBuilder<Region> builder)
    {
        builder.Property(r => r.Slug).HasMaxLength(Slug.MaxLength);
        builder.HasIndex(r => r.Slug).IsUnique();

        builder.HasData(CatalogSeed.Regions.Select((r, index) => new
        {
            r.Id,
            r.Slug,
            r.Name,
            SortOrder = index + 1,
        }));
    }
}

internal sealed class CityConfiguration : IEntityTypeConfiguration<City>
{
    public void Configure(EntityTypeBuilder<City> builder)
    {
        builder.Property(c => c.Slug).HasMaxLength(Slug.MaxLength);
        builder.HasIndex(c => c.Slug).IsUnique();
        builder.Property(c => c.Kind).HasConversion<string>().HasMaxLength(16);
        builder.HasIndex(c => c.RegionId);
        builder.HasOne<Region>().WithMany().HasForeignKey(c => c.RegionId).OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(c => c.Districts)
            .WithOne()
            .HasForeignKey(d => d.CityId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(c => c.Districts).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasData(CatalogSeed.Cities.Select((c, index) => new
        {
            c.Id,
            c.Slug,
            c.Name,
            SortOrder = index + 1,
            IsActive = true,
            RegionId = (Guid?)c.RegionId,
            c.Kind,
        }));
    }
}

internal sealed class DistrictConfiguration : IEntityTypeConfiguration<District>
{
    public void Configure(EntityTypeBuilder<District> builder)
    {
        builder.Property(d => d.Slug).HasMaxLength(Slug.MaxLength);
        builder.HasIndex(d => new { d.CityId, d.Slug }).IsUnique();

        builder.HasData(CatalogSeed.YerevanDistricts.Select((d, index) => new
        {
            d.Id,
            CityId = CatalogSeed.YerevanId,
            d.Slug,
            d.Name,
            SortOrder = index + 1,
            IsActive = true,
        }));
    }
}
