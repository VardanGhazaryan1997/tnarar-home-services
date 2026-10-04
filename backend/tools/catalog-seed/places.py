# Generates CatalogSeed.Places.cs from armenia_settlements.csv: regions, towns and villages.
import csv, sys, uuid, re

CSV, OUT = sys.argv[1], sys.argv[2]
NS = uuid.UUID("019a0000-0000-7000-8000-0000000004ff")

REGIONS = [  # slug, csv name, hy, ru, en
    ("yerevan", None, "Երևան", "Ереван", "Yerevan"),
    ("aragatsotn", "Aragatsotn", "Արագածոտն", "Арагацотн", "Aragatsotn"),
    ("ararat", "Ararat", "Արարատ", "Арарат", "Ararat"),
    ("armavir", "Armavir", "Արմավիր", "Армавир", "Armavir"),
    ("gegharkunik", "Gegharkunik", "Գեղարքունիք", "Гегаркуник", "Gegharkunik"),
    ("kotayk", "Kotayk", "Կոտայք", "Котайк", "Kotayk"),
    ("lori", "Lori", "Լոռի", "Лори", "Lori"),
    ("shirak", "Shirak", "Շիրակ", "Ширак", "Shirak"),
    ("syunik", "Syunik", "Սյունիք", "Сюник", "Syunik"),
    ("tavush", "Tavush", "Տավուշ", "Тавуш", "Tavush"),
    ("vayots-dzor", "Vayots Dzor", "Վայոց ձոր", "Вайоц Дзор", "Vayots Dzor"),
]

# Towns: Armenian name -> (slug, ru, en). The four launch cities keep their ids and slugs.
EXISTING = {
    "yerevan": "019a0000-0000-7000-8000-000000000201",
    "ejmiatsin": "019a0000-0000-7000-8000-000000000202",
    "abovyan": "019a0000-0000-7000-8000-000000000203",
    "ashtarak": "019a0000-0000-7000-8000-000000000204",
    "masis": "019a0000-0000-7000-8000-000000000205",
}
TOWNS = {
    "Աշտարակ": ("ashtarak", "Аштарак", "Ashtarak"), "Ապարան": ("aparan", "Апаран", "Aparan"), "Թալին": ("talin", "Талин", "Talin"),
    "Արարատ": ("ararat", "Арарат", "Ararat"), "Արտաշատ": ("artashat", "Арташат", "Artashat"), "Մասիս": ("masis", "Масис", "Masis"), "Վեդի": ("vedi", "Веди", "Vedi"),
    "Արմավիր": ("armavir", "Армавир", "Armavir"), "Մեծամոր": ("metsamor", "Мецамор", "Metsamor"),
    "Գավառ": ("gavar", "Гавар", "Gavar"), "Ճամբարակ": ("chambarak", "Чамбарак", "Chambarak"), "Մարտունի": ("martuni", "Мартуни", "Martuni"),
    "Սևան": ("sevan", "Севан", "Sevan"), "Վարդենիս": ("vardenis", "Варденис", "Vardenis"),
    "Աբովյան": ("abovyan", "Абовян", "Abovyan"), "Բյուրեղավան": ("byureghavan", "Бюрегаван", "Byureghavan"), "Եղվարդ": ("yeghvard", "Егвард", "Yeghvard"),
    "Ծաղկաձոր": ("tsaghkadzor", "Цахкадзор", "Tsaghkadzor"), "Հրազդան": ("hrazdan", "Раздан", "Hrazdan"), "Նոր Հաճն": ("nor-hachn", "Нор Ачин", "Nor Hachn"),
    "Չարենցավան": ("charentsavan", "Чаренцаван", "Charentsavan"),
    "Ալավերդի": ("alaverdi", "Алаверди", "Alaverdi"), "Ախթալա": ("akhtala", "Ахтала", "Akhtala"), "Թումանյան": ("tumanyan", "Туманян", "Tumanyan"),
    "Շամլուղ": ("shamlugh", "Шамлуг", "Shamlugh"), "Սպիտակ": ("spitak", "Спитак", "Spitak"), "Ստեփանավան": ("stepanavan", "Степанаван", "Stepanavan"),
    "Վանաձոր": ("vanadzor", "Ванадзор", "Vanadzor"), "Տաշիր": ("tashir", "Ташир", "Tashir"),
    "Արթիկ": ("artik", "Артик", "Artik"), "Գյումրի": ("gyumri", "Гюмри", "Gyumri"), "Մարալիկ": ("maralik", "Маралик", "Maralik"),
    "Ագարակ": ("agarak", "Агарак", "Agarak"), "Գորիս": ("goris", "Горис", "Goris"), "Դաստակերտ": ("dastakert", "Дастакерт", "Dastakert"),
    "Կապան": ("kapan", "Капан", "Kapan"), "Մեղրի": ("meghri", "Мегри", "Meghri"), "Սիսիան": ("sisian", "Сисиан", "Sisian"), "Քաջարան": ("kajaran", "Каджаран", "Kajaran"),
    "Այրում": ("ayrum", "Айрум", "Ayrum"), "Բերդ": ("berd", "Берд", "Berd"), "Դիլիջան": ("dilijan", "Дилижан", "Dilijan"),
    "Իջևան": ("ijevan", "Иджеван", "Ijevan"), "Նոյեմբերյան": ("noyemberyan", "Ноемберян", "Noyemberyan"),
    "Եղեգնաձոր": ("yeghegnadzor", "Ехегнадзор", "Yeghegnadzor"), "Ջերմուկ": ("jermuk", "Джермук", "Jermuk"), "Վայք": ("vayk", "Вайк", "Vayk"),
}
# Vagharshapat is the launch city "Ejmiatsin" (kept as is).
TOWN_OVERRIDES = {"Վաղարշապատ": ("ejmiatsin", "Эчмиадзин", "Ejmiatsin", "Էջմիածին")}

