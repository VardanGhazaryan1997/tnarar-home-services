# Generates CatalogSeed.WorkItems.cs from work_items.csv (UTF-8; edit it in Excel and save as "CSV UTF-8").
# Usage (from backend/tools/catalog-seed): python work_items.py work_items.csv ../../src/HomeServices.Infrastructure/Persistence/Configurations/CatalogSeed.WorkItems.cs
import csv, re, sys, uuid

CSV, OUT = sys.argv[1], sys.argv[2]
NS = uuid.UUID("019a0000-0000-7000-8000-0000000005ff")  # ids derive from the slug, so they never change
UNITS = {"SquareMeter", "RunningMeter", "Piece", "Point", "CubicMeter", "Hour", "Fixed"}
SURFACES = {"None", "Floor", "Wall", "Ceiling"}
LANGUAGES = ["hy", "ru", "en", "ar", "fa", "hi"]

def cs(s): return s.replace("\\", "\\\\").replace('"', '\\"')

rows = list(csv.DictReader(open(CSV, encoding="utf-8-sig", newline="")))
slugs = set()
out = ['''using HomeServices.Domain.Catalog;
using HomeServices.Domain.Localization;

namespace HomeServices.Infrastructure.Persistence.Configurations;

// Work items with starter labour prices for Yerevan (AMD per unit), generated from tools/catalog-seed/work_items.csv.
// Edit the CSV and run tools/catalog-seed/work_items.py instead of changing this file by hand.
internal static partial class CatalogSeed
{
    /// <summary>Work items in display order within each subcategory.</summary>
    internal static readonly (Guid Id, string CategorySlug, string Slug, WorkUnit Unit, WorkSurface Surface, int PriceMin, int PriceTypical, int PriceMax, LocalizedText Name)[] WorkItems =
    [''']
current = None
for n, r in enumerate(rows, 2):
    slug = r["slug"].strip()
    where = f"row {n} ({slug})"
    assert re.fullmatch(r"[a-z0-9]+(-[a-z0-9]+)*", slug) and len(slug) <= 64, f"{where}: bad slug"
    assert slug not in slugs, f"{where}: duplicate slug"; slugs.add(slug)
    assert r["unit"] in UNITS, f"{where}: unit must be one of {sorted(UNITS)}"
    assert r["surface"] in SURFACES, f"{where}: surface must be one of {sorted(SURFACES)}"
    lo, typ, hi = (int(r[k].replace(" ", "").replace(",", "")) for k in ("price_min", "price_typical", "price_max"))
    assert 0 <= lo <= typ <= hi <= 100_000_000, f"{where}: prices must be min <= typical <= max"
    names = [r[l].strip() for l in LANGUAGES]
    assert all(names) and all(len(x) <= 100 for x in names), f"{where}: every name is required, at most 100 characters"
    if r["subcategory"] != current:
        out.append(f"        // {r['subcategory']}")
        current = r["subcategory"]
    text = ", ".join(f'"{cs(x)}"' for x in names)
    out.append(f'        (new("{uuid.uuid5(NS, slug)}"), "{r["subcategory"]}", "{slug}", WorkUnit.{r["unit"]}, WorkSurface.{r["surface"]}, {lo}, {typ}, {hi}, Text({text})),')
out.append('''    ];
}
''')
open(OUT, "w", encoding="utf-8", newline="\n").write("\n".join(out))
print(len(rows), "work items")
