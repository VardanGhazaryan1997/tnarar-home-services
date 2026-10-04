using HomeServices.Domain.Catalog;
using HomeServices.Domain.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HomeServices.Infrastructure.Persistence.Configurations;

internal sealed class CityConfiguration : IEntityTypeConfiguration<City>
{
    public void Configure(EntityTypeBuilder<City> builder)
    {
        builder.Property(c => c.Slug).HasMaxLength(Slug.MaxLength);
        builder.HasIndex(c => c.Slug).IsUnique();

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
