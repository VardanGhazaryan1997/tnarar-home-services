using HomeServices.Domain.Files;
using Microsoft.EntityFrameworkCore;

namespace HomeServices.Infrastructure.IntegrationTests.Files;

[Collection(PostgresCollection.Name)]
public class StoredFileStorageTests(PostgresFixture db)
{
    [Fact]
    public async Task A_file_is_saved_and_read_back_with_readable_enum_values()
    {
        var file = StoredFile.Begin(FileOwnerType.User, Guid.CreateVersion7().ToString(), "kitchen.jpg", "image/jpeg", 2048, db.Clock.Now);
        file.MarkReady(2000, db.Clock.Now, file.ThumbnailKeyFor(), 4000, 3000);

        await using (var context = db.CreateContext())
        {
            context.Files.Add(file);
            await context.SaveChangesAsync();
        }

        await using (var context = db.CreateContext())
        {
            var saved = await context.Files.SingleAsync(f => f.Id == file.Id);
            saved.OwnerType.ShouldBe(FileOwnerType.User);
            saved.Kind.ShouldBe(FileKind.Image);
            saved.Status.ShouldBe(FileStatus.Ready);
            saved.Key.ShouldBe(file.Key);
            saved.ThumbnailKey.ShouldBe(file.ThumbnailKeyFor());
            saved.Size.ShouldBe(2000);
            saved.Width.ShouldBe(4000);
            saved.CreatedAt.ShouldBe(db.Clock.Now);

            var stored = await context.Database
                .SqlQueryRaw<string>("SELECT owner_type || ' ' || kind || ' ' || status AS \"Value\" FROM files WHERE id = {0}", file.Id)
                .SingleAsync();
            stored.ShouldBe("User Image Ready");
        }
    }
}
