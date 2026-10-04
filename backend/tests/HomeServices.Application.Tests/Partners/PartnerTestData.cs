using HomeServices.Application.Abstractions;
using HomeServices.Application.Files;
using HomeServices.Application.Partners;
using HomeServices.Application.Tests.Support;
using HomeServices.Domain.Catalog;
using HomeServices.Domain.Files;
using HomeServices.Domain.Identity;
using HomeServices.Domain.Localization;
using HomeServices.Domain.Partners;
using Microsoft.Extensions.Options;

namespace HomeServices.Application.Tests.Partners;

/// <summary>A small catalog, a signed-in Portal user and helpers for partner profile tests.</summary>
public sealed class PartnerTestData
{
    public static readonly DateTimeOffset Now = new(2026, 10, 5, 9, 0, 0, TimeSpan.Zero);

    public PartnerTestData()
    {
        Plumbing = Category.Create("plumbing", Text("Սանտեխնիկա"), 1);
        Heating = Category.Create("heating", Text("Ջեռուցում"), 2);
        Hidden = Category.Create("hidden", Text("Թաքնված"), 3);
        Hidden.Deactivate();
        Boilers = Category.Create("boilers", Text("Կաթսաներ"), 1, parentId: Heating.Id);

        Yerevan = City.Create("yerevan", Text("Երևան"), 1);
        Kentron = Yerevan.AddDistrict("kentron", Text("Կենտրոն"), 1);
        ClosedDistrict = Yerevan.AddDistrict("closed", Text("Փակ"), 2);
        Yerevan.SetDistrictActive(ClosedDistrict.Id, false);
        Ararat = Region.Create("ararat", Text("Արարատ"), 3);
        Masis = City.Create("masis", Text("Մասիս"), 2, Ararat.Id);
        ClosedCity = City.Create("closed-city", Text("Փակ"), 3);
        ClosedCity.Deactivate();

        User = Domain.Identity.User.Register(PhoneNumber.Parse("+37477123456"));
        OtherUser = Domain.Identity.User.Register(PhoneNumber.Parse("+37477654321"));

        Db.Categories.AddRange(Plumbing, Heating, Hidden, Boilers);
        Db.Regions.Add(Ararat);
        Db.Cities.AddRange(Yerevan, Masis, ClosedCity);
        Db.Users.AddRange(User, OtherUser);
        Db.SaveChanges();
    }

    public InMemoryAppDbContext Db { get; } = InMemoryAppDbContext.Create();

    public FakeFileStorage Storage { get; } = new();

    public FakeClock Clock { get; } = new(Now);

    public Category Plumbing { get; }

    public Category Heating { get; }

    public Category Hidden { get; }

    /// <summary>A subcategory of <see cref="Heating"/>.</summary>
    public Category Boilers { get; }

    public City Yerevan { get; }

    /// <summary>The region <see cref="Masis"/> is in.</summary>
    public Region Ararat { get; }

    public District Kentron { get; }

    public District ClosedDistrict { get; }

    public City Masis { get; }

    public City ClosedCity { get; }

    public User User { get; }

    public User OtherUser { get; }

    public ICurrentUser Me => new FakeCurrentUser(User.Id);

    public PartnerProfileDtoFactory Dtos => new(new FileDtoFactory(Storage, Options.Create(new FileSettings()), Clock));

    public static LocalizedText Text(string hy) => LocalizedText.Empty.With("hy", hy);

    /// <summary>A file uploaded (and by default completed) by <paramref name="owner"/>.</summary>
    public StoredFile GivenFile(string contentType = "image/jpeg", User? owner = null, bool ready = true)
    {
        var file = StoredFile.Begin(FileOwnerType.User, (owner ?? User).Id.ToString(), "upload", contentType, 100, Now);
        if (ready)
        {
            file.MarkReady(100, Now);
        }

        Db.Files.Add(file);
        Db.SaveChanges();
        return file;
    }

    /// <summary>A new user with a complete profile moved to <paramref name="status"/>.</summary>
    public PartnerProfile GivenProfile(
        string displayName,
        PartnerStatus status = PartnerStatus.UnderReview,
        PartnerType type = PartnerType.Specialist,
        DateTimeOffset? submittedAt = null,
        string? phone = null,
        DateTimeOffset? approvedAt = null,
        Action<PartnerProfile>? configure = null)
    {
        var owner = Domain.Identity.User.Register(PhoneNumber.Parse(phone ?? $"+37499{Random.Shared.Next(100_000, 999_999)}"));
        owner.UpdateProfile($"Owner of {displayName}", null);
        Db.Users.Add(owner);

        var photo = GivenFile(owner: owner);
        var license = GivenFile("application/pdf", owner);
        var profile = PartnerProfile.Create(owner.Id, type, displayName);
        profile.UpdateDetails(type, displayName, new string('a', 60), 5, null);
        profile.SetServices([Plumbing.Id, Heating.Id]);
        profile.SetAreas([(Yerevan.Id, null)]);
        profile.AddMedia(PartnerMediaKind.WorkExample, photo.Id, null);
        profile.AddMedia(PartnerMediaKind.Document, license.Id, "License");
        profile.MarkCreated(submittedAt ?? Now, null);
        configure?.Invoke(profile);

        if (status != PartnerStatus.Draft)
        {
            profile.Submit(submittedAt ?? Now);
            switch (status)
            {
                case PartnerStatus.NeedsChanges:
                    profile.RequestChanges("Fix it.");
                    break;
                case PartnerStatus.Rejected:
                    profile.Reject("No.");
                    break;
                case PartnerStatus.Approved:
                    profile.Approve(approvedAt ?? Now);
                    break;
                case PartnerStatus.Suspended:
                    profile.Approve(approvedAt ?? Now);
                    profile.Suspend("Complaints.");
                    break;
            }
        }

        Db.PartnerProfiles.Add(profile);
        Db.SaveChanges();
        return profile;
    }

    public SaveMyPartnerProfile ValidSave(Guid? avatarFileId = null) => new(
        PartnerType.Specialist,
        "Aram Plumbing",
        new string('a', 60),
        12,
        avatarFileId,
        [Plumbing.Id],
        [new PartnerAreaDto(Yerevan.Id, null)]);
}
