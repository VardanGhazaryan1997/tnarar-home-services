using HomeServices.Application.Demo;
using HomeServices.Application.Tests.Support;
using HomeServices.Domain.Catalog;
using HomeServices.Domain.Files;
using HomeServices.Domain.Identity;
using HomeServices.Domain.Localization;
using HomeServices.Domain.Partners;
using Microsoft.EntityFrameworkCore;

namespace HomeServices.Application.Tests.Demo;

public class SeedDemoPartnersTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 5, 9, 0, 0, TimeSpan.Zero);
    private readonly InMemoryAppDbContext _db = InMemoryAppDbContext.Create();
    private readonly FakeFileStorage _storage = new();

    public SeedDemoPartnersTests()
    {
        var text = LocalizedText.Empty.With("hy", "x");
        _db.Categories.AddRange(Category.Create("plumbing", text, 1), Category.Create("cleaning", text, 2));
        var yerevanRegion = Region.Create("yerevan", text, 1);
        var yerevan = City.Create("yerevan", text, 1, yerevanRegion.Id);
        yerevan.AddDistrict("kentron", text, 1);
        _db.Regions.Add(yerevanRegion);
        _db.Cities.Add(yerevan);
        _db.SaveChanges();
    }

    private Task<int> SeedAsync() =>
        new SeedDemoPartnersHandler(_db, _storage, new FakeClock(Now)).HandleAsync(new SeedDemoPartners(), CancellationToken.None);

    [Fact]
    public async Task Demo_partners_that_fit_the_catalog_are_created_approved_with_pictures()
    {
        var created = await SeedAsync();

        created.ShouldBeGreaterThan(0);
        _db.ChangeTracker.Clear();
        var profiles = await _db.PartnerProfiles.Include(p => p.Services).Include(p => p.Areas).Include(p => p.Media).ToListAsync();
        profiles.Count.ShouldBe(created);
        profiles.ShouldAllBe(p => p.Status == PartnerStatus.Approved && p.Slug != null && p.Services.Count > 0 && p.Areas.Count > 0);
        profiles.ShouldAllBe(p => p.Media.Count(m => m.Kind == PartnerMediaKind.WorkExample) == 3);
        profiles.ShouldContain(p => p.DisplayName == "Արամ Սանտեխնիկ");

        var files = await _db.Files.ToListAsync();
        files.Count.ShouldBe(created * 3);
        files.ShouldAllBe(f => f.Status == FileStatus.Ready && f.ContentType == "image/png" && _storage.Objects.ContainsKey(f.Key));

        var owners = await _db.Users.Where(u => profiles.Select(p => p.UserId).Contains(u.Id)).ToListAsync();
        owners.ShouldAllBe(u => u.HasRole(UserRoles.Partner) && u.Phone.Value.StartsWith("+37499000", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Seeding_again_does_nothing()
    {
        var first = await SeedAsync();

        (await SeedAsync()).ShouldBe(0);
        (await _db.PartnerProfiles.CountAsync()).ShouldBe(first);
    }

    [Fact]
    public void Demo_pictures_are_valid_png_files()
    {
        var png = DemoImage.Png(seed: 7, width: 32, height: 24);

        png.Take(8).ShouldBe(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A });
        // IHDR holds the size, big-endian.
        png.Skip(16).Take(8).ShouldBe(new byte[] { 0, 0, 0, 32, 0, 0, 0, 24 });
        DemoImage.Png(seed: 7, width: 32, height: 24).ShouldBe(png);
    }
}
