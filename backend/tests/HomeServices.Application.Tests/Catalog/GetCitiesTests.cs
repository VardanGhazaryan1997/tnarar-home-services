using HomeServices.Application.Catalog;
using HomeServices.Application.Tests.Support;
using HomeServices.Domain.Catalog;
using HomeServices.Domain.Localization;

namespace HomeServices.Application.Tests.Catalog;

public class GetCitiesTests
{
    private static LocalizedText Text(string hy, string en) => LocalizedText.Empty.With("hy", hy).With("en", en);

    [Fact]
    public async Task Returns_active_cities_with_their_districts_in_display_order()
    {
        await using var db = InMemoryAppDbContext.Create();
        var yerevan = City.Create("yerevan", Text("Երևան", "Yerevan"), 1);
        yerevan.AddDistrict("kentron", Text("Կենտրոն", "Kentron"), 2);
        yerevan.AddDistrict("arabkir", Text("Արաբկիր", "Arabkir"), 1);
        var masis = City.Create("masis", Text("Մասիս", "Masis"), 2);
        var closed = City.Create("closed", Text("x", "Closed"), 3);
        closed.Deactivate();
        db.Cities.AddRange(masis, yerevan, closed);
        await db.SaveChangesAsync();

        var result = await new GetCitiesHandler(db, new FakeCurrentLanguage("en"))
            .HandleAsync(new GetCities(), CancellationToken.None);

        result.Select(c => c.Name).ShouldBe(new[] { "Yerevan", "Masis" });
        result[0].Districts.Select(d => d.Name).ShouldBe(new[] { "Arabkir", "Kentron" });
        result[0].Districts[0].Slug.ShouldBe("arabkir");
        result[1].Districts.ShouldBeEmpty();
    }
}
