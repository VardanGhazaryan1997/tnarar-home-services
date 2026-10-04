using HomeServices.Application.Files;
using HomeServices.Application.Partners;
using HomeServices.Domain.Catalog;
using HomeServices.Domain.Files;
using HomeServices.Domain.Identity;
using HomeServices.Domain.Localization;
using HomeServices.Domain.Partners;
using HomeServices.Infrastructure.IntegrationTests.Fakes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Npgsql;

namespace HomeServices.Infrastructure.IntegrationTests.Partners;

[Collection(PostgresCollection.Name)]
public class PartnerStorageTests(PostgresFixture db)
{
    private static LocalizedText Text(string hy) => LocalizedText.Empty.With("hy", hy);

    private static string UniqueSlug(string prefix) => $"{prefix}-{Guid.NewGuid():N}"[..30];

    private async Task<(User User, Category Category, City City, StoredFile Photo)> GivenCatalogAndUserAsync()
    {
        var user = User.Register(PhoneNumber.Parse($"+37491{Random.Shared.Next(100_000, 999_999)}"));
        var category = Category.Create(UniqueSlug("cat"), Text("Ծառայություն"), 1);
        var city = City.Create(UniqueSlug("city"), Text("Քաղաք"), 1);
        city.AddDistrict("center", Text("Կենտրոն"), 1);
        var photo = StoredFile.Begin(FileOwnerType.User, user.Id.ToString(), "kitchen.jpg", "image/jpeg", 100, db.Clock.Now);
        photo.MarkReady(100, db.Clock.Now);

        await using var context = db.CreateContext();
        context.AddRange(user, category, city, photo);
        await context.SaveChangesAsync();
        return (user, category, city, photo);
    }

    [Fact]
    public async Task A_profile_is_saved_with_its_services_areas_media_and_history()
    {
        var (user, category, city, photo) = await GivenCatalogAndUserAsync();
        var profile = PartnerProfile.Create(user.Id, PartnerType.Specialist, "Aram Plumbing");
        profile.UpdateDetails(PartnerType.Specialist, "Aram Plumbing", new string('a', 60), 10, photo.Id);
        profile.SetServices([category.Id]);
        profile.SetAreas([(city.Id, null)]);
        profile.AddMedia(PartnerMediaKind.WorkExample, photo.Id, "Kitchen");
        profile.Submit(db.Clock.Now);

        await using (var context = db.CreateContext())
        {
            context.PartnerProfiles.Add(profile);
            await context.SaveChangesAsync();
        }

        await using (var context = db.CreateContext())
        {
            var saved = await context.PartnerProfiles
                .Include(p => p.Services).Include(p => p.Areas).Include(p => p.Media).Include(p => p.StatusChanges)
                .SingleAsync(p => p.Id == profile.Id);

            saved.Status.ShouldBe(PartnerStatus.UnderReview);
            saved.AvatarFileId.ShouldBe(photo.Id);
            saved.Services.ShouldHaveSingleItem().CategoryId.ShouldBe(category.Id);
            saved.Areas.ShouldHaveSingleItem().DistrictId.ShouldBeNull();
            saved.Media.ShouldHaveSingleItem().Caption.ShouldBe("Kitchen");
            var change = saved.StatusChanges.ShouldHaveSingleItem();
            change.ToStatus.ShouldBe(PartnerStatus.UnderReview);
            change.CreatedAt.ShouldBe(db.Clock.Now);
        }
    }

    [Fact]
    public async Task Changing_services_and_areas_updates_the_rows()
    {
        var (user, category, city, _) = await GivenCatalogAndUserAsync();
        var district = city.Districts.Single();
        var profile = PartnerProfile.Create(user.Id, PartnerType.Company, "Best Build");
        profile.SetServices([category.Id]);
        profile.SetAreas([(city.Id, null)]);
        await using (var context = db.CreateContext())
        {
            context.PartnerProfiles.Add(profile);
            await context.SaveChangesAsync();
        }

        await using (var context = db.CreateContext())
        {
            var loaded = await context.PartnerProfiles.Include(p => p.Services).Include(p => p.Areas).SingleAsync(p => p.Id == profile.Id);
            loaded.SetServices([]);
            loaded.SetAreas([(city.Id, district.Id)]);
            await context.SaveChangesAsync();
        }

        await using (var context = db.CreateContext())
        {
            var saved = await context.PartnerProfiles.Include(p => p.Services).Include(p => p.Areas).SingleAsync(p => p.Id == profile.Id);
            saved.Services.ShouldBeEmpty();
            saved.Areas.ShouldHaveSingleItem().DistrictId.ShouldBe(district.Id);
        }
    }

