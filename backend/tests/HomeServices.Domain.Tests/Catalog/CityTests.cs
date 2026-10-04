using HomeServices.Domain.Catalog;
using HomeServices.Domain.Localization;

namespace HomeServices.Domain.Tests.Catalog;

public class CityTests
{
    private static LocalizedText Text(string hy) => LocalizedText.Empty.With("hy", hy);

    [Fact]
    public void A_new_city_is_active_and_has_no_districts()
    {
        var city = City.Create("Yerevan", Text("Երևան"), sortOrder: 1);

        city.Slug.ShouldBe("yerevan");
        city.Name.Get("hy", "hy").ShouldBe("Երևան");
        city.SortOrder.ShouldBe(1);
        city.IsActive.ShouldBeTrue();
        city.Districts.ShouldBeEmpty();
    }

    [Fact]
    public void Slug_must_be_valid_and_name_must_not_be_empty()
    {
        Should.Throw<DomainException>(() => City.Create("new york", Text("x"), 1)).Code.ShouldBe("city.slug_invalid");
        Should.Throw<DomainException>(() => City.Create("masis", LocalizedText.Empty, 1)).Code.ShouldBe("city.name_required");
    }

    [Fact]
    public void Districts_are_added_to_their_city()
    {
        var city = City.Create("yerevan", Text("Երևան"), 1);

        var kentron = city.AddDistrict("Kentron", Text("Կենտրոն"), sortOrder: 7);

        city.Districts.ShouldHaveSingleItem().ShouldBeSameAs(kentron);
        kentron.CityId.ShouldBe(city.Id);
        kentron.Slug.ShouldBe("kentron");
        kentron.SortOrder.ShouldBe(7);
        kentron.IsActive.ShouldBeTrue();
    }

    [Fact]
    public void District_slugs_are_unique_within_a_city()
    {
        var city = City.Create("yerevan", Text("Երևան"), 1);
        city.AddDistrict("kentron", Text("Կենտրոն"), 1);

        Should.Throw<DomainException>(() => city.AddDistrict("KENTRON", Text("Կենտրոն"), 2)).Code.ShouldBe("district.slug_taken");
    }

    [Fact]
    public void District_slug_and_name_are_validated()
    {
        var city = City.Create("yerevan", Text("Երևան"), 1);

        Should.Throw<DomainException>(() => city.AddDistrict("nor nork", Text("Նոր Նորք"), 1)).Code.ShouldBe("district.slug_invalid");
        Should.Throw<DomainException>(() => city.AddDistrict("nor-nork", LocalizedText.Empty, 1)).Code.ShouldBe("district.name_required");
    }

    [Fact]
    public void Cities_can_be_deactivated_and_reactivated()
    {
        var city = City.Create("masis", Text("Մասիս"), 5);

        city.Deactivate();
        city.IsActive.ShouldBeFalse();

        city.Activate();
        city.IsActive.ShouldBeTrue();
    }

    [Fact]
    public void A_city_can_be_updated()
    {
        var city = City.Create("masis", Text("Մասիս"), 5);

        city.Update(" Masis-City ", Text("Մասիս քաղաք"), 6);

        city.Slug.ShouldBe("masis-city");
        city.Name.Get("hy", "hy").ShouldBe("Մասիս քաղաք");
        city.SortOrder.ShouldBe(6);
    }

    [Fact]
    public void A_city_update_validates_slug_and_name()
    {
        var city = City.Create("masis", Text("Մասիս"), 5);

        Should.Throw<DomainException>(() => city.Update("bad slug", Text("x"), 1)).Code.ShouldBe("city.slug_invalid");
        Should.Throw<DomainException>(() => city.Update("masis", LocalizedText.Empty, 1)).Code.ShouldBe("city.name_required");
    }

    [Fact]
    public void A_district_can_be_updated_keeping_its_own_slug()
    {
        var city = City.Create("yerevan", Text("Երևան"), 1);
        var kentron = city.AddDistrict("kentron", Text("Կենտրոն"), 1);

        city.UpdateDistrict(kentron.Id, "kentron", Text("Կենտրոն թաղամաս"), 3);

        kentron.Name.Get("hy", "hy").ShouldBe("Կենտրոն թաղամաս");
        kentron.SortOrder.ShouldBe(3);
        city.HasDistrict(kentron.Id).ShouldBeTrue();
    }

    [Fact]
    public void A_district_cannot_take_another_districts_slug()
    {
        var city = City.Create("yerevan", Text("Երևան"), 1);
        city.AddDistrict("kentron", Text("Կենտրոն"), 1);
        var arabkir = city.AddDistrict("arabkir", Text("Արաբկիր"), 2);

        Should.Throw<DomainException>(() => city.UpdateDistrict(arabkir.Id, "Kentron", Text("x"), 2)).Code.ShouldBe("district.slug_taken");
        Should.Throw<DomainException>(() => city.UpdateDistrict(arabkir.Id, "bad slug", Text("x"), 2)).Code.ShouldBe("district.slug_invalid");
        Should.Throw<DomainException>(() => city.UpdateDistrict(arabkir.Id, "arabkir", LocalizedText.Empty, 2)).Code.ShouldBe("district.name_required");
    }

    [Fact]
    public void Districts_can_be_deactivated_and_reactivated()
    {
        var city = City.Create("yerevan", Text("Երևան"), 1);
        var kentron = city.AddDistrict("kentron", Text("Կենտրոն"), 1);

        city.SetDistrictActive(kentron.Id, false).IsActive.ShouldBeFalse();
        city.SetDistrictActive(kentron.Id, true).IsActive.ShouldBeTrue();
    }

    [Fact]
    public void Changing_a_district_of_another_city_fails()
    {
        var city = City.Create("yerevan", Text("Երևան"), 1);
        var unknown = Guid.NewGuid();

        city.HasDistrict(unknown).ShouldBeFalse();
        Should.Throw<DomainException>(() => city.SetDistrictActive(unknown, false)).Code.ShouldBe("district.not_found");
    }
}
