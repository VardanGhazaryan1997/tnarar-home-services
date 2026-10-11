# Generates CatalogSeed.Materials.cs from materials.csv and work_item_materials.csv (UTF-8; edit them in Excel and save
# as "CSV UTF-8").
# Usage (from backend/tools/catalog-seed):
#   python materials.py materials.csv work_item_materials.csv work_items.csv ../../src/HomeServices.Infrastructure/Persistence/Configurations/CatalogSeed.Materials.cs
import csv, re, sys, uuid
from decimal import Decimal

MATERIALS, LINKS, WORK_ITEMS, OUT = sys.argv[1:5]
NS = uuid.UUID("019a0000-0000-7000-8000-0000000006ff")  # ids derive from the slugs, so they never change
UNITS = {"Kilogram", "Liter", "SquareMeter", "RunningMeter", "CubicMeter", "Piece"}
LANGUAGES = ["hy", "ru", "en", "ar", "fa", "hi"]

def cs(s): return s.replace("\\", "\\\\").replace('"', '\\"')
def fail(message): sys.exit(message)
def read(path): return list(csv.DictReader(open(path, encoding="utf-8-sig", newline="")))

work_slugs = {r["slug"].strip() for r in read(WORK_ITEMS)}
out = ['''using HomeServices.Domain.Catalog;
using HomeServices.Domain.Localization;

namespace HomeServices.Infrastructure.Persistence.Configurations;

// Materials with starter Yerevan prices (AMD per unit: budget, standard, premium) and how much of them work items use,
// generated from tools/catalog-seed/materials.csv and work_item_materials.csv. Edit the CSVs and run
// tools/catalog-seed/materials.py instead of changing this file by hand.
internal static partial class CatalogSeed
{
    /// <summary>Materials in display order.</summary>
    internal static readonly (Guid Id, string Slug, MaterialUnit Unit, int PriceBudget, int PriceStandard, int PricePremium, LocalizedText Name)[] Materials =
    [''']
materials = set()
for n, r in enumerate(read(MATERIALS), 2):
    slug = r["slug"].strip()
    where = f"materials row {n} ({slug})"
    if not re.fullmatch(r"[a-z0-9]+(-[a-z0-9]+)*", slug) or slug in materials: fail(f"{where}: bad or repeated slug")
    if r["unit"] not in UNITS: fail(f"{where}: unit must be one of {sorted(UNITS)}")
    budget, standard, premium = (int(r[k]) for k in ("price_budget", "price_standard", "price_premium"))
    if not 1 <= budget <= standard <= premium <= 10_000_000: fail(f"{where}: prices must be 1 ≤ budget ≤ standard ≤ premium")
    if any(not r[l].strip() for l in LANGUAGES): fail(f"{where}: a name is missing")
    materials.add(slug)
    names = ", ".join(f'"{cs(r[l].strip())}"' for l in LANGUAGES)
    out.append(f'        (new("{uuid.uuid5(NS, "material/" + slug)}"), "{slug}", MaterialUnit.{r["unit"]}, {budget}, {standard}, {premium}, Text({names})),')
out.append('''    ];

    /// <summary>How much of each material a unit of work uses (by slugs), with waste in percent, in display order.</summary>
    internal static readonly (Guid Id, string WorkItemSlug, string MaterialSlug, decimal QuantityPerUnit, int WastePercent)[] WorkItemMaterials =
    [''')
pairs = set()
for n, r in enumerate(read(LINKS), 2):
    work, material = r["work_item"].strip(), r["material"].strip()
    where = f"work_item_materials row {n} ({work} / {material})"
    if work not in work_slugs: fail(f"{where}: unknown work item")
    if material not in materials: fail(f"{where}: unknown material")
    if (work, material) in pairs: fail(f"{where}: repeated")
    quantity, waste = Decimal(r["quantity_per_unit"]), int(r["waste_percent"])
    if not 0 < quantity <= 1000 or not 0 <= waste <= 100: fail(f"{where}: amount 0–1000, waste 0–100")
    pairs.add((work, material))
    out.append(f'        (new("{uuid.uuid5(NS, f"work-item-material/{work}/{material}")}"), "{work}", "{material}", {quantity.normalize():f}m, {waste}),')
out.append("    ];\n}\n")
open(OUT, "w", encoding="utf-8", newline="\n").write("\n".join(out))
print(f"{len(materials)} materials, {len(pairs)} work item materials")
