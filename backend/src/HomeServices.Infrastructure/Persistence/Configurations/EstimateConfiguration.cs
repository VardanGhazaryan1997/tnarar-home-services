using HomeServices.Domain.Catalog;
using HomeServices.Domain.Estimates;
using HomeServices.Domain.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HomeServices.Infrastructure.Persistence.Configurations;

internal sealed class EstimateConfiguration : IEntityTypeConfiguration<Estimate>
{
    public void Configure(EntityTypeBuilder<Estimate> builder)
    {
        builder.Property(e => e.Title).HasMaxLength(Estimate.TitleMaxLength);
        builder.HasIndex(e => e.UserId);
        builder.Property(e => e.ShareToken).HasMaxLength(Estimate.ShareTokenMaxLength);
        builder.HasIndex(e => e.ShareToken).IsUnique();
        builder.HasOne<User>().WithMany().HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<City>().WithMany().HasForeignKey(e => e.CityId).OnDelete(DeleteBehavior.SetNull);
        builder.HasMany(e => e.Rooms).WithOne().HasForeignKey(r => r.EstimateId).OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(e => e.Rooms).HasField("_rooms");
    }
}

internal sealed class EstimateRoomConfiguration : IEntityTypeConfiguration<EstimateRoom>
{
    public void Configure(EntityTypeBuilder<EstimateRoom> builder)
    {
        builder.Property(r => r.Id).ValueGeneratedNever();
        builder.Property(r => r.Name).HasMaxLength(EstimateRoom.NameMaxLength);
        builder.Property(r => r.Type).HasConversion<string>().HasMaxLength(16);
        builder.Property(r => r.Length).HasPrecision(7, 2);
        builder.Property(r => r.Width).HasPrecision(7, 2);
        builder.Property(r => r.Area).HasPrecision(8, 2);
        builder.Property(r => r.Height).HasPrecision(5, 2);
        builder.Ignore(r => r.Size);
        builder.Ignore(r => r.Geometry);
        builder.HasMany(r => r.Openings).WithOne().HasForeignKey(o => o.RoomId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(r => r.Lines).WithOne().HasForeignKey(l => l.RoomId).OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(r => r.Openings).HasField("_openings");
        builder.Navigation(r => r.Lines).HasField("_lines");
    }
}

internal sealed class EstimateOpeningConfiguration : IEntityTypeConfiguration<EstimateOpening>
{
    public void Configure(EntityTypeBuilder<EstimateOpening> builder)
    {
        builder.Property(o => o.Id).ValueGeneratedNever();
        builder.Property(o => o.Kind).HasConversion<string>().HasMaxLength(16);
        builder.Property(o => o.Width).HasPrecision(5, 2);
        builder.Property(o => o.Height).HasPrecision(5, 2);
    }
}

internal sealed class EstimateLineConfiguration : IEntityTypeConfiguration<EstimateLine>
{
    public void Configure(EntityTypeBuilder<EstimateLine> builder)
    {
        builder.Property(l => l.Id).ValueGeneratedNever();
        builder.Property(l => l.Quantity).HasPrecision(10, 2);
        builder.HasIndex(l => new { l.RoomId, l.WorkItemId }).IsUnique();
        builder.HasOne<WorkItem>().WithMany().HasForeignKey(l => l.WorkItemId).OnDelete(DeleteBehavior.Restrict);
    }
}
