using HomeServices.Domain.Auditing;
using HomeServices.Domain.Catalog;
using HomeServices.Domain.Chat;
using HomeServices.Domain.Commissions;
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

namespace HomeServices.Application.Abstractions;

/// <summary>
/// The database as use cases see it. Implemented by Infrastructure's AppDbContext;
/// tests can use EF Core's in-memory provider.
/// </summary>
public interface IAppDbContext
{
    DbSet<Language> Languages { get; }

    DbSet<UiTranslation> UiTranslations { get; }

    DbSet<Category> Categories { get; }

    DbSet<Region> Regions { get; }

    DbSet<City> Cities { get; }

    DbSet<District> Districts { get; }

    DbSet<User> Users { get; }

    DbSet<OtpCode> OtpCodes { get; }

    DbSet<RefreshToken> RefreshTokens { get; }

    DbSet<StaffUser> StaffUsers { get; }

    DbSet<StaffRefreshToken> StaffRefreshTokens { get; }

    DbSet<Role> Roles { get; }

    DbSet<AuditLogEntry> AuditLog { get; }

    DbSet<StoredFile> Files { get; }

    DbSet<PartnerProfile> PartnerProfiles { get; }

    DbSet<StaticPage> StaticPages { get; }

    DbSet<FaqItem> FaqItems { get; }

    DbSet<ServiceRequest> ServiceRequests { get; }

    DbSet<Offer> Offers { get; }

    DbSet<Order> Orders { get; }

    DbSet<Conversation> Conversations { get; }

    DbSet<Message> Messages { get; }

    DbSet<Payment> Payments { get; }

    DbSet<Review> Reviews { get; }

    DbSet<Notification> Notifications { get; }

    DbSet<CommissionRate> CommissionRates { get; }

    DbSet<CommissionObligation> CommissionObligations { get; }

    DbSet<CommissionStatement> CommissionStatements { get; }

    DbSet<Settlement> Settlements { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
