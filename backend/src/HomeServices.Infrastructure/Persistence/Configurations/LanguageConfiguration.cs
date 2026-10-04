using HomeServices.Domain.Localization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HomeServices.Infrastructure.Persistence.Configurations;

internal sealed class LanguageConfiguration : IEntityTypeConfiguration<Language>
{
    // Fixed ids so the seed data is stable across migrations.
    internal static readonly Guid ArmenianId = new("019a0000-0000-7000-8000-000000000001");
    internal static readonly Guid RussianId = new("019a0000-0000-7000-8000-000000000002");
    internal static readonly Guid EnglishId = new("019a0000-0000-7000-8000-000000000003");

    public void Configure(EntityTypeBuilder<Language> builder)
    {
        builder.Property(l => l.Code).HasMaxLength(16);
        builder.Property(l => l.Name).HasMaxLength(64);
        builder.Property(l => l.NativeName).HasMaxLength(64);

        builder.HasIndex(l => l.Code).IsUnique();

        // At most one default language (column name is snake_case: see AppDbContext conventions).
        builder.HasIndex(l => l.IsDefault).IsUnique().HasFilter("is_default = true");

        builder.HasData(
            new { Id = ArmenianId, Code = "hy", Name = "Armenian", NativeName = "Հայերեն", IsActive = true, IsDefault = true, SortOrder = 1 },
            new { Id = RussianId, Code = "ru", Name = "Russian", NativeName = "Русский", IsActive = true, IsDefault = false, SortOrder = 2 },
            new { Id = EnglishId, Code = "en", Name = "English", NativeName = "English", IsActive = true, IsDefault = false, SortOrder = 3 });
    }
}
