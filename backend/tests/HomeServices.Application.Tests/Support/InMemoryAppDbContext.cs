using System.Text.Json;
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
using HomeServices.Domain.Offers;
using HomeServices.Domain.Orders;
using HomeServices.Domain.Partners;
using HomeServices.Domain.Payments;
using HomeServices.Domain.Requests;
using HomeServices.Domain.Reviews;
using HomeServices.Domain.Staff;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace HomeServices.Application.Tests.Support;

/// <summary>
/// Fast in-memory stand-in for the real database, for handler unit tests.
/// Behaviour that depends on PostgreSQL is covered by the Infrastructure integration tests.
/// </summary>
public sealed class InMemoryAppDbContext(DbContextOptions<InMemoryAppDbContext> options) : DbContext(options), IAppDbContext
{
    public DbSet<Language> Languages => Set<Language>();

    public DbSet<UiTranslation> UiTranslations => Set<UiTranslation>();

    public DbSet<Category> Categories => Set<Category>();

    public DbSet<WorkItem> WorkItems => Set<WorkItem>();

    public DbSet<PartnerPrice> PartnerPrices => Set<PartnerPrice>();

    public DbSet<Region> Regions => Set<Region>();

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

    public static InMemoryAppDbContext Create() =>
        new(new DbContextOptionsBuilder<InMemoryAppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Properties<LocalizedText>().HaveConversion<JsonLocalizedTextConverter, LocalizedTextComparer>();
        configurationBuilder.Properties<PhoneNumber>().HaveConversion<PhoneNumberConverter>();
        configurationBuilder.Properties<IReadOnlyList<AuditPropertyChange>>().HaveConversion<AuditChangesJsonConverter, AuditChangesComparer>();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        foreach (var type in new[] { typeof(Language), typeof(Category), typeof(WorkItem), typeof(PartnerPrice), typeof(Region), typeof(City), typeof(District), typeof(User), typeof(OtpCode), typeof(RefreshToken), typeof(StaffUser), typeof(StaffRefreshToken), typeof(Role), typeof(AuditLogEntry), typeof(StoredFile), typeof(PartnerProfile), typeof(PartnerArea), typeof(PartnerMedia), typeof(PartnerStatusChange), typeof(UiTranslation), typeof(StaticPage), typeof(FaqItem), typeof(ServiceRequest), typeof(RequestRecipient), typeof(RequestMedia), typeof(Offer), typeof(OfferItem), typeof(OfferPaymentStage), typeof(Order), typeof(OrderStage), typeof(OrderStatusChange), typeof(OrderChangeRequest), typeof(Conversation), typeof(Message), typeof(MessageAttachment), typeof(Payment), typeof(Review), typeof(Notification), typeof(CommissionRate), typeof(CommissionObligation), typeof(CommissionStatement), typeof(Settlement) })
        {
            modelBuilder.Entity(type).Ignore(nameof(Entity.DomainEvents));
        }

        // Same shape as the real configuration (Infrastructure/Persistence/Configurations/StaffConfiguration.cs).
        modelBuilder.Entity<Role>().Ignore(r => r.Permissions).Property<List<string>>("_permissions");
        modelBuilder.Entity<StaffUserRole>().HasKey(r => new { r.StaffUserId, r.RoleId });
        modelBuilder.Entity<StaffUser>().HasMany(s => s.Roles).WithOne().HasForeignKey(r => r.StaffUserId);
        modelBuilder.Entity<StaffUser>().Navigation(s => s.Roles).AutoInclude();
        modelBuilder.Entity<StaffUser>().Ignore(s => s.RoleIds);

        // Same shape as Infrastructure/Persistence/Configurations/PartnerConfiguration.cs. Child ids are set in code,
        // so EF must treat new children as added, not as existing rows to update.
        modelBuilder.Entity<PartnerService>().HasKey(s => new { s.PartnerProfileId, s.CategoryId });
        modelBuilder.Entity<PartnerArea>().Property(a => a.Id).ValueGeneratedNever();
        modelBuilder.Entity<PartnerMedia>().Property(m => m.Id).ValueGeneratedNever();
        modelBuilder.Entity<PartnerStatusChange>().Property(c => c.Id).ValueGeneratedNever();
        modelBuilder.Entity<PartnerProfile>().HasMany(p => p.Services).WithOne().HasForeignKey(s => s.PartnerProfileId);
        modelBuilder.Entity<PartnerProfile>().HasMany(p => p.Areas).WithOne().HasForeignKey(a => a.PartnerProfileId);
        modelBuilder.Entity<PartnerProfile>().HasMany(p => p.Media).WithOne().HasForeignKey(m => m.PartnerProfileId);
        modelBuilder.Entity<PartnerProfile>().HasMany(p => p.StatusChanges).WithOne().HasForeignKey(c => c.PartnerProfileId);
        modelBuilder.Entity<PartnerProfile>().Ignore(p => p.CanEdit).Ignore(p => p.CanSubmit).Ignore(p => p.ReviewComment);

        // Same shape as Infrastructure/Persistence/Configurations/RequestConfiguration.cs.
        modelBuilder.Entity<RequestRecipient>().Property(r => r.Id).ValueGeneratedNever();
        modelBuilder.Entity<RequestMedia>().Property(m => m.Id).ValueGeneratedNever();
        modelBuilder.Entity<ServiceRequest>().HasMany(r => r.Recipients).WithOne().HasForeignKey(x => x.RequestId);
        modelBuilder.Entity<ServiceRequest>().HasMany(r => r.Media).WithOne().HasForeignKey(m => m.RequestId);
        modelBuilder.Entity<ServiceRequest>().Ignore(r => r.NeedsAttention);

        // Same shape as Infrastructure/Persistence/Configurations/OfferConfiguration.cs and OrderConfiguration.cs.
        modelBuilder.Entity<OfferItem>().Property(i => i.Id).ValueGeneratedNever();
        modelBuilder.Entity<OfferPaymentStage>().Property(s => s.Id).ValueGeneratedNever();
        modelBuilder.Entity<Offer>().HasMany(o => o.Items).WithOne().HasForeignKey(i => i.OfferId);
        modelBuilder.Entity<Offer>().HasMany(o => o.Stages).WithOne().HasForeignKey(s => s.OfferId);
        modelBuilder.Entity<OrderStage>().Property(s => s.Id).ValueGeneratedNever();
        modelBuilder.Entity<OrderStatusChange>().Property(c => c.Id).ValueGeneratedNever();
        modelBuilder.Entity<Order>().HasMany(o => o.Stages).WithOne().HasForeignKey(s => s.OrderId);
        modelBuilder.Entity<Order>().HasMany(o => o.StatusChanges).WithOne().HasForeignKey(c => c.OrderId);
        modelBuilder.Entity<OrderChangeRequest>().Property(c => c.Id).ValueGeneratedNever();
        modelBuilder.Entity<Order>().HasMany(o => o.ChangeRequests).WithOne().HasForeignKey(c => c.OrderId);
        modelBuilder.Entity<Order>().Ignore(o => o.IsOpen).Ignore(o => o.PendingChange);

        // Same shape as Infrastructure/Persistence/Configurations/ChatConfiguration.cs.
        modelBuilder.Entity<MessageAttachment>().Property(a => a.Id).ValueGeneratedNever();
        modelBuilder.Entity<Message>().HasMany(m => m.Attachments).WithOne().HasForeignKey(a => a.MessageId);

        // Same shape as Infrastructure/Persistence/Configurations/PaymentReviewNotificationConfiguration.cs.
        modelBuilder.Entity<Payment>().Ignore(p => p.Counts);
        modelBuilder.Entity<CommissionStatement>().Ignore(s => s.Outstanding);
    }

    private sealed class JsonLocalizedTextConverter() : ValueConverter<LocalizedText, string>(
        text => JsonSerializer.Serialize(text.Values, (JsonSerializerOptions?)null),
        json => LocalizedText.From(JsonSerializer.Deserialize<Dictionary<string, string>>(json, (JsonSerializerOptions?)null)!));

    private sealed class PhoneNumberConverter() : ValueConverter<PhoneNumber, string>(
        phone => phone.Value,
        value => PhoneNumber.Parse(value));

    private sealed class AuditChangesJsonConverter() : ValueConverter<IReadOnlyList<AuditPropertyChange>, string>(
        changes => JsonSerializer.Serialize(changes, (JsonSerializerOptions?)null),
        json => JsonSerializer.Deserialize<List<AuditPropertyChange>>(json, (JsonSerializerOptions?)null)!);

    private sealed class AuditChangesComparer() : ValueComparer<IReadOnlyList<AuditPropertyChange>>(
        (a, b) => a!.SequenceEqual(b!),
        changes => changes.Count,
        changes => changes.ToList());

    private sealed class LocalizedTextComparer() : ValueComparer<LocalizedText>(
        (a, b) => object.Equals(a, b),
        text => text.GetHashCode(),
        text => text);
}
