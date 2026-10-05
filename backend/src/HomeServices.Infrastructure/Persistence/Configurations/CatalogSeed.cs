using HomeServices.Domain.Localization;

namespace HomeServices.Infrastructure.Persistence.Configurations;

/// <summary>
/// Reference data the platform starts with: Yerevan's districts (categories are in CatalogSeed.Categories.cs,
/// regions, towns and villages in CatalogSeed.Places.cs). Staff manage them in the Back Office later.
/// Ids are fixed so migrations stay stable.
/// </summary>
internal static partial class CatalogSeed
{
    internal static LocalizedText Text(string hy, string ru, string en) =>
        LocalizedText.From(new Dictionary<string, string> { ["hy"] = hy, ["ru"] = ru, ["en"] = en });

    /// <summary>A name in the three main languages plus Arabic, Persian and Hindi (portal-only languages).</summary>
    internal static LocalizedText Text(string hy, string ru, string en, string ar, string fa, string hi) =>
        LocalizedText.From(new Dictionary<string, string>
        {
            ["hy"] = hy, ["ru"] = ru, ["en"] = en, ["ar"] = ar, ["fa"] = fa, ["hi"] = hi,
        });

    internal static readonly Guid YerevanId = new("019a0000-0000-7000-8000-000000000201");

    /// <summary>Yerevan's 12 administrative districts.</summary>
    internal static readonly (Guid Id, string Slug, LocalizedText Name)[] YerevanDistricts =
    [
        (new("019a0000-0000-7000-8000-000000000301"), "ajapnyak", Text("Աջափնյակ", "Ачапняк", "Ajapnyak", "أجابنياك", "آجاپنیاک", "अजाप्न्याक")),
        (new("019a0000-0000-7000-8000-000000000302"), "arabkir", Text("Արաբկիր", "Арабкир", "Arabkir", "أرابكير", "آرابکیر", "अराबकिर")),
        (new("019a0000-0000-7000-8000-000000000303"), "avan", Text("Ավան", "Аван", "Avan", "أفان", "آوان", "अवान")),
        (new("019a0000-0000-7000-8000-000000000304"), "davtashen", Text("Դավթաշեն", "Давташен", "Davtashen", "دافتاشين", "داوتاشن", "दावताशेन")),
        (new("019a0000-0000-7000-8000-000000000305"), "erebuni", Text("Էրեբունի", "Эребуни", "Erebuni", "إريبوني", "اربونی", "एरेबुनी")),
        (new("019a0000-0000-7000-8000-000000000306"), "kanaker-zeytun", Text("Քանաքեռ-Զեյթուն", "Канакер-Зейтун", "Kanaker-Zeytun", "كاناكير-زيتون", "کاناکر-زیتون", "कानाकेर-ज़ेयतुन")),
        (new("019a0000-0000-7000-8000-000000000307"), "kentron", Text("Կենտրոն", "Кентрон", "Kentron", "كينترون", "کنترون", "केंत्रोन")),
        (new("019a0000-0000-7000-8000-000000000308"), "malatia-sebastia", Text("Մալաթիա-Սեբաստիա", "Малатия-Себастия", "Malatia-Sebastia", "ملاطية-سيباستيا", "ملاطیه-سباستیا", "मालातिया-सेबास्तिया")),
        (new("019a0000-0000-7000-8000-000000000309"), "nor-nork", Text("Նոր Նորք", "Нор Норк", "Nor Nork", "نور نورك", "نور نورک", "नोर नोर्क")),
        (new("019a0000-0000-7000-8000-000000000310"), "nork-marash", Text("Նորք-Մարաշ", "Норк-Мараш", "Nork-Marash", "نورك-ماراش", "نورک-ماراش", "नोर्क-माराश")),
        (new("019a0000-0000-7000-8000-000000000311"), "nubarashen", Text("Նուբարաշեն", "Нубарашен", "Nubarashen", "نوباراشين", "نوباراشن", "नुबाराशेन")),
        (new("019a0000-0000-7000-8000-000000000312"), "shengavit", Text("Շենգավիթ", "Шенгавит", "Shengavit", "شينغافيت", "شنگاویت", "शेंगावित")),
    ];
}
