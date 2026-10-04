using HomeServices.Application.Errors;
using HomeServices.Application.Orders;
using HomeServices.Application.Tests.Support;
using HomeServices.Domain.Orders;

namespace HomeServices.Application.Tests.Offers;

public class OrderQueryTests
{
    private readonly OfferTestData _data = new();

    [Fact]
    public async Task Each_party_sees_the_order_with_the_others_contact_details()
    {
        var aram = _data.Requests.GivenPartner("Aram");
        var customer = _data.Requests.Db.Users.Single(u => u.Id == _data.Requests.Partners.User.Id);
        customer.UpdateProfile("Ani Petrosyan", null);
        await _data.Requests.Db.SaveChangesAsync();
        var request = _data.Requests.GivenRequest(partners: [aram]);
        var offer = await _data.SendAsync(aram, _data.Work(request.Id));
        var order = await _data.AcceptAsync(offer.Id);

        var asPartner = await _data.OrderAsync(order.Id, OfferTestData.As(aram));
        asPartner.MyRole.ShouldBe("Partner");
        asPartner.Customer.FullName.ShouldBe("Ani Petrosyan");
        asPartner.Customer.Phone.ShouldBe("+37477123456");
        asPartner.Terms.Summary.ShouldBe(order.Terms.Summary);

        (await _data.OrderAsync(order.Id, _data.Customer)).MyRole.ShouldBe("Customer");
        (await Should.ThrowAsync<NotFoundException>(() => _data.OrderAsync(order.Id, new FakeCurrentUser(_data.Requests.Partners.OtherUser.Id))))
            .Code.ShouldBe("order.not_found");
        (await Should.ThrowAsync<NotFoundException>(() => _data.OrderAsync(Guid.NewGuid(), _data.Customer))).Code.ShouldBe("order.not_found");
    }

    [Fact]
    public async Task My_orders_cover_both_sides()
    {
        var aram = _data.Requests.GivenPartner("Aram");
        var gor = _data.Requests.GivenPartner("Gor");
        var forMe = _data.Requests.GivenRequest(partners: [aram]);
        var byMe = _data.Requests.GivenRequest(customerId: aram.UserId, partners: [gor]);
        var myOrder = await _data.AcceptAsync((await _data.SendAsync(aram, _data.Work(forMe.Id))).Id);
        var aramsOwnOrder = await _data.AcceptAsync((await _data.SendAsync(gor, _data.Visit(byMe.Id))).Id, OfferTestData.As(aram));

        var all = await _data.OrdersAsync(OfferTestData.As(aram));
        all.TotalCount.ShouldBe(2);
        var asPartner = all.Items.Single(i => i.Id == myOrder.Id);
        asPartner.MyRole.ShouldBe("Partner");
        asPartner.OtherParty.ShouldBeNull();
        asPartner.Summary.ShouldBe("Replace the kitchen tap and the pipes under the sink.");
        asPartner.Price.ShouldBe(100_000);
        asPartner.StartDate.ShouldBe(new DateOnly(2026, 10, 8));
        var asCustomer = all.Items.Single(i => i.Id == aramsOwnOrder.Id);
        asCustomer.MyRole.ShouldBe("Customer");
        asCustomer.Kind.ShouldBe("Visit");
        asCustomer.OtherParty.ShouldBe("Gor");
        asCustomer.VisitAt.ShouldBe(OfferTestData.Now.AddDays(1));

        (await _data.OrdersAsync(OfferTestData.As(aram), new GetMyOrders(OrderRole.Partner))).Items.ShouldHaveSingleItem().Id.ShouldBe(myOrder.Id);
        (await _data.OrdersAsync(OfferTestData.As(aram), new GetMyOrders(OrderRole.Customer))).Items.ShouldHaveSingleItem().Id.ShouldBe(aramsOwnOrder.Id);
        (await _data.OrdersAsync(OfferTestData.As(aram), new GetMyOrders(Status: OrderStatus.Completed))).Items.ShouldBeEmpty();
        (await _data.OrdersAsync(OfferTestData.As(aram), new GetMyOrders(Page: 2, PageSize: 1))).Items.ShouldHaveSingleItem();
        (await _data.OrdersAsync(_data.Customer)).Items.ShouldHaveSingleItem().OtherParty.ShouldBe("Aram");
        (await _data.OrdersAsync(new FakeCurrentUser(_data.Requests.Partners.OtherUser.Id))).Items.ShouldBeEmpty();
    }
}
