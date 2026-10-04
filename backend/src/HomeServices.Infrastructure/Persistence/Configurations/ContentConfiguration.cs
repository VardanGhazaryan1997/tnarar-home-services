using HomeServices.Domain.Common;
using HomeServices.Domain.Content;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HomeServices.Infrastructure.Persistence.Configurations;

internal sealed class StaticPageConfiguration : IEntityTypeConfiguration<StaticPage>
{
    public void Configure(EntityTypeBuilder<StaticPage> builder)
    {
        builder.Property(p => p.Slug).HasMaxLength(Slug.MaxLength);

        // Unique among pages that aren't deleted, so a deleted page's address can be reused.
        builder.HasIndex(p => p.Slug).IsUnique().HasFilter("is_deleted = false");
    }
}

internal sealed class FaqItemConfiguration : IEntityTypeConfiguration<FaqItem>
{
    public void Configure(EntityTypeBuilder<FaqItem> builder)
    {
        builder.Property(f => f.Audience).HasConversion<string>().HasMaxLength(16);
        builder.HasIndex(f => new { f.IsPublished, f.Audience, f.SortOrder });
    }
}
