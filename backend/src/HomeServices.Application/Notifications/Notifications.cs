using FluentValidation;
using HomeServices.Application.Abstractions;
using HomeServices.Application.Common;
using HomeServices.Application.Errors;
using HomeServices.Application.Identity;
using HomeServices.Application.Messaging;
using HomeServices.Application.Partners;
using HomeServices.Domain.Notifications;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace HomeServices.Application.Notifications;

/// <summary>The signed-in user's notifications, newest first.</summary>
public sealed record GetMyNotifications(bool UnreadOnly = false, int Page = 1, int PageSize = GetMyNotifications.DefaultPageSize)
    : IQuery<PagedResult<NotificationDto>>
{
    public const int DefaultPageSize = 20;
    public const int MaxPageSize = 100;
}

/// <summary>How many of the signed-in user's notifications are unread (the bell's badge).</summary>
public sealed record GetUnreadNotificationCount : IQuery<int>;

/// <summary>Marks one notification as read.</summary>
public sealed record MarkNotificationRead(Guid Id) : ICommand<NotificationDto>;

/// <summary>Marks every notification of the signed-in user as read. Returns how many changed.</summary>
public sealed record MarkAllNotificationsRead : ICommand<int>;

/// <summary>Sends notifications not delivered yet: an SMS for the important types, a live update for all (the delivery job). Returns how many.</summary>
public sealed record DeliverNotifications : ICommand<int>;

public sealed class GetMyNotificationsValidator : AbstractValidator<GetMyNotifications>
{
    public GetMyNotificationsValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1).WithErrorCode("page.invalid");
        RuleFor(x => x.PageSize).InclusiveBetween(1, GetMyNotifications.MaxPageSize).WithErrorCode("page_size.invalid");
    }
}

internal static class NotificationViews
{
    public static NotificationDto ToDto(Notification n) =>
        new(n.Id, n.Type.ToString(), n.Link, Notifier.Read(n.Params), n.CreatedAt, n.ReadAt);
}

public sealed class GetMyNotificationsHandler(IAppDbContext db, ICurrentUser currentUser)
    : IQueryHandler<GetMyNotifications, PagedResult<NotificationDto>>
{
    public async Task<PagedResult<NotificationDto>> HandleAsync(GetMyNotifications query, CancellationToken cancellationToken)
    {
        var userId = MyPartnerProfile.UserId(currentUser);
        var mine = db.Notifications.AsNoTracking().Where(n => n.UserId == userId);
        if (query.UnreadOnly)
        {
            mine = mine.Where(n => n.ReadAt == null);
        }

        var total = await mine.CountAsync(cancellationToken);
        var page = await mine
            .OrderByDescending(n => n.CreatedAt).ThenByDescending(n => n.Id)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);
        return new PagedResult<NotificationDto>(page.Select(NotificationViews.ToDto).ToList(), query.Page, query.PageSize, total);
    }
}

public sealed class GetUnreadNotificationCountHandler(IAppDbContext db, ICurrentUser currentUser) : IQueryHandler<GetUnreadNotificationCount, int>
{
    public Task<int> HandleAsync(GetUnreadNotificationCount query, CancellationToken cancellationToken)
    {
        var userId = MyPartnerProfile.UserId(currentUser);
        return db.Notifications.CountAsync(n => n.UserId == userId && n.ReadAt == null, cancellationToken);
    }
}

public sealed class MarkNotificationReadHandler(IAppDbContext db, ICurrentUser currentUser, TimeProvider clock)
    : ICommandHandler<MarkNotificationRead, NotificationDto>
{
    public async Task<NotificationDto> HandleAsync(MarkNotificationRead command, CancellationToken cancellationToken)
    {
        var userId = MyPartnerProfile.UserId(currentUser);
        var notification = await db.Notifications.SingleOrDefaultAsync(n => n.Id == command.Id && n.UserId == userId, cancellationToken)
            ?? throw new NotFoundException("Notification not found.", "notification.not_found");
        notification.MarkRead(clock.GetUtcNow());
        await db.SaveChangesAsync(cancellationToken);
        return NotificationViews.ToDto(notification);
    }
}

