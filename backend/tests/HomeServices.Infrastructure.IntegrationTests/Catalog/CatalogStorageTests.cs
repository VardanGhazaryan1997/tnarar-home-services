using HomeServices.Domain.Catalog;
using HomeServices.Domain.Localization;
using HomeServices.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HomeServices.Infrastructure.IntegrationTests.Catalog;

[Collection(PostgresCollection.Name)]
public class CatalogStorageTests(PostgresFixture db)
{
    private static LocalizedText Text(string hy) => LocalizedText.Empty.With("hy", hy);

    [Fact]
    public async Task Migrations_seed_the_main_categories_and_their_subcategories()
    {
        await using var context = await MigratedContext("catalog_categories");

        var all = await context.Categories.ToListAsync();
        var main = all.Where(c => c.ParentId == null).OrderBy(c => c.SortOrder).ToList();

        main.Count.ShouldBe(18);
        main.Take(7).Select(c => c.Slug).ShouldBe(new[]
        {
            "construction", "renovation", "plumbing", "heating", "electrical", "exterior-cladding", "cleaning",
        });
        all.Count.ShouldBe(18 + 133);
        all.ShouldAllBe(c => c.IsActive);
        all.Where(c => c.ParentId != null).ShouldAllBe(c => main.Any(m => m.Id == c.ParentId));
        main.ShouldAllBe(m => all.Any(c => c.ParentId == m.Id));
        all.Single(c => c.Slug == "plumbing").Name.Get("ru", "hy").ShouldBe("Сантехника");
        all.Single(c => c.Slug == "cleaning-after-renovation").Name.Get("en", "hy").ShouldBe("Post-renovation cleaning");
    }

    [Fact]
    public async Task Migrations_seed_the_five_launch_cities_and_Yerevans_districts()
    {
        await using var context = await MigratedContext("catalog_cities");

        var cities = await context.Cities.Include(c => c.Districts).OrderBy(c => c.SortOrder).ToListAsync();

        cities.Select(c => c.Slug).ShouldBe(new[] { "yerevan", "ejmiatsin", "abovyan", "ashtarak", "masis" });
        cities[0].Districts.Count.ShouldBe(12);
        cities[0].Districts.ShouldContain(d => d.Slug == "kentron" && d.Name.Get("hy", "hy") == "Կենտրոն");
        cities.Skip(1).ShouldAllBe(c => c.Districts.Count == 0);
    }

    [Fact]
    public async Task A_city_and_its_new_districts_are_saved_together()
    {
        var city = City.Create("gyumri", Text("Գյումրի"), 9);
        city.AddDistrict("center", Text("Կենտրոն"), 1);

        await using (var context = db.CreateContext())
        {
            context.Cities.Add(city);
            await context.SaveChangesAsync();
        }

        await using var check = db.CreateContext();
        var saved = await check.Cities.Include(c => c.Districts).SingleAsync(c => c.Id == city.Id);
        saved.Districts.ShouldHaveSingleItem().Slug.ShouldBe("center");
    }

    [Fact]
    public async Task Category_slugs_are_unique()
    {
        await using var context = db.CreateContext();
        context.Categories.Add(Category.Create("plumbing", Text("Կրկնօրինակ"), 99));

        await Should.ThrowAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }

    [Fact]
    public async Task A_sub_category_must_point_to_an_existing_parent()
    {
        await using var context = db.CreateContext();
        context.Categories.Add(Category.Create("orphan", Text("Որբ"), 1, parentId: Guid.NewGuid()));

        await Should.ThrowAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }

    [Fact]
    public async Task A_deleted_categorys_slug_can_be_used_again()
    {
        var slug = $"temporary-{Guid.NewGuid():N}";
        var first = Category.Create(slug, Text("Ժամանակավոր"), 1);
        await using (var context = db.CreateContext())
        {
            context.Categories.Add(first);
            await context.SaveChangesAsync();
            context.Categories.Remove(first);
            await context.SaveChangesAsync();
        }

        var second = Category.Create(slug, Text("Նոր"), 1);
        await using (var context = db.CreateContext())
        {
            context.Categories.Add(second);
            await context.SaveChangesAsync();
        }

        await using var check = db.CreateContext();
        (await check.Categories.SingleAsync(c => c.Slug == slug)).Id.ShouldBe(second.Id);
        (await check.Categories.IgnoreQueryFilters().CountAsync(c => c.Slug == slug)).ShouldBe(2);
    }

    [Fact]
    public async Task A_district_added_to_an_existing_city_is_saved()
    {
        var city = City.Create($"city-{Guid.NewGuid():N}", Text("Քաղաք"), 9);
        await using (var context = db.CreateContext())
        {
            context.Cities.Add(city);
            await context.SaveChangesAsync();
        }

        await using (var context = db.CreateContext())
        {
            var loaded = await context.Cities.Include(c => c.Districts).SingleAsync(c => c.Id == city.Id);
            context.Districts.Add(loaded.AddDistrict("center", Text("Կենտրոն"), 1));
            await context.SaveChangesAsync();
        }

        await using var check = db.CreateContext();
        (await check.Districts.SingleAsync(d => d.CityId == city.Id)).Slug.ShouldBe("center");
    }

    private async Task<AppDbContext> MigratedContext(string database)
    {
        var context = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseNpgsql(db.ConnectionStringFor(database)).Options);
        await context.Database.MigrateAsync();
        return context;
    }
}