    [Fact]
    public async Task A_user_has_at_most_one_profile_and_a_whole_city_is_listed_once()
    {
        var (user, _, city, _) = await GivenCatalogAndUserAsync();
        var profile = PartnerProfile.Create(user.Id, PartnerType.Specialist, "Aram");
        profile.SetAreas([(city.Id, null)]);
        await using (var context = db.CreateContext())
        {
            context.PartnerProfiles.Add(profile);
            await context.SaveChangesAsync();
        }

        await using (var context = db.CreateContext())
        {
            context.PartnerProfiles.Add(PartnerProfile.Create(user.Id, PartnerType.Company, "Aram Again"));
            var duplicateProfile = await Should.ThrowAsync<DbUpdateException>(() => context.SaveChangesAsync());
            duplicateProfile.InnerException.ShouldBeOfType<PostgresException>().SqlState.ShouldBe(PostgresErrorCodes.UniqueViolation);
        }

        await using (var context = db.CreateContext())
        {
            var duplicateArea = await Should.ThrowAsync<PostgresException>(() => context.Database.ExecuteSqlRawAsync(
                "INSERT INTO partner_areas (id, partner_profile_id, city_id, district_id) VALUES ({0}, {1}, {2}, NULL)",
                Guid.CreateVersion7(),
                profile.Id,
                city.Id));
            duplicateArea.SqlState.ShouldBe(PostgresErrorCodes.UniqueViolation);
        }
    }

    [Fact]
    public async Task The_admin_list_query_runs_on_PostgreSQL()
    {
        var (user, category, city, _) = await GivenCatalogAndUserAsync();
        var name = $"Searchable {Guid.NewGuid():N}";
        var profile = PartnerProfile.Create(user.Id, PartnerType.Company, name);
        profile.UpdateDetails(PartnerType.Company, name, new string('a', 60), null, null);
        profile.SetServices([category.Id]);
        profile.SetAreas([(city.Id, null), (city.Id, city.Districts.Single().Id)]);
        await using (var context = db.CreateContext())
        {
            context.PartnerProfiles.Add(profile);
            await context.SaveChangesAsync();
        }

        await using (var context = db.CreateContext())
        {
            var handler = new GetAdminPartnersHandler(context);

            var byName = await handler.HandleAsync(new GetAdminPartners(PartnerStatus.Draft, PartnerType.Company, name.ToUpperInvariant()), CancellationToken.None);
            var byPhone = await handler.HandleAsync(new GetAdminPartners(Search: user.Phone.Value), CancellationToken.None);

            var item = byName.Items.ShouldHaveSingleItem();
            item.Id.ShouldBe(profile.Id);
            item.ServiceCount.ShouldBe(1);
            item.AreaCount.ShouldBe(1);
            item.Phone.ShouldBe(user.Phone.Value);
            byPhone.Items.ShouldHaveSingleItem().Id.ShouldBe(profile.Id);
        }
    }

    [Fact]
    public async Task The_public_search_runs_on_PostgreSQL()
    {
        var (user, category, city, photo) = await GivenCatalogAndUserAsync();
        var district = city.Districts.Single();
        var profile = PartnerProfile.Create(user.Id, PartnerType.Specialist, "Արամ Սանտեխնիկ");
        profile.UpdateDetails(PartnerType.Specialist, "Արամ Սանտեխնիկ", "Pipes, boilers and bathrooms. " + new string('a', 40), 9, photo.Id);
        profile.SetServices([category.Id]);
        profile.SetAreas([(city.Id, null)]);
        profile.AddMedia(PartnerMediaKind.WorkExample, photo.Id, "Kitchen");
        profile.Submit(db.Clock.Now);
        profile.Approve(db.Clock.Now);
        await using (var context = db.CreateContext())
        {
            context.PartnerProfiles.Add(profile);
            await context.SaveChangesAsync();
        }

        await using (var context = db.CreateContext())
        {
            var files = new FileDtoFactory(new FakeFileStorage(), Options.Create(new FileSettings()), db.Clock);
            var search = new SearchPartnersHandler(context, new FakeCurrentLanguage(), files);

            var page = await search.HandleAsync(
                new SearchPartners(category.Slug, city.Slug, district.Slug, PartnerType.Specialist, "BOILERS"),
                CancellationToken.None);
            var detail = await new GetPublicPartnerHandler(context, new FakeCurrentLanguage(), files)
                .HandleAsync(new GetPublicPartner(profile.Slug!), CancellationToken.None);

            var card = page.Items.ShouldHaveSingleItem();
            card.Slug.ShouldStartWith("aram-santekhnik-");
            card.Cover!.Caption.ShouldBe("Kitchen");
            card.Avatar.ShouldNotBeNull();
            detail.Areas.ShouldHaveSingleItem().CitySlug.ShouldBe(city.Slug);
            detail.WorkExamples.ShouldHaveSingleItem().Url.ShouldContain(photo.Key);
        }
    }
}
