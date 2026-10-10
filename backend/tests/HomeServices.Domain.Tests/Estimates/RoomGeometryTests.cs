using HomeServices.Domain.Catalog;
using HomeServices.Domain.Estimates;

namespace HomeServices.Domain.Tests.Estimates;

public class RoomGeometryTests
{
    private static readonly RoomOpening Door = new(OpeningKind.Door, 0.9m, 2.1m);
    private static readonly RoomOpening Window = new(OpeningKind.Window, 1.5m, 1.4m);

    [Fact]
    public void A_rectangular_room_gives_its_floor_walls_ceiling_and_skirting()
    {
        var geometry = RoomGeometry.Of(new RoomSize(4m, 3m, null, 2.7m), [Door, Window]);

        geometry.FloorArea.ShouldBe(12m);
        geometry.CeilingArea.ShouldBe(12m);
        geometry.Perimeter.ShouldBe(14m);
        geometry.WallArea.ShouldBe(33.81m); // 14 × 2.7 − 1.89 − 2.1
        geometry.SkirtingLength.ShouldBe(13.1m); // 14 − 0.9 for the door
    }

    [Fact]
    public void A_room_given_only_by_its_area_is_treated_as_a_square()
    {
        var geometry = RoomGeometry.Of(new RoomSize(null, null, 16m, 2.5m), []);

        geometry.FloorArea.ShouldBe(16m);
        geometry.Perimeter.ShouldBe(16m);
        geometry.WallArea.ShouldBe(40m);
    }

    [Fact]
    public void Openings_count_as_many_times_as_there_are_and_walls_never_go_below_zero()
    {
        var geometry = RoomGeometry.Of(new RoomSize(1m, 1m, null, 2m), [new RoomOpening(OpeningKind.Window, 2m, 2m, Count: 3)]);

        geometry.WallArea.ShouldBe(0m);
        new RoomOpening(OpeningKind.Door, 0.8m, 2m, 2).Area.ShouldBe(3.2m);
    }

    [Theory]
    [InlineData(WorkUnit.SquareMeter, WorkSurface.Floor, 12)]
    [InlineData(WorkUnit.SquareMeter, WorkSurface.Wall, 33.81)]
    [InlineData(WorkUnit.SquareMeter, WorkSurface.Ceiling, 12)]
    [InlineData(WorkUnit.SquareMeter, WorkSurface.None, 12)]
    [InlineData(WorkUnit.RunningMeter, WorkSurface.Floor, 13.1)]
    [InlineData(WorkUnit.RunningMeter, WorkSurface.Ceiling, 14)]
    [InlineData(WorkUnit.Fixed, WorkSurface.None, 1)]
    public void Quantities_follow_the_unit_and_surface(WorkUnit unit, WorkSurface surface, double expected)
    {
        var geometry = RoomGeometry.Of(new RoomSize(4m, 3m, null, 2.7m), [Door, Window]);

        geometry.QuantityFor(unit, surface).ShouldBe((decimal)expected);
    }

    [Theory]
    [InlineData(WorkUnit.Piece)]
    [InlineData(WorkUnit.Point)]
    [InlineData(WorkUnit.Hour)]
    [InlineData(WorkUnit.CubicMeter)]
    public void Counted_work_cannot_be_measured_from_the_room(WorkUnit unit) =>
        RoomGeometry.Of(new RoomSize(4m, 3m, null, 2.7m), []).QuantityFor(unit, WorkSurface.None).ShouldBeNull();

    [Theory]
    [InlineData(null, null, null, 2.7)]
    [InlineData(4.0, null, null, 2.7)]
    [InlineData(0.0, 3.0, null, 2.7)]
    [InlineData(101.0, 3.0, null, 2.7)]
    [InlineData(null, null, 2001.0, 2.7)]
    [InlineData(4.0, 3.0, null, 1.2)]
    [InlineData(4.0, 3.0, null, 11.0)]
    public void Impossible_sizes_are_refused(double? length, double? width, double? area, double height)
    {
        var size = new RoomSize((decimal?)length, (decimal?)width, (decimal?)area, (decimal)height);

        Should.Throw<DomainException>(() => RoomGeometry.Of(size, [])).Code.ShouldBe("estimate.size_invalid");
    }

    [Fact]
    public void Impossible_openings_are_refused()
    {
        Should.Throw<DomainException>(() => RoomGeometry.Of(new RoomSize(4m, 3m, null, 2.7m), [new RoomOpening(OpeningKind.Door, 0m, 2m)]))
            .Code.ShouldBe("estimate.opening_invalid");
        Should.Throw<DomainException>(() => new RoomOpening(OpeningKind.Window, 1m, 1m, 0).EnsureValid()).Code.ShouldBe("estimate.opening_invalid");
        Should.Throw<DomainException>(() => new RoomOpening((OpeningKind)9, 1m, 1m).EnsureValid()).Code.ShouldBe("estimate.opening_invalid");
    }
}
