using HomeServices.Domain.Estimates;
using HomeServices.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HomeServices.Infrastructure.IntegrationTests.Estimates;

[Collection(PostgresCollection.Name)]
public class EstimateStorageTests(PostgresFixture db)
{
    [Fact]
    public async Task An_estimate_is_stored_with_its_rooms_openings_and_lines()
    {
        await using var context = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseNpgsql(db.ConnectionStringFor("estimates")).Options);
        await context.Database.MigrateAsync();
        var workItem = await context.WorkItems.FirstAsync();

        var estimate = Estimate.Create(null, "Flat in Arabkir", null);
        var room = estimate.AddRoom("Bedroom", RoomType.Bedroom, new RoomSize(4.25m, 3.1m, null, 2.75m), [new RoomOpening(OpeningKind.Window, 1.5m, 1.4m, 2)]);
        room.AddLine(workItem.Id, 12.5m);
        estimate.AddRoom("Bath", RoomType.Bathroom, new RoomSize(null, null, 4.4m, 2.5m), []);
        context.Estimates.Add(estimate);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var stored = await context.Estimates
            .Include(e => e.Rooms).ThenInclude(r => r.Openings)
            .Include(e => e.Rooms).ThenInclude(r => r.Lines)
            .SingleAsync(e => e.Id == estimate.Id);

        stored.Title.ShouldBe("Flat in Arabkir");
        var bedroom = stored.Rooms.Single(r => r.Type == RoomType.Bedroom);
        (bedroom.Length, bedroom.Width, bedroom.Height).ShouldBe(((decimal?)4.25m, (decimal?)3.1m, 2.75m));
        bedroom.Openings.Single().Count.ShouldBe(2);
        bedroom.Lines.Single().Quantity.ShouldBe(12.5m);
        stored.Rooms.Single(r => r.Type == RoomType.Bathroom).Area.ShouldBe(4.4m);

        stored.RemoveRoom(bedroom.Id);
        await context.SaveChangesAsync();
        (await context.Set<EstimateLine>().CountAsync(l => l.RoomId == bedroom.Id)).ShouldBe(0);
    }

    [Fact]
    public async Task Rooms_are_replaced_and_the_share_token_is_unique()
    {
        await using var context = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseNpgsql(db.ConnectionStringFor("estimates")).Options);
        await context.Database.MigrateAsync();
        var workItem = await context.WorkItems.FirstAsync();
        var estimate = Estimate.Create(null, "Replace me", null);
        estimate.AddRoom("Old", RoomType.Bedroom, new RoomSize(4m, 3m, null, 2.7m), [new RoomOpening(OpeningKind.Door, 0.9m, 2m)]).AddLine(workItem.Id, null);
        estimate.Share($"token-{Guid.NewGuid():N}"[..24]);
        context.Estimates.Add(estimate);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var stored = await context.Estimates
            .Include(e => e.Rooms).ThenInclude(r => r.Openings)
            .Include(e => e.Rooms).ThenInclude(r => r.Lines)
            .SingleAsync(e => e.Id == estimate.Id);
        var oldRoomId = stored.Rooms.Single().Id;
        stored.ClearRooms();
        stored.AddRoom("New", RoomType.Kitchen, new RoomSize(null, null, 9m, 2.7m), []).AddLine(workItem.Id, 2m);
        stored.SetOldBuilding(true);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var reloaded = await context.Estimates.Include(e => e.Rooms).ThenInclude(r => r.Lines).SingleAsync(e => e.Id == estimate.Id);
        reloaded.OldBuilding.ShouldBeTrue();
        reloaded.Rooms.Single().Name.ShouldBe("New");
        reloaded.Rooms.Single().Lines.Single().Quantity.ShouldBe(2m);
        (await context.Set<EstimateRoom>().AnyAsync(r => r.Id == oldRoomId)).ShouldBeFalse();
        (await context.Set<EstimateOpening>().AnyAsync(o => o.RoomId == oldRoomId)).ShouldBeFalse();

        var copy = Estimate.Create(null, "Copy", null);
        copy.Share(estimate.ShareToken!);
        context.Estimates.Add(copy);
        await Should.ThrowAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }
}
