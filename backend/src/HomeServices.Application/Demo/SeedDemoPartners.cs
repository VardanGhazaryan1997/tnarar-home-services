using HomeServices.Application.Abstractions;
using HomeServices.Application.Files;
using HomeServices.Application.Messaging;
using HomeServices.Domain.Catalog;
using HomeServices.Domain.Files;
using HomeServices.Domain.Identity;
using HomeServices.Domain.Partners;
using Microsoft.EntityFrameworkCore;

namespace HomeServices.Application.Demo;

/// <summary>
/// Creates approved demo companies and specialists (staging and local development only), each with services,
/// areas and work-example pictures, so search and request matching have someone to find. Their phone numbers are
/// +374 99 000 1NN; with a fixed sign-in code (staging) testers can sign in as them. Does nothing when the demo
/// partners already exist. Returns how many were created.
/// </summary>
public sealed record SeedDemoPartners : ICommand<int>;

public sealed class SeedDemoPartnersHandler(IAppDbContext db, IFileStorage storage, TimeProvider clock)
    : ICommandHandler<SeedDemoPartners, int>
{
    /// <summary>Demo phone numbers: +374 99 000 101, 102, …</summary>
    public static PhoneNumber Phone(int index) => PhoneNumber.Parse($"+374990001{index + 1:00}");

    public async Task<int> HandleAsync(SeedDemoPartners command, CancellationToken cancellationToken)
    {
        // Any demo account already there means this ran before (demos that didn't fit the catalog are skipped).
        var phones = DemoPartners.All.Select((_, index) => Phone(index)).ToList();
        if (await db.Users.AnyAsync(u => phones.Contains(u.Phone), cancellationToken))
        {
            return 0;
        }

        var categories = await db.Categories.AsNoTracking().Where(c => c.IsActive).ToDictionaryAsync(c => c.Slug, c => c.Id, cancellationToken);
        var regions = await db.Regions.AsNoTracking().ToDictionaryAsync(r => r.Slug, r => r.Id, cancellationToken);
        var cities = await db.Cities.AsNoTracking().Include(c => c.Districts).Where(c => c.IsActive).ToListAsync(cancellationToken);
        var now = clock.GetUtcNow();
        var created = 0;

        foreach (var (demo, index) in DemoPartners.All.Select((demo, index) => (demo, index)))
        {
            var services = demo.Services.Where(categories.ContainsKey).Select(slug => categories[slug]).ToList();
            var areas = demo.Areas.Select(area => ToChoice(area, regions, cities)).OfType<AreaChoice>().ToList();
            if (services.Count == 0 || areas.Count == 0)
            {
                continue; // the catalog was changed and this demo no longer fits
            }

            var user = User.Register(Phone(index));
            user.UpdateProfile(demo.OwnerName, email: null);
            user.AddRole(UserRoles.Partner);
            db.Users.Add(user);

            var profile = PartnerProfile.Create(user.Id, demo.Type, demo.DisplayName);
            profile.UpdateDetails(demo.Type, demo.DisplayName, demo.About, demo.Years, avatarFileId: null);
            profile.SetServices(services);
            profile.SetAreas(areas);

            for (var picture = 0; picture < 3; picture++)
            {
                var file = await StoreImageAsync(user.Id, (index * 10) + picture, now, cancellationToken);
                db.Files.Add(file);
                profile.AddMedia(PartnerMediaKind.WorkExample, file.Id, demo.Captions.ElementAtOrDefault(picture));
            }

            profile.Submit(now);
            profile.Approve(now);
            db.PartnerProfiles.Add(profile);
            created++;
        }

        await db.SaveChangesAsync(cancellationToken);
        return created;
    }

    private async Task<StoredFile> StoreImageAsync(Guid ownerId, int seed, DateTimeOffset now, CancellationToken cancellationToken)
    {
        const int width = 640;
        const int height = 480;
        var bytes = DemoImage.Png(seed, width, height);
        var file = StoredFile.Begin(FileOwnerType.User, ownerId.ToString(), $"demo-{seed}.png", "image/png", bytes.Length, now);
        using (var content = new MemoryStream(bytes))
        {
            await storage.PutAsync(file.Key, content, file.ContentType, cancellationToken);
        }

        file.MarkReady(bytes.Length, now, thumbnailKey: null, width, height);
        return file;
    }

    // "region:ararat", "city:gyumri" or "district:yerevan/kentron"; null when the place no longer exists.
    private static AreaChoice? ToChoice(string area, IReadOnlyDictionary<string, Guid> regions, IReadOnlyList<City> cities)
    {
        var (kind, slug) = (area[..area.IndexOf(':')], area[(area.IndexOf(':') + 1)..]);
        switch (kind)
        {
            case "region":
                return regions.TryGetValue(slug, out var regionId) ? AreaChoice.WholeRegion(regionId) : null;
            case "city":
                return cities.FirstOrDefault(c => c.Slug == slug) is { } city ? AreaChoice.WholeCity(city.Id) : null;
            default:
                var parts = slug.Split('/');
                var parent = cities.FirstOrDefault(c => c.Slug == parts[0]);
                var district = parent?.Districts.FirstOrDefault(d => d.Slug == parts[1] && d.IsActive);
                return parent is not null && district is not null ? AreaChoice.District(parent.Id, district.Id) : null;
        }
    }
}

