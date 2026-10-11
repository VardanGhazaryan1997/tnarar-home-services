using HomeServices.Domain.Localization;
using HomeServices.Domain.Stores;

namespace HomeServices.Domain.Tests.Stores;

public class StoreTests
{
    private static readonly LocalizedText Description = LocalizedText.Empty.With("en", "Building materials");

    private static Store NewStore() => Store.Create(" Demo-Market ", " Demo Market ", Description, "https://demo.example", " +374 10 000000 ", null, 1);

    private static BranchInput Branch(string name = "Komitas", string address = "Komitas 1") =>
        new(name, Guid.NewGuid(), null, address, null, " Mon–Sat 9:00–19:00 ");

    [Fact]
    public void A_store_is_created_active_with_cleaned_fields()
    {
        var store = NewStore();

        (store.Slug, store.Name, store.Phone, store.Website, store.IsActive).ShouldBe(("demo-market", "Demo Market", "+374 10 000000", "https://demo.example", true));
        store.Deactivate();
        store.IsActive.ShouldBeFalse();
        store.Activate();
        store.IsActive.ShouldBeTrue();
    }

    [Fact]
    public void Blank_optional_fields_become_empty()
    {
        var store = Store.Create("shop", "Shop", LocalizedText.Empty, "  ", "", null, 1);

        store.Website.ShouldBeNull();
        store.Phone.ShouldBeNull();
    }

    [Fact]
    public void A_store_needs_a_name_and_a_slug()
    {
        Should.Throw<DomainException>(() => Store.Create("shop", "  ", Description, null, null, null, 1)).Code.ShouldBe("store.name_required");
        Should.Throw<DomainException>(() => Store.Create("Not a slug", "Shop", Description, null, null, null, 1)).Code.ShouldBe("store.slug_invalid");
        Should.Throw<DomainException>(() => Store.Create("shop", new string('x', Store.MaxNameLength + 1), Description, null, null, null, 1)).Code.ShouldBe("store.invalid");
        Should.Throw<DomainException>(() => Store.Create("shop", "Shop", Description, null, new string('1', Store.MaxPhoneLength + 1), null, 1)).Code.ShouldBe("store.invalid");
    }

    [Theory]
    [InlineData("demo.example")]
    [InlineData("ftp://demo.example")]
    [InlineData("javascript:alert(1)")]
    public void A_website_is_an_http_or_https_address(string website) =>
        Should.Throw<DomainException>(() => Store.Create("shop", "Shop", Description, website, null, null, 1)).Code.ShouldBe("store.website_invalid");

    [Fact]
    public void Branches_are_replaced_as_a_whole_and_numbered()
    {
        var store = NewStore();
        store.SetBranches(new[] { Branch("A"), Branch("B") });

        store.Branches.Select(b => $"{b.Name}:{b.SortOrder}").ShouldBe(new[] { "A:1", "B:2" });
        store.Branches.ShouldAllBe(b => b.StoreId == store.Id);
        store.Branches.First().OpeningHours.ShouldBe("Mon–Sat 9:00–19:00");

        store.SetBranches(new[] { Branch("C") });
        store.Branches.Select(b => b.Name).ShouldBe(new[] { "C" });
        store.SetBranches(Array.Empty<BranchInput>());
        store.Branches.ShouldBeEmpty();
    }

    [Fact]
    public void A_bad_branch_leaves_the_old_list_untouched()
    {
        var store = NewStore();
        store.SetBranches(new[] { Branch("A") });

        Should.Throw<DomainException>(() => store.SetBranches(new[] { Branch("B"), Branch(address: " ") })).Code.ShouldBe("store_branch.invalid");
        Should.Throw<DomainException>(() => store.SetBranches(new[] { new BranchInput("B", Guid.Empty, null, "Address", null, null) })).Code.ShouldBe("store_branch.invalid");

        store.Branches.Select(b => b.Name).ShouldBe(new[] { "A" });
    }

    [Fact]
    public void A_store_has_a_limited_number_of_branches()
    {
        var store = NewStore();

        store.SetBranches(Enumerable.Range(1, Store.MaxBranches).Select(i => Branch($"B{i}")));
        Should.Throw<DomainException>(() => store.SetBranches(Enumerable.Range(1, Store.MaxBranches + 1).Select(i => Branch($"B{i}")))).Code.ShouldBe("store.too_many_branches");
        store.Branches.Count.ShouldBe(Store.MaxBranches);
    }
}
