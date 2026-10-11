using HomeServices.Application.Common;
using HomeServices.Application.Errors;
using HomeServices.Application.Estimates;
using HomeServices.Application.Requests;
using HomeServices.Application.Tests.Requests;
using HomeServices.Domain.Catalog;
using HomeServices.Domain.Estimates;
using HomeServices.Domain.Localization;

namespace HomeServices.Application.Tests.Estimates;

public class AdminEstimatesTests
{
    private readonly RequestTestData _data = new();
    private readonly Estimate _flat;
    private readonly Estimate _dacha;
    private readonly Estimate _old;

    public AdminEstimatesTests()
    {
        var tiling = WorkItem.Create(_data.Partners.Plumbing.Id, "floor-tiling", LocalizedText.Empty.With("hy", "tiling"), WorkUnit.SquareMeter, WorkSurface.Floor, 1);
        tiling.SetPrice(new PriceRange(5_000, 6_500, 9_000));
        _data.Db.WorkItems.Add(tiling);

        _flat = Given(_data.Partners.User.Id, "Our flat", RequestTestData.Now.AddDays(-1), tiling);
        _dacha = Given(_data.Partners.User.Id, "Dacha", RequestTestData.Now.AddDays(-2), tiling);
        _dacha.Share("abcdefghijklmnopqrstuv");
        _old = Given(_data.Partners.OtherUser.Id, "Old house", RequestTestData.Now.AddDays(-60), tiling);
        _data.Db.SaveChanges();
    }

    private Estimate Given(Guid userId, string title, DateTimeOffset createdAt, WorkItem work)
    {
        var estimate = Estimate.Create(userId, title, null);
        estimate.AddRoom("Bath", RoomType.Bathroom, new RoomSize(null, null, 4m, 2.7m), []).AddLine(work.Id, null);
        estimate.MarkCreated(createdAt, null);
        _data.Db.Estimates.Add(estimate);
        return estimate;
    }

    private Task<PagedResult<AdminEstimateListItemDto>> ListAsync(GetAdminEstimates query) =>
        new GetAdminEstimatesHandler(_data.Db, _data.Language).HandleAsync(query, CancellationToken.None);

    [Fact]
    public async Task Staff_see_estimates_with_owners_totals_and_requests()
    {
        _data.GivenPartner("Plumber");
        var request = await _data.CreateAsync(_data.OpenRequest() with { EstimateId = _flat.Id });

        var all = await ListAsync(new GetAdminEstimates());

        all.TotalCount.ShouldBe(3);
        all.Items.Select(e => e.Title).ShouldBe(new[] { "Our flat", "Dacha", "Old house" });
        var flat = all.Items[0];
        flat.Owner!.Phone.ShouldBe(_data.Partners.User.Phone.Value);
        (flat.RoomCount, flat.LineCount, flat.TotalMin, flat.TotalMax, flat.RequestCount).ShouldBe((1, 1, 20_000, 36_000, 1));
        all.Items[1].IsShared.ShouldBeTrue();

        (await ListAsync(new GetAdminEstimates(WithRequest: true))).Items.Single().Id.ShouldBe(_flat.Id);
        (await ListAsync(new GetAdminEstimates(WithRequest: false))).TotalCount.ShouldBe(2);
        (await ListAsync(new GetAdminEstimates(Shared: true))).Items.Single().Id.ShouldBe(_dacha.Id);
        (await ListAsync(new GetAdminEstimates(Search: "DACHA"))).Items.Single().Id.ShouldBe(_dacha.Id);
        (await ListAsync(new GetAdminEstimates(Search: _data.Partners.OtherUser.Phone.Value))).Items.Single().Id.ShouldBe(_old.Id);

        var detail = await new GetAdminEstimateHandler(_data.Db, _data.Language).HandleAsync(new GetAdminEstimate(_flat.Id), CancellationToken.None);
        detail.Rooms.Single().Name.ShouldBe("Bath");
        detail.Measurement.TotalTypical.ShouldBe(26_000);
        detail.Requests.Single().ShouldSatisfyAllConditions(
            r => r.Id.ShouldBe(request.Id),
            r => r.Status.ShouldBe("Open"),
            r => r.LineCount.ShouldBe(1));

        var adminRequest = await new GetAdminRequestHandler(_data.Db, _data.Language, _data.Files).HandleAsync(new GetAdminRequest(request.Id), CancellationToken.None);
        adminRequest.EstimateId.ShouldBe(_flat.Id);
        adminRequest.Lines.Single().EstimateMax.ShouldBe(36_000);
        (await new GetAdminRequestsHandler(_data.Db, _data.Language).HandleAsync(new GetAdminRequests(), CancellationToken.None)).Items.Single().LineCount.ShouldBe(1);
    }

    [Fact]
    public async Task Stats_count_how_many_estimates_became_requests()
    {
        _data.GivenPartner("Plumber");
        await _data.CreateAsync(_data.OpenRequest() with { EstimateId = _flat.Id });
        await _data.CreateAsync(_data.OpenRequest() with { EstimateId = _flat.Id });

        var stats = await new GetEstimateStatsHandler(_data.Db, _data.Clock).HandleAsync(new GetEstimateStats(), CancellationToken.None);

        stats.ShouldBe(new EstimateStatsDto(Total: 3, WithRequest: 1, Shared: 1, ConversionPercent: 33, TotalLast30Days: 2, WithRequestLast30Days: 1));
    }

    [Fact]
    public async Task An_unknown_estimate_is_not_found() =>
        (await Should.ThrowAsync<NotFoundException>(() =>
            new GetAdminEstimateHandler(_data.Db, _data.Language).HandleAsync(new GetAdminEstimate(Guid.NewGuid()), CancellationToken.None)))
            .Code.ShouldBe("estimate.not_found");
}
