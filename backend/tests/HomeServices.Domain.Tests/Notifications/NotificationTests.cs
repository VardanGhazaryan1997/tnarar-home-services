using HomeServices.Domain.Notifications;

namespace HomeServices.Domain.Tests.Notifications;

public class NotificationTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 5, 9, 0, 0, TimeSpan.Zero);

    [Fact]
    public void A_notification_keeps_the_first_read_and_delivery_times()
    {
        var notification = Notification.Create(Guid.NewGuid(), NotificationType.OfferReceived, "/requests/1", "{\"name\":\"Aram\"}", true, Now);

        notification.ReadAt.ShouldBeNull();
        notification.MarkRead(Now.AddMinutes(1));
        notification.MarkRead(Now.AddMinutes(2));
        notification.ReadAt.ShouldBe(Now.AddMinutes(1));

        notification.MarkDelivered(Now.AddSeconds(5));
        notification.MarkDelivered(Now.AddSeconds(9));
        notification.DeliveredAt.ShouldBe(Now.AddSeconds(5));
        notification.SendSms.ShouldBeTrue();
    }

    [Fact]
    public void A_notification_needs_a_user_and_a_link()
    {
        Should.Throw<DomainException>(() => Notification.Create(Guid.Empty, NotificationType.OrderStarted, "/orders/1", "{}", false, Now)).Code.ShouldBe("notification.user_required");
        Should.Throw<DomainException>(() => Notification.Create(Guid.NewGuid(), NotificationType.OrderStarted, " ", "{}", false, Now)).Code.ShouldBe("notification.link_invalid");
    }
}
