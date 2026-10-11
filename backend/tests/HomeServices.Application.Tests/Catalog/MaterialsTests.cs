using HomeServices.Application.Catalog;
using HomeServices.Application.Errors;
using HomeServices.Application.Estimates;
using HomeServices.Application.Stores;
using HomeServices.Application.Tests.Support;
using HomeServices.Domain;
using HomeServices.Domain.Catalog;
using HomeServices.Domain.Estimates;
using HomeServices.Domain.Localization;
using Microsoft.Extensions.Options;

namespace HomeServices.Application.Tests.Catalog;

public class MaterialsTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 11, 9, 0, 0, TimeSpan.Zero);

    private readonly InMemoryAppDbContext _db = InMemoryAppDbContext.Create();
    private readonly FakeClock _clock = new(Now);
    private readonly WorkItem _tiling;
    private readonly WorkItem _sockets;
    private readonly Material _tiles;
    private readonly Material _adhesive;
    private readonly Material _socket;
    private readonly Material _hidden;

    public MaterialsTests()
    {
        var category = Category.Create("tiling", LocalizedText.Empty.With("hy", "Սալիկ"), 1);
        _db.Categories.Add(category);
        _tiling = WorkItem.Create(category.Id, "floor-tiling", Text("floor-tiling", "Floor tiling"), WorkUnit.SquareMeter, WorkSurface.Floor, 1);
        _sockets = WorkItem.Create(category.Id, "sockets", Text("sockets", "Sockets"), WorkUnit.Point, WorkSurface.None, 2);
        _db.WorkItems.AddRange(_tiling, _sockets);

        _tiles = Material.Create("floor-tiles", Text("tiles", "Floor tiles"), MaterialUnit.SquareMeter, 5000, 9000, 18000, 1, Now);
        _adhesive = Material.Create("tile-adhesive", Text("adhesive", "Tile adhesive"), MaterialUnit.Kilogram, 100, 160, 260, 2, Now);
        _socket = Material.Create("socket", Text("socket", "Socket"), MaterialUnit.Piece, 1500, 3500, 8000, 3, Now);
        _hidden = Material.Create("hidden", Text("hidden", "Hidden"), MaterialUnit.Piece, 1, 1, 1, 4, Now);
        _hidden.Deactivate();
        _db.Materials.AddRange(_tiles, _adhesive, _socket, _hidden);
        _db.WorkItemMaterials.AddRange(
            WorkItemMaterial.Create(_tiling.Id, _tiles.Id, 1m, 10, 1),
            WorkItemMaterial.Create(_tiling.Id, _adhesive.Id, 6m, 5, 2),
            WorkItemMaterial.Create(_tiling.Id, _hidden.Id, 1m, 0, 3),
            WorkItemMaterial.Create(_sockets.Id, _socket.Id, 1m, 0, 1));
        _db.SaveChanges();
    }

    private static LocalizedText Text(string hy, string en) => LocalizedText.Empty.With("hy", hy).With("en", en);

    private static Dictionary<string, string> Name(string en) => new() { ["hy"] = en, ["en"] = en };

    private Task<EstimateMeasurementDto> MeasureAsync(MaterialGrade? grade) =>
        new MeasureEstimateHandler(_db, new FakeCurrentLanguage("en")).HandleAsync(
            new MeasureEstimate(
                [
                    new RoomInput(RoomType.Bathroom, null, null, 10m, null, null, [new LineInput(_tiling.Id)]),
                    new RoomInput(RoomType.Kitchen, null, null, 5m, null, null, [new LineInput(_tiling.Id), new LineInput(_sockets.Id, 3m)]),
                ],
                Materials: grade),
            CancellationToken.None);

    [Fact]
    public async Task An_estimate_counts_and_prices_the_materials_its_work_needs()
    {
        var result = await MeasureAsync(MaterialGrade.Standard);

        result.MaterialGrade.ShouldBe(MaterialGrade.Standard);
        result.Materials!.Select(m => (m.Name, m.Unit, m.Quantity, m.UnitPrice, m.Cost)).ShouldBe(new[]
        {
            ("Floor tiles", MaterialUnit.SquareMeter, 16.5m, 9000, 149000), // 15 m² + 10% waste
            ("Tile adhesive", MaterialUnit.Kilogram, 94.5m, 160, 15100), // 6 kg per m² + 5%
            ("Socket", MaterialUnit.Piece, 3m, 3500, 10500),
        });
        result.MaterialsTotal.ShouldBe(174600);
        result.Rooms.Select(r => r.MaterialsCost).ShouldBe(new[] { 109000, 65000 });
    }

    [Fact]
    public async Task The_grade_picks_the_price_and_labour_only_counts_no_materials()
    {
        (await MeasureAsync(MaterialGrade.Budget)).Materials![0].UnitPrice.ShouldBe(5000);
        (await MeasureAsync(MaterialGrade.Premium)).Materials![0].UnitPrice.ShouldBe(18000);

        var labourOnly = await MeasureAsync(null);
        labourOnly.MaterialGrade.ShouldBeNull();
        labourOnly.Materials.ShouldBeEmpty();
        labourOnly.MaterialsTotal.ShouldBe(0);
    }

    [Theory]
    [InlineData(2.01, MaterialUnit.Piece, 3)]
    [InlineData(2.01, MaterialUnit.Kilogram, 2.1)]
    [InlineData(2.1, MaterialUnit.Liter, 2.1)]
    public void Quantities_are_rounded_up(double quantity, MaterialUnit unit, double expected) =>
        EstimateMaterials.RoundUp((decimal)quantity, unit).ShouldBe((decimal)expected);

    [Fact]
    public async Task Staff_manage_materials()
    {
        var created = await new CreateMaterialHandler(_db, _clock).HandleAsync(
            new CreateMaterial("Primer", Name("Primer"), MaterialUnit.Liter, 500, 850, 1400, 5),
            CancellationToken.None);
        (created.Slug, created.Unit, created.PriceStandard, created.IsActive).ShouldBe(("primer", MaterialUnit.Liter, 850, true));

        (await Should.ThrowAsync<ConflictException>(() => new CreateMaterialHandler(_db, _clock).HandleAsync(
            new CreateMaterial("primer", Name("Again"), MaterialUnit.Liter, 1, 1, 1, 6), CancellationToken.None)))
            .Code.ShouldBe("material.slug_taken");

        var updated = await new UpdateMaterialHandler(_db, Options.Create(new StorePricingSettings()), _clock).HandleAsync(
            new UpdateMaterial(created.Id, "primer", Name("Deep primer"), MaterialUnit.Liter, 600, 900, 1500, 5, IsActive: false),
            CancellationToken.None);
        (updated.Name["en"], updated.PriceBudget, updated.IsActive).ShouldBe(("Deep primer", 600, false));

        var list = await new GetAdminMaterialsHandler(_db, _clock).HandleAsync(new GetAdminMaterials(), CancellationToken.None);
        list.Select(m => m.Slug).ShouldBe(new[] { "floor-tiles", "tile-adhesive", "socket", "hidden", "primer" });
        list[0].WorkItemCount.ShouldBe(1);

        (await new DeleteMaterialHandler(_db).HandleAsync(new DeleteMaterial(created.Id), CancellationToken.None)).ShouldBeTrue();
        (await Should.ThrowAsync<DomainException>(() => new DeleteMaterialHandler(_db).HandleAsync(new DeleteMaterial(_tiles.Id), CancellationToken.None)))
            .Code.ShouldBe("material.in_use");
    }

    [Fact]
    public async Task Staff_set_the_materials_a_work_item_uses()
    {
        var set = await new SetWorkItemMaterialsHandler(_db).HandleAsync(
            new SetWorkItemMaterials(_tiling.Id, [new WorkItemMaterialInput(_adhesive.Id, 5m, 5), new WorkItemMaterialInput(_tiles.Id, 1m, 12)]),
            CancellationToken.None);
        set.ShouldBe(new[] { new WorkItemMaterialInput(_adhesive.Id, 5m, 5), new WorkItemMaterialInput(_tiles.Id, 1m, 12) });

        _db.ChangeTracker.Clear();
        (await new GetWorkItemMaterialsHandler(_db).HandleAsync(new GetWorkItemMaterials(_tiling.Id), CancellationToken.None)).ShouldBe(set);

        (await Should.ThrowAsync<DomainException>(() => new SetWorkItemMaterialsHandler(_db).HandleAsync(
            new SetWorkItemMaterials(_tiling.Id, [new WorkItemMaterialInput(Guid.NewGuid(), 1m)]), CancellationToken.None)))
            .Code.ShouldBe("work_item_material.material_not_found");
        (await Should.ThrowAsync<NotFoundException>(() => new GetWorkItemMaterialsHandler(_db).HandleAsync(
            new GetWorkItemMaterials(Guid.NewGuid()), CancellationToken.None)))
            .Code.ShouldBe("work_item.not_found");
    }

    [Fact]
    public async Task The_validators_check_prices_and_amounts()
    {
        var languages = new FakeLanguageCatalog("hy", "hy", "en");
        (await new CreateMaterialValidator(languages).ValidateAsync(new CreateMaterial("paint", Name("Paint"), MaterialUnit.Liter, 900, 800, 1000, 1)))
            .Errors.Select(e => e.ErrorCode).ShouldBe(new[] { "price.invalid" });

        var validator = new SetWorkItemMaterialsValidator();
        var id = Guid.NewGuid();
        (await validator.ValidateAsync(new SetWorkItemMaterials(_tiling.Id, [new WorkItemMaterialInput(id, 1m), new WorkItemMaterialInput(id, 2m)])))
            .Errors.Select(e => e.ErrorCode).ShouldBe(new[] { "items.duplicate" });
        (await validator.ValidateAsync(new SetWorkItemMaterials(_tiling.Id, [new WorkItemMaterialInput(id, 0m)])))
            .Errors.Select(e => e.ErrorCode).ShouldBe(new[] { "work_item_material.invalid" });
        (await validator.ValidateAsync(new SetWorkItemMaterials(_tiling.Id, [new WorkItemMaterialInput(id, 1m, 101)])))
            .Errors.Select(e => e.ErrorCode).ShouldBe(new[] { "work_item_material.invalid" });
    }

    [Fact]
    public async Task Staff_see_which_prices_need_checking_and_confirm_them()
    {
        _clock.Now = Now.AddDays(MaterialData.CheckEveryDays + 1);
        var list = await new GetAdminMaterialsHandler(_db, _clock).HandleAsync(new GetAdminMaterials(), CancellationToken.None);
        list.ShouldAllBe(m => m.PricesNeedChecking);

        var confirmed = await new ConfirmMaterialPricesHandler(_db, _clock).HandleAsync(new ConfirmMaterialPrices(_tiles.Id), CancellationToken.None);
        (confirmed.PricesCheckedAt, confirmed.PricesNeedChecking, confirmed.WorkItemCount).ShouldBe(((DateTimeOffset?)_clock.Now, false, 1));
    }
}
