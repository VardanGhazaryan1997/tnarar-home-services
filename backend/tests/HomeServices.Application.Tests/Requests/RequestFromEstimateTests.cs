using HomeServices.Application.Errors;
using HomeServices.Application.Offers;
using HomeServices.Application.Requests;
using HomeServices.Application.Tests.Offers;
using HomeServices.Domain;
using HomeServices.Domain.Catalog;
using HomeServices.Domain.Estimates;
using HomeServices.Domain.Localization;
using HomeServices.Domain.Offers;
using Microsoft.EntityFrameworkCore;

namespace HomeServices.Application.Tests.Requests;

public class RequestFromEstimateTests
{
    private readonly OfferTestData _data = new();
    private readonly WorkItem _tiling;
    private readonly WorkItem _boiler;
    private readonly Estimate _estimate;

    public RequestFromEstimateTests()
    {
        var partners = _data.Requests.Partners;
        _tiling = Item(partners.Plumbing.Id, "floor-tiling", WorkUnit.SquareMeter, WorkSurface.Floor, new PriceRange(5_000, 6_500, 9_000));
        _boiler = Item(partners.Boilers.Id, "boiler-installation", WorkUnit.Piece, WorkSurface.None, new PriceRange(20_000, 30_000, 40_000));

        _estimate = Estimate.Create(partners.User.Id, "Our flat", null);
        var bath = _estimate.AddRoom("Bath", RoomType.Bathroom, new RoomSize(null, null, 4m, 2.7m), []);
        bath.AddLine(_tiling.Id, null);
        var kitchen = _estimate.AddRoom("Kitchen", RoomType.Kitchen, new RoomSize(null, null, 9m, 2.7m), []);
        kitchen.AddLine(_boiler.Id, 1m);
        _data.Requests.Db.Estimates.Add(_estimate);
        _data.Requests.Db.SaveChanges();
    }

    private WorkItem Item(Guid categoryId, string slug, WorkUnit unit, WorkSurface surface, PriceRange price)
    {
        var item = WorkItem.Create(categoryId, slug, LocalizedText.Empty.With("hy", slug), unit, surface, 1);
        item.SetPrice(price);
        _data.Requests.Db.WorkItems.Add(item);
        return item;
    }

    [Fact]
    public async Task A_request_made_from_an_estimate_carries_its_work_room_by_room()
    {
        _data.Requests.GivenPartner("Plumber");
        var heating = _data.Requests.GivenPartner("Heating only", p => p.SetServices([_data.Requests.Partners.Heating.Id]));

        var request = await _data.Requests.CreateAsync(_data.Requests.OpenRequest() with { EstimateId = _estimate.Id });

        request.EstimateId.ShouldBe(_estimate.Id);
        request.Lines.Select(l => (l.RoomName, l.WorkItemId, l.Name, l.Unit, l.Quantity, l.EstimateMin, l.EstimateMax)).ShouldBe(new[]
        {
            ("Bath", _tiling.Id, "floor-tiling", WorkUnit.SquareMeter, (decimal?)4m, (int?)20_000, (int?)36_000),
            ("Kitchen", _boiler.Id, "boiler-installation", WorkUnit.Piece, (decimal?)1m, (int?)20_000, (int?)40_000),
        });
        // The boiler line is heating work, so heating partners get the request too.
        request.SentTo.ShouldBe(2);

        var seen = await new GetInboxRequestHandler(_data.Requests.Db, OfferTestData.As(heating), _data.Requests.Language, _data.Requests.Files, _data.Requests.Clock)
            .HandleAsync(new GetInboxRequest(request.Id), CancellationToken.None);
        seen.Lines.Select(l => (l.RoomName, l.Quantity)).ShouldBe(new[] { ("Bath", (decimal?)4m), ("Kitchen", (decimal?)1m) });
        seen.Lines.ShouldAllBe(l => l.EstimateMin == null && l.EstimateMax == null); // partners never see the estimate
    }

    [Fact]
    public async Task Only_the_customers_own_estimate_can_be_used()
    {
        var other = Estimate.Create(_data.Requests.Partners.OtherUser.Id, "Not mine", null);
        _data.Requests.Db.Estimates.Add(other);
        await _data.Requests.Db.SaveChangesAsync();

        (await Should.ThrowAsync<NotFoundException>(() => _data.Requests.CreateAsync(_data.Requests.OpenRequest() with { EstimateId = other.Id })))
            .Code.ShouldBe("estimate.not_found");
        (await _data.Requests.Db.ServiceRequests.CountAsync()).ShouldBe(0);
    }

    [Fact]
    public async Task Partners_price_the_requests_lines_and_the_customer_sees_each_amount()
    {
        var plumber = _data.Requests.GivenPartner("Plumber");
        var request = await _data.Requests.CreateAsync(_data.Requests.OpenRequest() with { EstimateId = _estimate.Id });
        var (bath, kitchen) = (request.Lines[0], request.Lines[1]);
        var offer = _data.Work(request.Id, 61_000) with
        {
            Lines =
            [
                new OfferLine("Bath · floor-tiling", true, bath.Id, 4m, 7_000),
                new OfferLine("Kitchen · boiler-installation", true, kitchen.Id, 1m, 33_000),
            ],
            Stages = null,
        };

        var sent = await _data.SendAsync(plumber, offer);

        sent.Lines.Select(l => (l.RequestLineId, l.Amount)).ShouldBe(new[] { ((Guid?)bath.Id, (int?)28_000), ((Guid?)kitchen.Id, (int?)33_000) });
        var seen = (await _data.OffersAsync(request.Id)).Single();
        seen.Lines.Sum(l => l.Amount ?? 0).ShouldBe(61_000);
    }

    [Fact]
    public async Task Offer_lines_must_answer_lines_of_the_same_request()
    {
        var plumber = _data.Requests.GivenPartner("Plumber");
        var request = await _data.Requests.CreateAsync(_data.Requests.OpenRequest());
        var offer = _data.Work(request.Id, 7_000) with { Lines = [new OfferLine("Tiling", true, Guid.NewGuid(), 1m, 7_000)], Stages = null };

        (await Should.ThrowAsync<DomainException>(() => _data.SendAsync(plumber, offer))).Code.ShouldBe("offer.line_invalid");
    }

    [Fact]
    public async Task Line_prices_are_validated()
    {
        var validator = new SendOfferValidator(_data.Requests.Clock);
        var request = Guid.NewGuid();

        var excludedWithPrice = _data.Work(request, 1_000) with { Lines = [new OfferLine("Tiling", false, null, 1m, 1_000)], Stages = null };
        var badQuantity = _data.Work(request, 1_000) with { Lines = [new OfferLine("Tiling", true, null, 0m, 1_000)], Stages = null };

        (await validator.ValidateAsync(excludedWithPrice)).Errors.Select(e => e.ErrorCode).ShouldBe(new[] { "lines.price_invalid" });
        (await validator.ValidateAsync(badQuantity)).Errors.Select(e => e.ErrorCode).ShouldBe(new[] { "lines.price_invalid" });
    }
}