/// <summary>The demo partners: invented names, Armenian texts.</summary>
internal static class DemoPartners
{
    public sealed record Demo(
        PartnerType Type,
        string DisplayName,
        string OwnerName,
        int Years,
        string About,
        string[] Services,
        string[] Areas,
        string[] Captions);

    private const string Note = " (Դեմո պրոֆիլ՝ փորձարկման համար։)";

    public static readonly Demo[] All =
    [
        new(PartnerType.Company, "ՇինՎարպետ", "Արմեն Հակոբյան", 15,
            "Կառուցում ենք առանձնատներ և կատարում բնակարանների հիմնանորոգում բանալին ձեռքին։ Ունենք սեփական բրիգադներ և տեխնիկա։" + Note,
            ["construction", "renovation"], ["region:yerevan", "region:kotayk"], ["Առանձնատուն Աբովյանում", "Բնակարանի վերանորոգում", "Հիմքի լցում"]),
        new(PartnerType.Specialist, "Արամ Սանտեխնիկ", "Արամ Պետրոսյան", 12,
            "Սանտեխնիկական բոլոր աշխատանքներ՝ խողովակներ, ծորակներ, ջրատաքացուցիչներ և արտահոսքերի վերացում։ Գալիս եմ նույն օրը։" + Note,
            ["plumbing"], ["city:yerevan"], ["Լոգարանի սանտեխնիկա", "Ջրատաքացուցիչի տեղադրում", "Խողովակների փոխարինում"]),
        new(PartnerType.Company, "ՋերմՏուն", "Գոռ Սարգսյան", 9,
            "Ջեռուցման համակարգեր՝ կաթսաներ, ռադիատորներ և տաք հատակ։ Տեղադրում, սպասարկում և երաշխիք։" + Note,
            ["heating", "plumbing-water-heaters"], ["region:yerevan", "region:ararat", "region:armavir"], ["Կաթսայի տեղադրում", "Տաք հատակ", "Ռադիատորներ"]),
        new(PartnerType.Specialist, "Վահե Էլեկտրիկ", "Վահե Մկրտչյան", 8,
            "Էլեկտրական լարերի անցկացում, վահանակներ, լուսավորություն և անսարքությունների վերացում բնակարաններում և գրասենյակներում։" + Note,
            ["electrical"], ["district:yerevan/arabkir", "district:yerevan/kentron", "district:yerevan/davtashen"], ["Էլեկտրական վահանակ", "Լուսավորության տեղադրում", "Լարերի անցկացում"]),
        new(PartnerType.Company, "Մաքուր Տուն", "Լիլիթ Գրիգորյան", 6,
            "Բնակարանների և գրասենյակների մաքրում, վերանորոգումից հետո մաքրում, պատուհանների լվացում։ Մեր միջոցներով և սարքավորումներով։" + Note,
            ["cleaning"], ["city:yerevan", "city:abovyan", "city:ejmiatsin"], ["Մաքրում վերանորոգումից հետո", "Պատուհանների լվացում", "Գրասենյակի մաքրում"]),
        new(PartnerType.Company, "ՏանիքՊրո", "Սամվել Ավետիսյան", 11,
            "Նոր տանիքներ և տանիքների վերանորոգում՝ մետաղական թիթեղ, կղմինդր, ջրահեռացման համակարգեր։" + Note,
            ["roofing"], ["region:shirak", "region:lori", "region:aragatsotn"], ["Մետաղական տանիք", "Ջրահեռացում", "Տանիքի վերանորոգում"]),
        new(PartnerType.Specialist, "Նարեկ Սալիկագործ", "Նարեկ Հովհաննիսյան", 10,
            "Սալիկապատում, հատակի և պատերի հարդարում, լոգարանների և խոհանոցների վերանորոգում մաքուր և ճշգրիտ։" + Note,
            ["renovation-tiling", "renovation-bathroom", "renovation-kitchen"], ["city:yerevan", "city:masis"], ["Լոգարանի սալիկապատում", "Խոհանոցի պատ", "Հատակի սալիկ"]),
        new(PartnerType.Company, "Լուսամուտ Պլյուս", "Կարեն Մանուկյան", 13,
            "Մետաղապլաստե և ալյումինե պատուհաններ, դռներ, պատշգամբների ապակեպատում։ Չափագրումն անվճար է։" + Note,
            ["windows-doors"], ["region:yerevan", "region:kotayk", "region:ararat"], ["Մետաղապլաստե պատուհաններ", "Պատշգամբի ապակեպատում", "Մուտքի դուռ"]),
        new(PartnerType.Company, "ԿլիմատՍերվիս", "Տիգրան Ղազարյան", 7,
            "Օդորակիչների տեղադրում և սպասարկում, օդափոխության համակարգեր տների, գրասենյակների և ռեստորանների համար։" + Note,
            ["hvac"], ["city:yerevan", "city:ejmiatsin", "city:ashtarak"], ["Օդորակիչի տեղադրում", "Օդափոխություն", "Սպասարկում"]),
        new(PartnerType.Specialist, "Հայկ Եռակցող", "Հայկ Վարդանյան", 14,
            "Եռակցման և մետաղական աշխատանքներ՝ դարպասներ, ցանկապատեր, ճաղավանդակներ, ծածկեր և դարբնոցային զարդեր։" + Note,
            ["metalwork"], ["region:ararat", "region:armavir"], ["Դարպաս", "Ճաղավանդակ", "Ծածկ"]),
        new(PartnerType.Company, "Փայտե Աշխարհ", "Սուրեն Աբրահամյան", 18,
            "Պատվերով կահույք՝ խոհանոցներ, զգեստապահարաններ, փայտե աստիճաններ և սեղանի երեսներ։" + Note,
            ["carpentry"], ["region:yerevan", "region:kotayk"], ["Խոհանոցային կահույք", "Զգեստապահարան", "Փայտե աստիճան"]),
        new(PartnerType.Company, "ՀողՏեխ", "Արթուր Ներսիսյան", 10,
            "Հողային աշխատանքներ, քանդում, աղբի դուրսբերում և տեխնիկայի վարձույթ՝ էքսկավատոր, ինքնաթափ։" + Note,
            ["earthworks"], ["region:yerevan", "region:ararat", "region:kotayk", "region:armavir"], ["Հիմնափոս", "Քանդման աշխատանքներ", "Տեխնիկա"]),
        new(PartnerType.Specialist, "Կանաչ Այգի", "Անի Ստեփանյան", 5,
            "Այգիների և բակերի կանաչապատում, սիզամարգեր, ոռոգման համակարգեր և սալահատակ։" + Note,
            ["landscaping"], ["city:yerevan", "region:kotayk"], ["Սիզամարգ", "Ոռոգում", "Սալահատակ"]),
        new(PartnerType.Company, "Անվտանգ Տուն", "Դավիթ Խաչատրյան", 8,
            "Տեսահսկում, ահազանգման համակարգեր, դոմոֆոններ և խելացի տուն։ Տեղադրում և հեռակա կարգավորում։" + Note,
            ["security-systems"], ["region:yerevan", "region:kotayk", "region:aragatsotn", "region:armavir", "region:ararat"], ["Տեսախցիկներ", "Դոմոֆոն", "Խելացի տուն"]),
        new(PartnerType.Company, "Նախագիծ Ստուդիա", "Մարիամ Բաղդասարյան", 12,
            "Ճարտարապետական և ինտերիերի դիզայն, կոնստրուկտիվ հաշվարկներ, նախահաշիվներ և տեխնիկական հսկողություն։" + Note,
            ["design-engineering"], ["region:yerevan", "region:kotayk", "region:shirak", "region:lori"], ["Ինտերիերի նախագիծ", "Առանձնատան նախագիծ", "3D վիզուալիզացիա"]),
        new(PartnerType.Specialist, "Վարպետ Ժամով", "Գագիկ Մարտիրոսյան", 20,
            "Մանր տնային աշխատանքներ՝ դարակների, լամպերի և հեռուստացույցների ամրացում, կողպեքներ, կենցաղային տեխնիկայի միացում։" + Note,
            ["handyman"], ["district:yerevan/ajapnyak", "district:yerevan/malatia-sebastia", "district:yerevan/shengavit"], ["Հեռուստացույցի ամրացում", "Դարակներ", "Կողպեքի փոխարինում"]),
        new(PartnerType.Company, "ԹերմոՖասադ", "Ռուբեն Գևորգյան", 9,
            "Ֆասադային աշխատանքներ և ջերմամեկուսացում՝ տուֆ, դեկորատիվ սվաղ, օդափոխվող ֆասադներ, ալպինիստական աշխատանքներ։" + Note,
            ["exterior-cladding", "insulation"], ["region:yerevan", "region:kotayk"], ["Տուֆե ֆասադ", "Ջերմամեկուսացում", "Դեկորատիվ սվաղ"]),
        new(PartnerType.Specialist, "Գյումրու Վարպետ", "Սերգեյ Հարությունյան", 16,
            "Բնակարանների վերանորոգում և հարդարում Գյումրիում և Շիրակի մարզում՝ սվաղ, ներկում, գիպսաստվարաթուղթ։" + Note,
            ["renovation"], ["region:shirak"], ["Բնակարանի հարդարում", "Գիպսաստվարաթուղթ", "Ներկում"]),
        new(PartnerType.Company, "Վանաձոր Շին", "Էդգար Սիմոնյան", 11,
            "Շինարարական և վերանորոգման աշխատանքներ Լոռու և Տավուշի մարզերում։ Աշխատում ենք պայմանագրով։" + Note,
            ["construction", "renovation", "roofing"], ["region:lori", "region:tavush"], ["Շինարարություն", "Տանիք", "Ներքին հարդարում"]),
        new(PartnerType.Specialist, "Սյունիքի Էլեկտրիկ", "Արմեն Դանիելյան", 7,
            "Էլեկտրամոնտաժային աշխատանքներ, արևային վահանակներ և գեներատորներ Կապանում, Գորիսում և ամբողջ Սյունիքում։" + Note,
            ["electrical"], ["region:syunik", "region:vayots-dzor"], ["Արևային վահանակներ", "Վահանակ", "Լուսավորություն"]),
        new(PartnerType.Company, "Սևան Սերվիս", "Վարդան Զաքարյան", 6,
            "Սանտեխնիկա, ջեռուցում և մանր վերանորոգում Գեղարքունիքի մարզում՝ Սևան, Գավառ, Մարտունի։" + Note,
            ["plumbing", "heating", "handyman"], ["region:gegharkunik"], ["Ջեռուցման կաթսա", "Սանտեխնիկա", "Մանր վերանորոգում"]),
        new(PartnerType.Specialist, "Մարինե Մաքրում", "Մարինե Պողոսյան", 4,
            "Կանոնավոր և հիմնական մաքրում, փափուկ կահույքի և գորգերի քիմմաքրում։ Աշխատում եմ խնամքով և ճշտապահ։" + Note,
            ["cleaning-regular", "cleaning-deep", "cleaning-upholstery", "cleaning-carpets"], ["district:yerevan/nor-nork", "district:yerevan/avan", "district:yerevan/kanaker-zeytun"], ["Փափուկ կահույք", "Գորգեր", "Հիմնական մաքրում"]),
        new(PartnerType.Company, "Արարատ Վերանորոգում", "Հովիկ Մելքոնյան", 10,
            "Բնակարանների և տների վերանորոգում Արարատի և Արմավիրի մարզերում՝ հատակ, պատեր, առաստաղ, լոգարան։" + Note,
            ["renovation", "plumbing"], ["region:ararat", "region:armavir"], ["Վերանորոգում", "Լոգարան", "Առաստաղ"]),
        new(PartnerType.Specialist, "Լևոն Ներկարար", "Լևոն Ասատրյան", 13,
            "Ներկարարական և պաստառապատման աշխատանքներ, դեկորատիվ ծածկույթներ։ Մաքուր աշխատանք, ժամանակին։" + Note,
            ["renovation-painting", "renovation-wallpaper", "renovation-decorative"], ["city:yerevan", "city:abovyan"], ["Ներկում", "Պաստառ", "Դեկորատիվ պատ"]),
    ];
}