LAT = {"ա": "a", "բ": "b", "գ": "g", "դ": "d", "զ": "z", "է": "e", "ը": "y", "թ": "t", "ժ": "zh", "ի": "i", "լ": "l", "խ": "kh",
       "ծ": "ts", "կ": "k", "հ": "h", "ձ": "dz", "ղ": "gh", "ճ": "ch", "մ": "m", "յ": "y", "ն": "n", "շ": "sh", "չ": "ch", "պ": "p",
       "ջ": "j", "ռ": "r", "ս": "s", "վ": "v", "տ": "t", "ր": "r", "ց": "ts", "փ": "p", "ք": "k", "օ": "o", "ֆ": "f"}
CYR = {"ա": "а", "բ": "б", "գ": "г", "դ": "д", "զ": "з", "է": "э", "ը": "ы", "թ": "т", "ժ": "ж", "ի": "и", "լ": "л", "խ": "х",
       "ծ": "ц", "կ": "к", "հ": "", "ձ": "дз", "ղ": "х", "ճ": "ч", "մ": "м", "յ": "й", "ն": "н", "շ": "ш", "չ": "ч", "պ": "п",
       "ջ": "дж", "ռ": "р", "ս": "с", "վ": "в", "տ": "т", "ր": "р", "ց": "ц", "փ": "п", "ք": "к", "օ": "о", "ֆ": "ф"}
VOWELS = set("աեէըիոօու")

def word(w, table, latin):
    w = w.lower()
    out, i = [], 0
    while i < len(w):
        c, nxt = w[i], w[i + 1] if i + 1 < len(w) else ""
        start = i == 0
        if c == "ո" and nxt == "ւ":
            out.append("u" if latin else "у"); i += 2; continue
        if c == "և":
            out.append(("yev" if start else "ev") if latin else ("ев" if start else "ев")); i += 1; continue
        if c == "ե":
            out.append(("ye" if start else "e") if latin else ("е")); i += 1; continue
        if c == "ո":
            out.append(("vo" if start else "o") if latin else ("во" if start else "о")); i += 1; continue
        if c == "յ" and not latin:
            if nxt == "ա": out.append("я"); i += 2; continue
            if nxt == "ո" and w[i + 2:i + 3] == "ւ": out.append("ю"); i += 3; continue
            if nxt == "ե": out.append("е"); i += 2; continue
        if c in table:
            out.append(table[c]); i += 1; continue
        out.append(c); i += 1
    s = "".join(out)
    return s[:1].upper() + s[1:]