public sealed class MarkAllNotificationsReadHandler(IAppDbContext db, ICurrentUser currentUser, TimeProvider clock)
    : ICommandHandler<MarkAllNotificationsRead, int>
{
    public async Task<int> HandleAsync(MarkAllNotificationsRead command, CancellationToken cancellationToken)
    {
        var userId = MyPartnerProfile.UserId(currentUser);
        var unread = await db.Notifications.Where(n => n.UserId == userId && n.ReadAt == null).ToListAsync(cancellationToken);
        var now = clock.GetUtcNow();
        foreach (var notification in unread)
        {
            notification.MarkRead(now);
        }

        if (unread.Count > 0)
        {
            await db.SaveChangesAsync(cancellationToken);
        }

        return unread.Count;
    }
}

public sealed class DeliverNotificationsHandler(
    IAppDbContext db, ISmsSender sms, INotificationPusher pusher, IOptions<NotificationSettings> settings, TimeProvider clock)
    : ICommandHandler<DeliverNotifications, int>
{
    public async Task<int> HandleAsync(DeliverNotifications command, CancellationToken cancellationToken)
    {
        var due = await db.Notifications
            .Where(n => n.DeliveredAt == null)
            .OrderBy(n => n.CreatedAt).ThenBy(n => n.Id)
            .Take(settings.Value.BatchSize)
            .ToListAsync(cancellationToken);
        if (due.Count == 0)
        {
            return 0;
        }

        var userIds = due.Where(n => n.SendSms).Select(n => n.UserId).Distinct().ToList();
        var phones = await db.Users.AsNoTracking().Where(u => userIds.Contains(u.Id)).ToDictionaryAsync(u => u.Id, u => u.Phone, cancellationToken);

        foreach (var notification in due)
        {
            // Marked first and saved per notification: a failure later in the batch never sends the same SMS twice.
            notification.MarkDelivered(clock.GetUtcNow());
            await db.SaveChangesAsync(cancellationToken);
            if (notification.SendSms && phones.TryGetValue(notification.UserId, out var phone))
            {
                await sms.SendAsync(phone, SmsTexts.For(notification, settings.Value.PortalUrl), cancellationToken);
            }

            await pusher.PushAsync(notification.UserId, NotificationViews.ToDto(notification), cancellationToken);
        }

        return due.Count;
    }
}

/// <summary>SMS wording, in Armenian (users have no language setting yet), with a link to the page.</summary>
internal static class SmsTexts
{
    public static string For(Notification notification, string portalUrl)
    {
        var text = notification.Type switch
        {
            NotificationType.RequestReceived => "Նոր հայտ է ստացվել ձեզ համար։",
            NotificationType.OfferReceived => "Ձեր հայտին նոր առաջարկ է եկել։",
            NotificationType.OfferAccepted => "Ձեր առաջարկն ընդունվել է․ նոր պատվեր։",
            NotificationType.CompletionRequested => "Պատվերը նշվել է որպես ավարտված․ խնդրում ենք հաստատել։",
            NotificationType.OrderCancelled => "Պատվերը չեղարկվել է։",
            NotificationType.PartnerApproved => "Ձեր գործընկերոջ էջը հաստատվել է։",
            NotificationType.PartnerNeedsChanges => "Ձեր գործընկերոջ էջում փոփոխություններ են անհրաժեշտ։",
            NotificationType.PartnerRejected => "Ձեր գործընկերոջ էջը մերժվել է։",
            NotificationType.CommissionStatementIssued => "Ձեր շաբաթական միջնորդավճարի հաշիվը պատրաստ է։",
            NotificationType.CommissionOverdue => "Միջնորդավճարի վճարման ժամկետն անցել է։",
            NotificationType.PartnerPausedForDebt => "Նոր հայտերը դադարեցված են մինչև միջնորդավճարի վճարումը։",
            _ => "Նոր ծանուցում։",
        };
        return $"Tnarar: {text} {portalUrl.TrimEnd('/')}/hy{notification.Link}";
    }
}
