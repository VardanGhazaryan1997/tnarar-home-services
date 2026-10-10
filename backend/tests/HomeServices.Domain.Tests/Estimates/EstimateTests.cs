using HomeServices.Domain.Estimates;

namespace HomeServices.Domain.Tests.Estimates;

public class EstimateTests
{
    private static readonly RoomSize Bedroom = new(4m, 3m, null, 2.7m);

    [Fact]
    public void An_estimate_has_rooms_in_order_with_their_work()
    {
        var userId = Guid.NewGuid();
        var estimate = Estimate.Create(userId, "  My flat ", cityId: null);
        var plastering = Guid.NewGuid();

        var bedroom = estimate.AddRoom("Bedroom", RoomType.Bedroom, Bedroom, [new RoomOpening(OpeningKind.Door, 0.9m, 2.1m)]);
        var bath = estimate.AddRoom("Bath", RoomType.Bathroom, new RoomSize(null, null, 4m, 2.5m), []);
        bedroom.AddLine(plastering, quantity: null);
        bedroom.AddLine(Guid.NewGuid(), quantity: 3.456m);

        estimate.Title.ShouldBe("My flat");
        estimate.UserId.ShouldBe(userId);
        estimate.Rooms.Select(r => r.SortOrder).ShouldBe(new[] { 1, 2 });
        bedroom.Geometry.FloorArea.ShouldBe(12m);
        bedroom.Openings.Single().ToOpening().Width.ShouldBe(0.9m);
        bedroom.Lines.Select(l => l.Quantity).ShouldBe(new decimal?[] { null, 3.46m });
        bath.Area.ShouldBe(4m);
        bath.Length.ShouldBeNull();

        estimate.RemoveRoom(bedroom.Id);
        estimate.Rooms.Single().SortOrder.ShouldBe(1);
    }

    [Fact]
    public void Changing_a_room_replaces_its_size_and_openings()
    {
        var room = Estimate.Create(null, "x", null).AddRoom("Kitchen", RoomType.Kitchen, Bedroom, [new RoomOpening(OpeningKind.Window, 1m, 1m)]);

        room.Change("Kitchen 2", RoomType.Kitchen, new RoomSize(null, null, 9m, 2.6m), []);

        room.Name.ShouldBe("Kitchen 2");
        (room.Length, room.Width, room.Area, room.Height).ShouldBe(((decimal?)null, (decimal?)null, (decimal?)9m, 2.6m));
        room.Openings.ShouldBeEmpty();
    }

    [Fact]
    public void Lines_are_unique_per_room_with_sensible_quantities()
    {
        var room = Estimate.Create(null, "x", null).AddRoom("Hall", RoomType.Hallway, Bedroom, []);
        var work = Guid.NewGuid();
        room.AddLine(work, null);

        Should.Throw<DomainException>(() => room.AddLine(work, null)).Code.ShouldBe("estimate.line_exists");
        Should.Throw<DomainException>(() => room.AddLine(Guid.NewGuid(), 0m)).Code.ShouldBe("estimate.quantity_invalid");
        room.RemoveLine(work);
        room.Lines.ShouldBeEmpty();
    }

    [Fact]
    public void Lines_keep_their_order()
    {
        var room = Estimate.Create(null, "x", null).AddRoom("Hall", RoomType.Hallway, Bedroom, []);
        var (a, b, c) = (Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        room.AddLine(a, null);
        room.AddLine(b, null);
        room.AddLine(c, 2m);

        room.RemoveLine(a);

        room.Lines.Select(l => (l.WorkItemId, l.SortOrder)).ShouldBe(new[] { (b, 1), (c, 2) });
    }

    [Fact]
    public void Titles_names_and_types_are_checked()
    {
        Should.Throw<DomainException>(() => Estimate.Create(null, " ", null)).Code.ShouldBe("estimate.title_invalid");
        var estimate = Estimate.Create(null, "x", null);
        Should.Throw<DomainException>(() => estimate.AddRoom("", RoomType.Other, Bedroom, [])).Code.ShouldBe("estimate.room_name_invalid");
        Should.Throw<DomainException>(() => estimate.AddRoom("A", (RoomType)99, Bedroom, [])).Code.ShouldBe("estimate.room_type_invalid");
        Should.Throw<DomainException>(() => estimate.RemoveRoom(Guid.NewGuid())).Code.ShouldBe("estimate.room_not_found");
    }

    [Fact]
    public void A_guest_estimate_can_be_claimed_once()
    {
        var estimate = Estimate.Create(null, "x", null);
        var user = Guid.NewGuid();

        estimate.ClaimFor(user);
        estimate.ClaimFor(user);

        estimate.UserId.ShouldBe(user);
        Should.Throw<DomainException>(() => estimate.ClaimFor(Guid.NewGuid())).Code.ShouldBe("estimate.owned");
    }

    [Fact]
    public void An_estimate_has_at_most_30_rooms()
    {
        var estimate = Estimate.Create(null, "x", null);
        for (var i = 0; i < Estimate.MaxRooms; i++)
        {
            estimate.AddRoom($"Room {i}", RoomType.Other, Bedroom, []);
        }

        Should.Throw<DomainException>(() => estimate.AddRoom("One more", RoomType.Other, Bedroom, [])).Code.ShouldBe("estimate.too_many_rooms");
    }

    [Fact]
    public void An_estimate_is_shared_by_a_token_until_sharing_stops()
    {
        var estimate = Estimate.Create(null, "x", null, oldBuilding: true);
        estimate.OldBuilding.ShouldBeTrue();
        estimate.ShareToken.ShouldBeNull();

        estimate.Share("abcdefghijklmnop_-12").ShouldBe("abcdefghijklmnop_-12");
        estimate.Share("another-token-0000000").ShouldBe("abcdefghijklmnop_-12");

        estimate.StopSharing();
        estimate.ShareToken.ShouldBeNull();
        Should.Throw<DomainException>(() => estimate.Share("short")).Code.ShouldBe("estimate.share_token_invalid");
        Should.Throw<DomainException>(() => estimate.Share("has spaces in the token")).Code.ShouldBe("estimate.share_token_invalid");
    }

    [Fact]
    public void Rooms_can_be_cleared_and_added_again()
    {
        var estimate = Estimate.Create(null, "x", null);
        estimate.AddRoom("A", RoomType.Bedroom, Bedroom, []);
        estimate.ClearRooms();
        estimate.Rooms.ShouldBeEmpty();
        estimate.AddRoom("B", RoomType.Kitchen, Bedroom, []).SortOrder.ShouldBe(1);
    }
}
