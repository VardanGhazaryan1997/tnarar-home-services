using System.Text.Json;
using HomeServices.Application.Abstractions;
using HomeServices.Domain.Notifications;
using HomeServices.Domain.Orders;
using Microsoft.EntityFrameworkCore;

namespace HomeServices.Application.Notifications;

/// <summary>
/// Adds notifications to the database context, so they are saved with the change they describe (in the same
/// <c>SaveChangesAsync</c>). The delivery job sends them afterwards.
/// </summary>
internal static class Notifier
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    /// <summary>The types that also go out as an SMS: things a user should act on soon.</summary>
    private static readonly HashSet<NotificationType> SmsTypes =
    [
        NotificationType.RequestReceived,
        NotificationType.OfferReceived,
        NotificationType.OfferAccepted,
        NotificationType.CompletionRequested,
        NotificationType.OrderCancelled,
        NotificationType.PartnerApproved,
        NotificationType.PartnerNeedsChanges,
        NotificationType.PartnerRejected,
        NotificationType.CommissionStatementIssued,
        NotificationType.CommissionOverdue,
        NotificationType.PartnerPausedForDebt,
    ];

    public static bool SendsSms(NotificationType type) => SmsTypes.Contains(type);

    public static string OrderLink(Guid orderId) => $"/orders/{orderId}";

    public static void Add(
        IAppDbContext db, Guid userId, NotificationType type, string link, IReadOnlyDictionary<string, string?>? values, DateTimeOffset now) =>
        db.Notifications.Add(Notification.Create(userId, type, link, Serialize(values), SendsSms(type), now));

    /// <summary>Tells the owners of these partner profiles.</summary>
    public static async Task ToPartnersAsync(
        IAppDbContext db,
        IReadOnlyCollection<Guid> partnerIds,
        NotificationType type,
        Func<Guid, string> link,
        IReadOnlyDictionary<string, string?>? values,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        if (partnerIds.Count == 0)
        {
            return;
        }

        var owners = await db.PartnerProfiles.AsNoTracking()
            .Where(p => partnerIds.Contains(p.Id))
            .Select(p => new { p.Id, p.UserId })
            .ToListAsync(cancellationToken);
        foreach (var owner in owners)
        {
            Add(db, owner.UserId, type, link(owner.Id), values, now);
        }
    }

    /// <summary>
    /// Tells one or both sides of <paramref name="order"/> about something <paramref name="actor"/> did. The values get
    /// <c>name</c> (who acted, when it was the customer or the partner) and link to the order.
    /// </summary>
    public static async Task ToOrderAsync(
        IAppDbContext db,
        Order order,
        OrderParty actor,
        NoticeTo to,
        NotificationType type,
        IReadOnlyDictionary<string, string?>? values,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var customerName = await db.Users.AsNoTracking().Where(u => u.Id == order.CustomerId).Select(u => u.FullName).SingleOrDefaultAsync(cancellationToken);
        var partner = await db.PartnerProfiles.AsNoTracking()
            .Where(p => p.Id == order.PartnerProfileId)
            .Select(p => new { p.UserId, p.DisplayName })
            .SingleAsync(cancellationToken);

        var all = new Dictionary<string, string?>(values ?? new Dictionary<string, string?>())
        {
            ["name"] = actor switch
            {
                OrderParty.Customer => customerName,
                OrderParty.Partner => partner.DisplayName,
                _ => null,
            },
            ["by"] = actor.ToString(),
        };

        var (customer, partnerSide) = to switch
        {
            NoticeTo.Customer => (true, false),
            NoticeTo.Partner => (false, true),
            NoticeTo.Both => (true, true),
            _ => (actor != OrderParty.Customer, actor != OrderParty.Partner),
        };

        if (customer)
        {
            Add(db, order.CustomerId, type, OrderLink(order.Id), all, now);
        }

        if (partnerSide)
        {
            Add(db, partner.UserId, type, OrderLink(order.Id), all, now);
        }
    }

    public static IReadOnlyDictionary<string, string?> Read(string json) =>
        JsonSerializer.Deserialize<Dictionary<string, string?>>(json, Json) ?? new Dictionary<string, string?>();

    private static string Serialize(IReadOnlyDictionary<string, string?>? values) =>
        values is null || values.Count == 0 ? "{}" : JsonSerializer.Serialize(values, Json);
}

/// <summary>What an order action tells whom; see <see cref="Notifier.ToOrderAsync"/>.</summary>
internal sealed record OrderNotice(NotificationType Type, NoticeTo To = NoticeTo.OtherSide, IReadOnlyDictionary<string, string?>? Values = null);
