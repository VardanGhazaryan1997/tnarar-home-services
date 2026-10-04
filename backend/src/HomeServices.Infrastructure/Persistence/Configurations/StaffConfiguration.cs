using HomeServices.Domain.Staff;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HomeServices.Infrastructure.Persistence.Configurations;

internal sealed class StaffUserConfiguration : IEntityTypeConfiguration<StaffUser>
{
    public void Configure(EntityTypeBuilder<StaffUser> builder)
    {
        builder.Property(s => s.Email).HasMaxLength(StaffUser.EmailMaxLength);
        builder.HasIndex(s => s.Email).IsUnique();
        builder.Property(s => s.FullName).HasMaxLength(StaffUser.FullNameMaxLength);
        builder.Property(s => s.PasswordHash).HasMaxLength(256);
        builder.Property(s => s.TotpSecret).HasMaxLength(64);
        builder.Property(s => s.PendingTotpSecret).HasMaxLength(64);
        builder.Property(s => s.InviteTokenHash).HasMaxLength(128);
        builder.HasIndex(s => s.InviteTokenHash).IsUnique();

        builder.HasMany(s => s.Roles).WithOne().HasForeignKey(r => r.StaffUserId).OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(s => s.Roles).AutoInclude().UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Ignore(s => s.RoleIds);
    }
}

internal sealed class StaffUserRoleConfiguration : IEntityTypeConfiguration<StaffUserRole>
{
    public void Configure(EntityTypeBuilder<StaffUserRole> builder)
    {
        builder.HasKey(r => new { r.StaffUserId, r.RoleId });
        builder.HasOne<Role>().WithMany().HasForeignKey(r => r.RoleId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.Property(r => r.Name).HasMaxLength(Role.NameMaxLength);
        builder.Property(r => r.Description).HasMaxLength(Role.DescriptionMaxLength);
        builder.HasIndex(r => r.Name).IsUnique();

        // Permissions are stored as a PostgreSQL text[] column.
        builder.Ignore(r => r.Permissions);
        builder.Property<List<string>>("_permissions").HasColumnName("permissions");

        // The built-in roles are inserted by the AddRoles migration, not HasData:
        // EF compares List<string> seed values by reference, so HasData here would always
        // report pending model changes.
    }
}

internal sealed class StaffRefreshTokenConfiguration : IEntityTypeConfiguration<StaffRefreshToken>
{
    public void Configure(EntityTypeBuilder<StaffRefreshToken> builder)
    {
        builder.Property(t => t.TokenHash).HasMaxLength(128);
        builder.Property(t => t.ReplacedByHash).HasMaxLength(128);
        builder.HasIndex(t => t.TokenHash).IsUnique();

        builder.HasOne<StaffUser>()
            .WithMany()
            .HasForeignKey(t => t.StaffUserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
