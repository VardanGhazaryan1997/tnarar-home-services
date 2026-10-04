using HomeServices.Application.Abstractions;
using HomeServices.Application.Common;
using HomeServices.Application.Notifications;
using HomeServices.Application.Partners;
using HomeServices.Application.Tests.Offers;
using HomeServices.Application.Tests.Support;
using HomeServices.Domain.Notifications;
using HomeServices.Domain.Partners;
using Microsoft.Extensions.Options;

namespace HomeServices.Application.Tests.Notifications;

public sealed class FakeNotificationPusher : INotificationPusher
{
    public List<(Guid UserId, NotificationDto Notification)> Pushed { get; } = [];

    public Task PushAsync(Guid userId, NotificationDto notification, CancellationToken cancellationToken)
    {
        Pushed.Add((userId, notification));
        return Task.CompletedTask;
    }
}

public class NotificationTests
{
    private readonly OfferTestData _data = new();
    private readonly FakeSmsSender _sms = new();
    private readonly FakeNotificationPusher _pusher = new();

    private IAppDbContext Db => _data.Requests.Db;

    private FakeClock Clock => _data.Requests.Clock;

    private Task<PagedResult<NotificationDto>> ListAsync(ICurrentUser user, bool unreadOnly = false) =>
        new GetMyNotificationsHandler(Db, user).HandleAsync(new GetMyNotifications(unreadOnly), CancellationToken.None);

    private Task<int> UnreadAsync(ICurrentUser user) =>
        new GetUnreadNotificationCountHandler(Db, user).HandleAsync(new GetUnreadNotificationCount(), CancellationToken.None);

    private Task<int> DeliverAsync() =>
        new DeliverNotificationsHandler(Db, _sms, _pusher, Options.Create(new NotificationSettings { PortalUrl = "https://tnashen.test/" }), Clock)
            .HandleAsync(new DeliverNotifications(), CancellationToken.None);

    [Fact]
    public async Task A_new_request_and_an_offer_notify_the_other_side_with_a_link()
    {
        var aram = _data.Requests.GivenPartner("Aram");
        var request = await _data.Requests.CreateAsync(_data.Requests.OpenRequest());
        var partner = OfferTestData.As(aram);

        var received = (await ListAsync(partner)).Items.ShouldHaveSingleItem();
        received.Type.ShouldBe("RequestReceived");
        received.Link.ShouldBe($"/inbox/{request.Id}");
        received.ReadAt.ShouldBeNull();

        await _data.SendAsync(aram, _data.Work(request.Id, price: 25_000));
        var offer = (await ListAsync(_data.Customer)).Items.ShouldHaveSingleItem();
        offer.Type.ShouldBe("OfferReceived");
        offer.Link.ShouldBe($"/requests/{request.Id}");
        offer.Params["name"].ShouldBe("Aram");
        offer.Params["price"].ShouldBe("25000");
        offer.Params["kind"].ShouldBe("Work");
    }

    [Fact]
    public async Task Users_read_their_notifications_one_by_one_or_all_at_once()
    {
        var me = _data.Customer;
        var userId = _data.Requests.Partners.User.Id;
        foreach (var minutes in new[] { 1, 2, 3 })
        {
            Db.Notifications.Add(Notification.Create(userId, NotificationType.OrderStarted, "/orders/1", "{}", false, OfferTestData.Now.AddMinutes(minutes)));
        }

        Db.Notifications.Add(Notification.Create(_data.Requests.Partners.OtherUser.Id, NotificationType.OrderStarted, "/orders/2", "{}", false, OfferTestData.Now));
        await Db.SaveChangesAsync();

        var list = await ListAsync(me);
        list.TotalCount.ShouldBe(3);
        list.Items.Select(n => n.CreatedAt).ShouldBe(list.Items.Select(n => n.CreatedAt).OrderDescending());
        (await UnreadAsync(me)).ShouldBe(3);

        var read = await new MarkNotificationReadHandler(Db, me, Clock).HandleAsync(new MarkNotificationRead(list.Items[0].Id), CancellationToken.None);
        read.ReadAt.ShouldBe(Clock.Now);
        (await UnreadAsync(me)).ShouldBe(2);
        (await ListAsync(me, unreadOnly: true)).Items.Count.ShouldBe(2);

        (await new MarkAllNotificationsReadHandler(Db, me, Clock).HandleAsync(new MarkAllNotificationsRead(), CancellationToken.None)).ShouldBe(2);
        (await UnreadAsync(me)).ShouldBe(0);
        (await UnreadAsync(new FakeCurrentUser(_data.Requests.Partners.OtherUser.Id))).ShouldBe(1);
    }

    [Fact]
    public async Task Delivery_sends_an_sms_only_for_important_types_and_pushes_every_notification_once()
    {
        var aram = _data.Requests.GivenPartner("Aram");
        var request = await _data.Requests.CreateAsync(_data.Requests.OpenRequest());
        Clock.Now = OfferTestData.Now.AddMinutes(1);
        var offer = await _data.SendAsync(aram, _data.Work(request.Id));
        Clock.Now = OfferTestData.Now.AddMinutes(2);
        await _data.RejectAsync(offer.Id, "Too expensive");

        (await DeliverAsync()).ShouldBe(3);

        _sms.Sent.Count.ShouldBe(2);
        var partnerPhone = Db.Users.Single(u => u.Id == aram.UserId).Phone;
        _sms.Sent.ShouldContain(m => m.To == partnerPhone && m.Message.EndsWith($"https://tnashen.test/hy/inbox/{request.Id}", StringComparison.Ordinal));
        _sms.Sent.ShouldContain(m => m.To == _data.Requests.Partners.User.Phone && m.Message.StartsWith("TnaShen:", StringComparison.Ordinal));
        _pusher.Pushed.Select(p => p.Notification.Type).ShouldBe(new[] { "RequestReceived", "OfferReceived", "OfferRejected" });

        (await DeliverAsync()).ShouldBe(0);
        _sms.Sent.Count.ShouldBe(2);
    }

    [Fact]
    public async Task Partner_profile_decisions_notify_the_owner()
    {
        var p = _data.Requests.Partners;
        var profile = p.GivenProfile("Under review", PartnerStatus.UnderReview);
        var handler = new DecideOnPartnerHandler(Db, p.Dtos, Clock);

        await handler.HandleAsync(new DecideOnPartner(profile.Id, PartnerDecision.RequestChanges, "Add a photo"), CancellationToken.None);

        var notification = Db.Notifications.Single(n => n.UserId == profile.UserId);
        notification.Type.ShouldBe(NotificationType.PartnerNeedsChanges);
        notification.Link.ShouldBe("/partner");
        notification.SendSms.ShouldBeTrue();
    }
}
