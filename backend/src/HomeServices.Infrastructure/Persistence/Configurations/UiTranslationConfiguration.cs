using HomeServices.Domain.Localization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HomeServices.Infrastructure.Persistence.Configurations;

internal sealed class UiTranslationConfiguration : IEntityTypeConfiguration<UiTranslation>
{
    public void Configure(EntityTypeBuilder<UiTranslation> builder)
    {
        builder.Property(t => t.Namespace).HasMaxLength(UiTranslation.NamespaceMaxLength);
        builder.Property(t => t.Key).HasMaxLength(UiTranslation.KeyMaxLength);
        builder.Property(t => t.LanguageCode).HasMaxLength(16);
        builder.Property(t => t.Value).HasMaxLength(UiTranslation.ValueMaxLength);

        // One text per key and language; also serves "all texts of a namespace".
        builder.HasIndex(t => new { t.Namespace, t.Key, t.LanguageCode }).IsUnique();
        builder.HasIndex(t => new { t.Namespace, t.LanguageCode });

        // A text belongs to an existing language (deleting a language with texts is refused).
        builder.HasOne<Language>().WithMany().HasPrincipalKey(l => l.Code).HasForeignKey(t => t.LanguageCode).OnDelete(DeleteBehavior.Restrict);
    }
}
