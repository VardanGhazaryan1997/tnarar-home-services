using System.Linq.Expressions;
using HomeServices.Application.Abstractions;
using HomeServices.Domain.Auditing;
using HomeServices.Domain.Catalog;
using HomeServices.Domain.Chat;
using HomeServices.Domain.Commissions;
using HomeServices.Domain.Common;
using HomeServices.Domain.Content;
using HomeServices.Domain.Files;
using HomeServices.Domain.Identity;
using HomeServices.Domain.Localization;
using HomeServices.Domain.Notifications;
using HomeServices.Domain.Partners;
using HomeServices.Domain.Payments;
using HomeServices.Domain.Offers;
using HomeServices.Domain.Orders;
using HomeServices.Domain.Requests;
using HomeServices.Domain.Reviews;
using HomeServices.Domain.Staff;
using HomeServices.Infrastructure.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;

namespace HomeServices.Infrastructure.Persistence;

public class AppDbContext : DbContext, IAppDbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    /// <summary>For derived contexts (e.g. tests that add their own entities).</summary>
    protected AppDbContext(DbContextOptions options)
        : base(options)
    {
    }

    public DbSet<Language> Languages => Set<Language>();

    public DbSet<UiTranslation> UiTranslations => Set<UiTranslation>();

    public DbSet<Category> Categories => Set<Category>();

    public DbSet<City> Cities => Set<City>();

    public DbSet<District> Districts => Set<District>();

    public DbSet<User> Users => Set<User>();

    public DbSet<OtpCode> OtpCodes => Set<OtpCode>();

    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    public DbSet<StaffUser> StaffUsers => Set<StaffUser>();

    public DbSet<StaffRefreshToken> StaffRefreshTokens => Set<StaffRefreshToken>();

    public DbSet<Role> Roles => Set<Role>();

    public DbSet<AuditLogEntry> AuditLog => Set<AuditLogEntry>();

    public DbSet<StoredFile> Files => Set<StoredFile>();

    public DbSet<PartnerProfile> PartnerProfiles => Set<PartnerProfile>();

    public DbSet<StaticPage> StaticPages => Set<StaticPage>();

    public DbSet<FaqItem> FaqItems => Set<FaqItem>();

    public DbSet<ServiceRequest> ServiceRequests => Set<ServiceRequest>();

    public DbSet<Offer> Offers => Set<Offer>();

    public DbSet<Order> Orders => Set<Order>();

    public DbSet<Conversation> Conversations => Set<Conversation>();

    public DbSet<Message> Messages => Set<Message>();

    public DbSet<Payment> Payments => Set<Payment>();

    public DbSet<Review> Reviews => Set<Review>();

    public DbSet<Notification> Notifications => Set<Notification>();

    public DbSet<CommissionRate> CommissionRates => Set<CommissionRate>();

    public DbSet<CommissionObligation> CommissionObligations => Set<CommissionObligation>();

    public DbSet<CommissionStatement> CommissionStatements => Set<CommissionStatement>();

    public DbSet<Settlement> Settlements => Set<Settlement>();

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        // Every LocalizedText property becomes a jsonb column.
        configurationBuilder.Properties<LocalizedText>()
            .HaveConversion<LocalizedTextConverter, LocalizedTextComparer>()
            .HaveColumnType("jsonb");

        // An audit entry's changed properties: one jsonb column, not a table.
        configurationBuilder.Properties<IReadOnlyList<AuditPropertyChange>>()
            .HaveConversion<AuditChangesConverter, AuditChangesComparer>()
            .HaveColumnType("jsonb");

        configurationBuilder.Properties<PhoneNumber>()
            .HaveConversion<PhoneNumberConverter>()
            .HaveMaxLength(16);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        ApplyConventions(modelBuilder);
    }

    /// <summary>Conventions every entity gets: client-side ids, no stored domain events, soft-delete filter, snake_case names.</summary>
    private static void ApplyConventions(ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes().ToList())
        {
            var clrType = entityType.ClrType;
            var entity = modelBuilder.Entity(clrType);

            if (typeof(Entity).IsAssignableFrom(clrType))
            {
                entity.Ignore(nameof(Entity.DomainEvents));
                entity.Property(nameof(Entity.Id)).ValueGeneratedNever();
            }

            if (typeof(ISoftDeletable).IsAssignableFrom(clrType))
            {
                var parameter = Expression.Parameter(clrType, "e");
                var notDeleted = Expression.Not(Expression.Property(parameter, nameof(ISoftDeletable.IsDeleted)));
                entity.HasQueryFilter(Expression.Lambda(notDeleted, parameter));
            }

            entityType.SetTableName(SnakeCase.Convert(entityType.GetTableName()!));

            foreach (var property in entityType.GetProperties())
            {
                property.SetColumnName(SnakeCase.Convert(property.GetColumnName()));
            }

            foreach (var key in entityType.GetKeys())
            {
                key.SetName(SnakeCase.Convert(key.GetName()!));
            }

            foreach (var foreignKey in entityType.GetForeignKeys())
            {
                foreignKey.SetConstraintName(SnakeCase.Convert(foreignKey.GetConstraintName()!));
            }

            foreach (var index in entityType.GetIndexes())
            {
                index.SetDatabaseName(SnakeCase.Convert(index.GetDatabaseName()!));
            }
        }
    }
}
