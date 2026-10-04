using HomeServices.Domain.Localization;

namespace HomeServices.Infrastructure.Persistence.Configurations;

/// <summary>
/// Reference data the platform starts with: the initial service categories (from the
/// partner network) and the five launch cities. Staff manage them in the Back Office later.
/// Ids are fixed so migrations stay stable.
/// </summary>
internal static class CatalogSeed
{
    internal static LocalizedText Text(string hy, string ru, string en) =>
        LocalizedText.From(new Dictionary<string, string> { ["hy"] = hy, ["ru"] = ru, ["en"] = en });

    internal static readonly (Guid Id, string Slug, string Icon, LocalizedText Name)[] Categories =
    [
        (new("019a0000-0000-7000-8000-000000000101"), "construction", "brick", Text("Շինարարություն", "Строительство", "Construction")),
        (new("019a0000-0000-7000-8000-000000000102"), "renovation", "roller", Text("Վերանորոգում", "Ремонт", "Renovation")),
        (new("019a0000-0000-7000-8000-000000000103"), "plumbing", "pipe", Text("Սանտեխնիկա", "Сантехника", "Plumbing")),
        (new("019a0000-0000-7000-8000-000000000104"), "heating", "flame", Text("Ջեռուցում", "Отопление", "Heating")),
        (new("019a0000-0000-7000-8000-000000000105"), "electrical", "bolt", Text("Էլեկտրական աշխատանքներ", "Электромонтажные работы", "Electrical work")),
        (new("019a0000-0000-7000-8000-000000000106"), "exterior-cladding", "facade", Text("Ֆասադի երեսպատում", "Облицовка фасада", "Exterior cladding")),
        (new("019a0000-0000-7000-8000-000000000107"), "cleaning", "sparkle", Text("Պրոֆեսիոնալ մաքրում", "Профессиональная уборка", "Professional cleaning")),
    ];

    internal static readonly Guid YerevanId = new("019a0000-0000-7000-8000-000000000201");

    internal static readonly (Guid Id, string Slug, LocalizedText Name)[] Cities =
    [
        (YerevanId, "yerevan", Text("Երևան", "Ереван", "Yerevan")),
        (new("019a0000-0000-7000-8000-000000000202"), "ejmiatsin", Text("Էջմիածին", "Эчмиадзин", "Ejmiatsin")),
        (new("019a0000-0000-7000-8000-000000000203"), "abovyan", Text("Աբովյան", "Абовян", "Abovyan")),
        (new("019a0000-0000-7000-8000-000000000204"), "ashtarak", Text("Աշտարակ", "Аштарак", "Ashtarak")),
        (new("019a0000-0000-7000-8000-000000000205"), "masis", Text("Մասիս", "Масис", "Masis")),
    ];

    /// <summary>Yerevan's 12 administrative districts.</summary>
    internal static readonly (Guid Id, string Slug, LocalizedText Name)[] YerevanDistricts =
    [
        (new("019a0000-0000-7000-8000-000000000301"), "ajapnyak", Text("Աջափնյակ", "Ачапняк", "Ajapnyak")),
        (new("019a0000-0000-7000-8000-000000000302"), "arabkir", Text("Արաբկիր", "Арабкир", "Arabkir")),
        (new("019a0000-0000-7000-8000-000000000303"), "avan", Text("Ավան", "Аван", "Avan")),
        (new("019a0000-0000-7000-8000-000000000304"), "davtashen", Text("Դավթաշեն", "Давташен", "Davtashen")),
        (new("019a0000-0000-7000-8000-000000000305"), "erebuni", Text("Էրեբունի", "Эребуни", "Erebuni")),
        (new("019a0000-0000-7000-8000-000000000306"), "kanaker-zeytun", Text("Քանաքեռ-Զեյթուն", "Канакер-Зейтун", "Kanaker-Zeytun")),
        (new("019a0000-0000-7000-8000-000000000307"), "kentron", Text("Կենտրոն", "Кентрон", "Kentron")),
        (new("019a0000-0000-7000-8000-000000000308"), "malatia-sebastia", Text("Մալաթիա-Սեբաստիա", "Малатия-Себастия", "Malatia-Sebastia")),
        (new("019a0000-0000-7000-8000-000000000309"), "nor-nork", Text("Նոր Նորք", "Нор Норк", "Nor Nork")),
        (new("019a0000-0000-7000-8000-000000000310"), "nork-marash", Text("Նորք-Մարաշ", "Норк-Мараш", "Nork-Marash")),
        (new("019a0000-0000-7000-8000-000000000311"), "nubarashen", Text("Նուբարաշեն", "Нубарашен", "Nubarashen")),
        (new("019a0000-0000-7000-8000-000000000312"), "shengavit", Text("Շենգավիթ", "Шенгавит", "Shengavit")),
    ];
}
