using HomeServices.Application.Catalog;
using HomeServices.Application.Errors;
using HomeServices.Application.Tests.Support;
using HomeServices.Domain.Catalog;
using HomeServices.Domain.Localization;

namespace HomeServices.Application.Tests.Catalog;

public class AdminCitiesTests
{
    private static readonly Dictionary<string, string> MasisName = new() { ["hy"] = "Մասիս", ["ru"] = "Масис" };

    private readonly InMemoryAppDbContext _db = InMemoryAppDbContext.Create();

    private static LocalizedText Text(string hy) => LocalizedText.Empty.With("hy", hy);

    private async Task<City> GivenCity(string slug, params string[] districts)
    {
        var city = City.Create(slug, Text(slug), 1);
        foreach (var district in districts)
        {
            city.AddDistrict(district, Text(district), 1);
        }

        _db.Cities.Add(city);
        await _db.SaveChangesAsync();
        return city;
    }

    [Fact]
    public async Task The_admin_list_includes_inactive_cities_and_districts_in_order()
    {
        var yerevan = await GivenCity("yerevan", "kentron");
        yerevan.SetDistrictActive(yerevan.Districts.Single().Id, false);
        var masis = await GivenCity("masis");
        masis.Deactivate();
        masis.Update("masis", Text("Մասիս"), 0);
        await _db.SaveChangesAsync();

        var cities = await new GetAdminCitiesHandler(_db).HandleAsync(new GetAdminCities(), CancellationToken.None);

        cities.Select(c => c.Slug).ShouldBe(new[] { "masis", "yerevan" });
        cities[0].IsActive.ShouldBeFalse();
        var kentron = cities[1].Districts.ShouldHaveSingleItem();
        kentron.IsActive.ShouldBeFalse();
        kentron.CityId.ShouldBe(yerevan.Id);
    }

    [Fact]
    public async Task Creates_a_city()
    {
        var city = await new CreateCityHandler(_db).HandleAsync(new CreateCity(" Masis ", MasisName, 5), CancellationToken.None);

        city.Slug.ShouldBe("masis");
        city.Name.ShouldBe(MasisName);
        city.SortOrder.ShouldBe(5);
        city.IsActive.ShouldBeTrue();
        city.Districts.ShouldBeEmpty();
    }

    [Fact]
    public async Task City_slugs_must_be_unique()
    {
        await GivenCity("masis");

        (await Should.ThrowAsync<ConflictException>(() =>
                new CreateCityHandler(_db).HandleAsync(new CreateCity("masis", MasisName, 1), CancellationToken.None)))
            .Code.ShouldBe("city.slug_taken");
    }

    [Fact]
    public async Task Updates_a_city()
    {
        var city = await GivenCity("masis");

        var updated = await new UpdateCityHandler(_db).HandleAsync(new UpdateCity(city.Id, "masis-city", MasisName, 9), CancellationToken.None);

        updated.Slug.ShouldBe("masis-city");
        updated.Name.ShouldBe(MasisName);
        updated.SortOrder.ShouldBe(9);
    }

    [Fact]
    public async Task A_city_cannot_take_another_citys_slug()
    {
        await GivenCity("masis");
        var abovyan = await GivenCity("abovyan");

        (await Should.ThrowAsync<ConflictException>(() =>
                new UpdateCityHandler(_db).HandleAsync(new UpdateCity(abovyan.Id, "masis", MasisName, 1), CancellationToken.None)))
            .Code.ShouldBe("city.slug_taken");
    }

    [Fact]
    public async Task Cities_can_be_hidden_and_shown_again()
    {
        var city = await GivenCity("masis");
        var handler = new SetCityActiveHandler(_db);

        (await handler.HandleAsync(new SetCityActive(city.Id, false), CancellationToken.None)).IsActive.ShouldBeFalse();
        (await handler.HandleAsync(new SetCityActive(city.Id, true), CancellationToken.None)).IsActive.ShouldBeTrue();
    }

    [Fact]
    public async Task Unknown_cities_are_not_found()
    {
        (await Should.ThrowAsync<NotFoundException>(() =>
                new SetCityActiveHandler(_db).HandleAsync(new SetCityActive(Guid.NewGuid(), false), CancellationToken.None)))
            .Code.ShouldBe("city.not_found");
    }

    [Fact]
    public async Task Adds_a_district_to_a_city()
    {
        var city = await GivenCity("yerevan");

        var district = await new AddDistrictHandler(_db).HandleAsync(
            new AddDistrict(city.Id, "Nor-Nork", new Dictionary<string, string> { ["hy"] = "Նոր Նորք" }, 8), CancellationToken.None);

        district.Slug.ShouldBe("nor-nork");
        district.CityId.ShouldBe(city.Id);
        district.SortOrder.ShouldBe(8);
        _db.Districts.Single(d => d.Id == district.Id).Name.Get("hy", "hy").ShouldBe("Նոր Նորք");
    }

    [Fact]
    public async Task District_slugs_are_unique_within_the_city()
    {
        var city = await GivenCity("yerevan", "kentron");

        (await Should.ThrowAsync<ConflictException>(() => new AddDistrictHandler(_db).HandleAsync(
                new AddDistrict(city.Id, "kentron", MasisName, 1), CancellationToken.None)))
            .Code.ShouldBe("district.slug_taken");
    }

    [Fact]
    public async Task Updates_a_district()
    {
        var city = await GivenCity("yerevan", "kentron");
        var kentron = city.Districts.Single();

        var updated = await new UpdateDistrictHandler(_db).HandleAsync(
            new UpdateDistrict(city.Id, kentron.Id, "center", MasisName, 4), CancellationToken.None);

        updated.Slug.ShouldBe("center");
        updated.Name.ShouldBe(MasisName);
        updated.SortOrder.ShouldBe(4);
    }

    [Fact]
    public async Task A_district_cannot_take_another_districts_slug()
    {
        var city = await GivenCity("yerevan", "kentron", "arabkir");
        var arabkir = city.Districts.Single(d => d.Slug == "arabkir");

        (await Should.ThrowAsync<ConflictException>(() => new UpdateDistrictHandler(_db).HandleAsync(
                new UpdateDistrict(city.Id, arabkir.Id, "kentron", MasisName, 1), CancellationToken.None)))
            .Code.ShouldBe("district.slug_taken");
    }

    [Fact]
    public async Task Districts_can_be_hidden_and_shown_again()
    {
        var city = await GivenCity("yerevan", "kentron");
        var kentron = city.Districts.Single();
        var handler = new SetDistrictActiveHandler(_db);

        (await handler.HandleAsync(new SetDistrictActive(city.Id, kentron.Id, false), CancellationToken.None)).IsActive.ShouldBeFalse();
        (await handler.HandleAsync(new SetDistrictActive(city.Id, kentron.Id, true), CancellationToken.None)).IsActive.ShouldBeTrue();
    }

    [Fact]
    public async Task Districts_of_other_cities_are_not_found()
    {
        var yerevan = await GivenCity("yerevan");
        var masis = await GivenCity("masis", "center");
        var center = masis.Districts.Single();

        (await Should.ThrowAsync<NotFoundException>(() => new SetDistrictActiveHandler(_db).HandleAsync(
                new SetDistrictActive(yerevan.Id, center.Id, false), CancellationToken.None)))
            .Code.ShouldBe("district.not_found");
        (await Should.ThrowAsync<NotFoundException>(() => new UpdateDistrictHandler(_db).HandleAsync(
                new UpdateDistrict(yerevan.Id, center.Id, "center", MasisName, 1), CancellationToken.None)))
            .Code.ShouldBe("district.not_found");
    }
}