def translit(name, latin):
    parts = re.split(r"([ \-])", name)
    return "".join(p if p in (" ", "-") else word(p, LAT if latin else CYR, latin) for p in parts)

def slugify(en):
    s = re.sub(r"[^a-z0-9]+", "-", en.lower()).strip("-")
    return s

rows = list(csv.DictReader(open(CSV, encoding="utf-8-sig")))
places = []  # (region, kind, hy, slug, ru, en)
for r in rows:
    region = next(x for x in REGIONS if x[1] == r["Marz (English)"].strip())
    hy = r["Settlement"].strip()
    town = r["Type"].strip() == "Քաղաք"
    if town:
        if hy in TOWN_OVERRIDES:
            slug, ru, en, hy = TOWN_OVERRIDES[hy]
        else:
            slug, ru, en = TOWNS[hy]
        places.append((region[0], "City", hy, slug, ru, en))
    else:
        en = translit(hy, True)
        places.append((region[0], "Village", hy, slugify(en), translit(hy, False), en))

# Village slugs that clash with a town or another village get their region added.
from collections import Counter
counts = Counter(p[3] for p in places)
towns = {p[3] for p in places if p[1] == "City"} | {"yerevan"}
fixed = []
for p in places:
    slug = p[3]
    if p[1] == "Village" and (counts[slug] > 1 or slug in towns):
        slug = f"{slug}-{p[0]}"
    fixed.append(p[:3] + (slug,) + p[4:])
places = fixed
assert len({p[3] for p in places}) == len(places), "duplicate slugs"
for p in places:
    assert re.fullmatch(r"[a-z0-9]+(-[a-z0-9]+)*", p[3]) and len(p[3]) <= 64, p

region_order = {r[0]: i for i, r in enumerate(REGIONS)}
places.sort(key=lambda p: (region_order[p[0]], 0 if p[1] == "City" else 1, p[2]))

def cs(s): return s.replace("\\", "\\\\").replace('"', '\\"')
def rid(slug): return f"019a0000-0000-7000-8000-{400 + region_order[slug] + 1:012d}"
def pid(slug): return EXISTING.get(slug) or str(uuid.uuid5(NS, slug))

out = ['''using HomeServices.Domain.Catalog;
using HomeServices.Domain.Localization;

namespace HomeServices.Infrastructure.Persistence.Configurations;

// Regions (marzes), towns and villages of Armenia, generated from armenia_settlements.csv. Town names in Russian and
// English are written by hand; village names in Russian and English are transliterated from Armenian and may need review.
internal static partial class CatalogSeed
{
    /// <summary>Yerevan and the ten regions (marzes).</summary>
    internal static readonly (Guid Id, string Slug, LocalizedText Name)[] Regions =
    [''']
for r in REGIONS:
    out.append(f'        (new("{rid(r[0])}"), "{r[0]}", Text("{cs(r[2])}", "{cs(r[3])}", "{cs(r[4])}")),')
out.append('''    ];

    internal static readonly Guid YerevanRegionId = new("''' + rid("yerevan") + '''");

    /// <summary>
    /// Every town and village, in display order: Yerevan, then each region's towns and villages. The launch cities keep
    /// their ids; the others are derived from the slug, so adding places later doesn't change existing ids.
    /// </summary>
    internal static readonly (Guid Id, Guid RegionId, SettlementKind Kind, string Slug, LocalizedText Name)[] Cities =
    [''')
out.append(f'        (new("019a0000-0000-7000-8000-000000000201"), YerevanRegionId, SettlementKind.City, "yerevan", Text("Երևան", "Ереван", "Yerevan")),')
current = None
for region, kind, hy, slug, ru, en in places:
    if region != current:
        out.append(f"        // {next(r[4] for r in REGIONS if r[0] == region)}")
        current = region
    out.append(f'        (new("{pid(slug)}"), new("{rid(region)}"), SettlementKind.{kind}, "{slug}", Text("{cs(hy)}", "{cs(ru)}", "{cs(en)}")),')
out.append('''    ];
}
''')
open(OUT, "w", encoding="utf-8", newline="\n").write("\n".join(out))
print(len(places) + 1, "places;", sum(1 for p in places if p[1] == "City") + 1, "towns")
