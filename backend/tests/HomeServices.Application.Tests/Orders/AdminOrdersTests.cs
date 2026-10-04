using HomeServices.Application.Common;
using HomeServices.Application.Errors;
using HomeServices.Application.Orders;
using HomeServices.Application.Tests.Offers;
using HomeServices.Domain;
using HomeServices.Domain.Orders;

namespace HomeServices.Application.Tests.Orders;

public class AdminOrdersTests
{
    private readonly OfferTestData _data = new();

    private Task<PagedResult<AdminOrderListItemDto>> ListAsync(GetAdminOrders? query = null) =>
        new GetAdminOrdersHandler(_data.Requests.Db, _data.Requests.Language).HandleAsync(query ?? new GetAdminOrders(), CancellationToken.None);

    private Task<OrderDto> GetAsync(Guid id) =>
        new GetAdminOrderHandler(_data.Requests.Db, _data.Requests.Language).HandleAsync(new GetAdminOrder(id), CancellationToken.None);

    private Task<OrderDto> CancelAsync(Guid id, string reason) =>
        new CancelOrderByStaffHandler(_data.Requests.Db, _data.Requests.Language, _data.Requests.Clock).HandleAsync(new CancelOrderByStaff(id, reason), CancellationToken.None);

    private Task<OrderDto> ResolveAsync(Guid id) =>
        new ResolveOrderHandler(_data.Requests.Db, _data.Requests.Language).HandleAsync(new ResolveOrder(id), CancellationToken.None);

    [Fact]
    public async Task Staff_list_find_and_open_orders()
    {
        var aram = _data.Requests.GivenPartner("Aram Plumbing");
        var gor = _data.Requests.GivenPartner("Gor Masters");
        var first = _data.Requests.GivenRequest(partners: [aram]);
        var second = _data.Requests.GivenRequest(partners: [gor]);
        var work = await _data.AcceptAsync((await _data.SendAsync(aram, _data.Work(first.Id))).Id);
        var visit = await _data.AcceptAsync((await _data.SendAsync(gor, _data.Visit(second.Id))).Id);
        await new ProposeOrderChangeHandler(_data.Requests.Db, OfferTestData.As(aram), _data.Requests.Language, _data.Requests.Clock)
            .HandleAsync(new ProposeOrderChange(work.Id, ChangeRequestKind.Schedule, null, null, null, null, 4, null), CancellationToken.None);

        var all = await ListAsync();
        all.TotalCount.ShouldBe(2);
        var row = all.Items.Single(i => i.Id == work.Id);
        row.PartnerName.ShouldBe("Aram Plumbing");
        row.CustomerPhone.ShouldBe("+37477123456");
        row.PendingChange.ShouldBeTrue();
        row.Summary.ShouldBe("Replace the kitchen tap and the pipes under the sink.");
        all.Items.Single(i => i.Id == visit.Id).PendingChange.ShouldBeFalse();

        (await ListAsync(new GetAdminOrders(Kind: OrderKind.Visit))).Items.ShouldHaveSingleItem().Id.ShouldBe(visit.Id);
        (await ListAsync(new GetAdminOrders(Status: OrderStatus.Cancelled))).Items.ShouldBeEmpty();
        (await ListAsync(new GetAdminOrders(Search: "gor"))).Items.ShouldHaveSingleItem().Id.ShouldBe(visit.Id);
        (await ListAsync(new GetAdminOrders(Search: "+37477123456"))).TotalCount.ShouldBe(2);
        (await ListAsync(new GetAdminOrders(Page: 2, PageSize: 1))).Items.ShouldHaveSingleItem();

        var detail = await GetAsync(work.Id);
        detail.MyRole.ShouldBe("Staff");
        detail.ChangeRequests.ShouldHaveSingleItem().Mine.ShouldBeFalse();
        detail.Actions.ShouldBe(new[] { OrderActions.Cancel });
        (await Should.ThrowAsync<NotFoundException>(() => GetAsync(Guid.NewGuid()))).Code.ShouldBe("order.not_found");
    }

    [Fact]
    public async Task Staff_cancel_orders_and_resolve_the_ones_that_need_attention()
    {
        var aram = _data.Requests.GivenPartner("Aram");
        var request = _data.Requests.GivenRequest(partners: [aram]);
        var order = await _data.AcceptAsync((await _data.SendAsync(aram, _data.Work(request.Id))).Id);
        await new StartOrderHandler(_data.Requests.Db, OfferTestData.As(aram), _data.Requests.Language, _data.Requests.Clock)
            .HandleAsync(new StartOrder(order.Id), CancellationToken.None);
        await new CancelOrderHandler(_data.Requests.Db, _data.Customer, _data.Requests.Language, _data.Requests.Clock)
            .HandleAsync(new CancelOrder(order.Id, "Changed my mind"), CancellationToken.None);

        var queue = await ListAsync(new GetAdminOrders(NeedsAttention: true));
        queue.Items.ShouldHaveSingleItem().NeedsAttentionSince.ShouldNotBeNull();
        (await ListAsync(new GetAdminOrders(NeedsAttention: false))).Items.ShouldBeEmpty();
        (await GetAsync(order.Id)).Actions.ShouldBe(new[] { OrderActions.Resolve });

        var resolved = await ResolveAsync(order.Id);
        resolved.NeedsAttentionSince.ShouldBeNull();
        (await Should.ThrowAsync<DomainException>(() => ResolveAsync(order.Id))).Code.ShouldBe("order.not_flagged");

        var other = await _data.AcceptAsync((await _data.SendAsync(aram, _data.Work(_data.Requests.GivenRequest(partners: [aram]).Id))).Id);
        var cancelled = await CancelAsync(other.Id, "Duplicate order");
        cancelled.CancelledBy.ShouldBe("Staff");
        cancelled.History[^1].By.ShouldBe("Staff");
        (await Should.ThrowAsync<DomainException>(() => CancelAsync(other.Id, "Again"))).Code.ShouldBe("order.cannot_cancel");
        (await new CancelOrderByStaffValidator().ValidateAsync(new CancelOrderByStaff(other.Id, ""))).Errors.ShouldHaveSingleItem().ErrorCode.ShouldBe("reason.required");
        (await new GetAdminOrdersValidator().ValidateAsync(new GetAdminOrders(PageSize: 101))).Errors.ShouldHaveSingleItem().ErrorCode.ShouldBe("page_size.invalid");
    }
}
