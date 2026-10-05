# Source of the category seed: generates CatalogSeed.Categories.cs.
TOP = [
 # n, slug, icon, hy, ru, en, children
 (1, "construction", "brick", "Շինարարություն", "Строительство", "Construction", [
  ("construction-turnkey-houses", "Տան կառուցում «բանալին ձեռքին»", "Строительство дома под ключ", "Turnkey house construction"),
  ("construction-foundations", "Հիմքի աշխատանքներ", "Фундаментные работы", "Foundations"),
  ("construction-masonry", "Որմնադրություն (տուֆ, բլոկ, աղյուս)", "Кладка (туф, блок, кирпич)", "Masonry (tuff, block, brick)"),
  ("construction-concrete", "Բետոնային աշխատանքներ", "Бетонные работы", "Concrete work"),
  ("construction-extensions", "Կցակառույցներ և հարկի ավելացում", "Пристройки и надстройки", "Extensions and added floors"),
  ("construction-outbuildings", "Ավտոտնակներ և օժանդակ շինություններ", "Гаражи и хозпостройки", "Garages and outbuildings"),
  ("construction-stairs", "Աստիճանների կառուցում", "Строительство лестниц", "Staircases"),
  ("construction-reinforcement", "Կառույցների ամրացում", "Усиление конструкций", "Structural strengthening"),
 ]),
 (2, "renovation", "roller", "Վերանորոգում և հարդարում", "Ремонт и отделка", "Renovation and finishing", [
  ("renovation-apartment", "Բնակարանի վերանորոգում «բանալին ձեռքին»", "Ремонт квартиры под ключ", "Turnkey apartment renovation"),
  ("renovation-cosmetic", "Կոսմետիկ վերանորոգում", "Косметический ремонт", "Cosmetic renovation"),
  ("renovation-plastering", "Սվաղում և ծեփում", "Штукатурка и шпаклёвка", "Plastering and skim coating"),
  ("renovation-painting", "Ներկարարական աշխատանքներ", "Малярные работы", "Painting"),
  ("renovation-wallpaper", "Պաստառապատում", "Поклейка обоев", "Wallpapering"),
  ("renovation-tiling", "Սալիկապատում", "Укладка плитки", "Tiling"),
  ("renovation-flooring", "Հատակի ծածկ (լամինատ, մանրահատակ)", "Напольные покрытия (ламинат, паркет)", "Flooring (laminate, parquet)"),
  ("renovation-screed", "Հատակի հարթեցում (ստյաժկա)", "Стяжка пола", "Floor screed"),
  ("renovation-ceilings", "Առաստաղներ (ձգվող, գիպսաստվարաթղթե)", "Потолки (натяжные, из гипсокартона)", "Ceilings (stretch, drywall)"),
  ("renovation-drywall", "Գիպսաստվարաթղթե աշխատանքներ", "Работы с гипсокартоном", "Drywall"),
  ("renovation-bathroom", "Լոգասենյակի վերանորոգում", "Ремонт ванной комнаты", "Bathroom renovation"),
  ("renovation-kitchen", "Խոհանոցի վերանորոգում", "Ремонт кухни", "Kitchen renovation"),
  ("renovation-decorative", "Դեկորատիվ ծեփ և պատերի հարդարում", "Декоративная штукатурка и отделка стен", "Decorative plaster and wall finishes"),
 ]),
 (3, "plumbing", "pipe", "Սանտեխնիկա", "Сантехника", "Plumbing", [
  ("plumbing-fixtures", "Սանտեխնիկայի տեղադրում", "Установка сантехники", "Fixture installation"),
  ("plumbing-pipes", "Ջրագիծ և կոյուղի", "Водопровод и канализация", "Water and sewer pipes"),
  ("plumbing-leaks", "Արտահոսքի վերացում", "Устранение протечек", "Leak repair"),
  ("plumbing-drain-cleaning", "Կոյուղու խցանման մաքրում", "Прочистка канализации", "Drain unclogging"),
  ("plumbing-water-heaters", "Ջրատաքացուցիչների տեղադրում", "Установка водонагревателей", "Water heater installation"),
  ("plumbing-pumps-filters", "Պոմպեր և ջրի ֆիլտրեր", "Насосы и фильтры для воды", "Pumps and water filters"),
  ("plumbing-meters", "Ջրաչափերի տեղադրում", "Установка счётчиков воды", "Water meter installation"),
 ]),
 (4, "heating", "flame", "Ջեռուցում", "Отопление", "Heating", [
  ("heating-boilers", "Գազի կաթսաների տեղադրում և սպասարկում", "Установка и обслуживание газовых котлов", "Gas boiler installation and service"),
  ("heating-radiators", "Ռադիատորների տեղադրում", "Установка радиаторов", "Radiator installation"),
  ("heating-underfloor", "Տաք հատակ", "Тёплый пол", "Underfloor heating"),
  ("heating-systems", "Ջեռուցման համակարգի մոնտաժ", "Монтаж системы отопления", "Heating system installation"),
  ("heating-gas-piping", "Ներքին գազատար", "Внутренний газопровод", "Indoor gas piping"),
  ("heating-fireplaces", "Բուխարիներ և վառարաններ", "Камины и печи", "Fireplaces and stoves"),
  ("heating-heat-pumps", "Ջերմային պոմպեր", "Тепловые насосы", "Heat pumps"),
 ]),
 (5, "electrical", "bolt", "Էլեկտրական աշխատանքներ", "Электромонтажные работы", "Electrical work", [
  ("electrical-wiring", "Էլեկտրալարերի անցկացում", "Электропроводка", "Wiring"),
  ("electrical-sockets", "Վարդակներ և անջատիչներ", "Розетки и выключатели", "Sockets and switches"),
  ("electrical-lighting", "Լուսավորության տեղադրում", "Монтаж освещения", "Lighting installation"),
  ("electrical-panels", "Էլեկտրական վահանակներ", "Электрощиты", "Electrical panels"),
  ("electrical-repair", "Էլեկտրական անսարքությունների վերացում", "Устранение неисправностей электрики", "Electrical fault repair"),
  ("electrical-solar", "Արևային վահանակներ", "Солнечные панели", "Solar panels"),
  ("electrical-generators", "Գեներատորներ և անխափան սնուցման սարքեր", "Генераторы и ИБП", "Generators and UPS"),
  ("electrical-ev-chargers", "Էլեկտրամեքենաների լիցքավորման կայաններ", "Зарядные станции для электромобилей", "EV chargers"),
 ]),
 (6, "exterior-cladding", "facade", "Ֆասադային աշխատանքներ", "Фасадные работы", "Facade works", [
  ("facade-stone", "Քարե երեսպատում (տուֆ, բազալտ)", "Облицовка камнем (туф, базальт)", "Stone cladding (tuff, basalt)"),
  ("facade-plaster", "Ֆասադի սվաղում և ներկում", "Штукатурка и покраска фасада", "Facade plastering and painting"),
  ("facade-ventilated", "Օդափոխվող ֆասադներ", "Вентилируемые фасады", "Ventilated facades"),
  ("facade-composite", "Կոմպոզիտային և սայդինգային երեսպատում", "Облицовка композитом и сайдингом", "Composite panels and siding"),
  ("facade-insulation", "Ֆասադի ջերմամեկուսացում", "Утепление фасада", "Facade insulation"),
  ("facade-restoration", "Ֆասադի վերականգնում", "Реставрация фасада", "Facade restoration"),
  ("facade-rope-access", "Արդյունաբերական ալպինիզմ", "Промышленный альпинизм", "Rope access work"),
 ]),
 (7, "cleaning", "sparkle", "Պրոֆեսիոնալ մաքրում", "Профессиональная уборка", "Professional cleaning", [
  ("cleaning-regular", "Կանոնավոր մաքրում", "Регулярная уборка", "Regular cleaning"),
  ("cleaning-deep", "Գլխավոր մաքրում", "Генеральная уборка", "Deep cleaning"),
  ("cleaning-after-renovation", "Վերանորոգումից հետո մաքրում", "Уборка после ремонта", "Post-renovation cleaning"),
  ("cleaning-windows", "Պատուհանների լվացում", "Мойка окон", "Window cleaning"),
  ("cleaning-upholstery", "Փափուկ կահույքի քիմմաքրում", "Химчистка мягкой мебели", "Upholstery cleaning"),
  ("cleaning-carpets", "Գորգերի լվացում", "Чистка ковров", "Carpet cleaning"),
  ("cleaning-offices", "Գրասենյակների մաքրում", "Уборка офисов", "Office cleaning"),
  ("cleaning-move", "Տեղափոխության մաքրում", "Уборка при переезде", "Move-in and move-out cleaning"),
  ("cleaning-facades", "Ֆասադների և վիտրաժների լվացում", "Мойка фасадов и витражей", "Facade and glass cleaning"),
  ("cleaning-disinfection", "Ախտահանում և վնասատուների ոչնչացում", "Дезинфекция и дезинсекция", "Disinfection and pest control"),
  ("cleaning-kitchens", "Խոհանոցի և կենցաղային տեխնիկայի մաքրում", "Чистка кухни и бытовой техники", "Kitchen and appliance cleaning"),
  ("cleaning-yard", "Բակի և տարածքի մաքրում", "Уборка двора и территории", "Yard and outdoor cleaning"),
 ]),
 (8, "roofing", "roof", "Տանիքային աշխատանքներ", "Кровельные работы", "Roofing", [
  ("roofing-new", "Նոր տանիքի կառուցում", "Монтаж новой кровли", "New roof installation"),
  ("roofing-repair", "Տանիքի վերանորոգում", "Ремонт кровли", "Roof repair"),
  ("roofing-metal", "Պրոֆնաստիլ և մետաղասալիկ", "Профнастил и металлочерепица", "Metal sheets and metal tiles"),
  ("roofing-flat", "Հարթ տանիքի ջրամեկուսացում", "Гидроизоляция плоской кровли", "Flat roof waterproofing"),
  ("roofing-gutters", "Ջրահեռացման համակարգեր", "Водостоки", "Gutters and downpipes"),
  ("roofing-attic", "Ձեղնահարկի կառուցում", "Мансарды", "Attic and loft conversion"),
 ]),
 (9, "windows-doors", "window", "Պատուհաններ և դռներ", "Окна и двери", "Windows and doors", [
  ("windows-pvc", "Մետաղապլաստե պատուհաններ", "Металлопластиковые окна", "uPVC windows"),
  ("windows-aluminium", "Ալյումինե պատուհաններ և վիտրաժներ", "Алюминиевые окна и витражи", "Aluminium windows and glazing"),
  ("windows-balcony", "Պատշգամբների ապակեպատում", "Остекление балконов", "Balcony glazing"),
  ("doors-interior", "Ներքին դռներ", "Межкомнатные двери", "Interior doors"),
  ("doors-entrance", "Մուտքի և մետաղական դռներ", "Входные и металлические двери", "Entrance and steel doors"),
  ("windows-repair", "Պատուհանների և դռների կարգավորում ու վերանորոգում", "Регулировка и ремонт окон и дверей", "Window and door repair"),
  ("windows-blinds", "Շերտավարագույրներ և ռոլետներ", "Жалюзи и рольставни", "Blinds and roller shutters"),
  ("doors-garage", "Ավտոտնակի դարպասներ", "Гаражные ворота", "Garage doors"),
 ]),
 (10, "hvac", "snowflake", "Օդափոխություն և օդորակում", "Вентиляция и кондиционирование", "Ventilation and air conditioning", [
  ("hvac-ac-installation", "Օդորակիչների տեղադրում", "Установка кондиционеров", "Air conditioner installation"),
  ("hvac-ac-service", "Օդորակիչների սպասարկում և վերանորոգում", "Обслуживание и ремонт кондиционеров", "Air conditioner service and repair"),
  ("hvac-ventilation", "Օդափոխության համակարգեր", "Системы вентиляции", "Ventilation systems"),
  ("hvac-hoods", "Խոհանոցային օդաքարշներ", "Кухонные вытяжки", "Kitchen hoods"),
  ("hvac-heat-recovery", "Ռեկուպերատորներ", "Рекуператоры", "Heat recovery ventilation"),
 ]),
 (11, "insulation", "layers", "Ջերմա- և ջրամեկուսացում", "Утепление и гидроизоляция", "Insulation and waterproofing", [
  ("insulation-thermal", "Պատերի և հատակի ջերմամեկուսացում", "Утепление стен и пола", "Wall and floor insulation"),
  ("insulation-roof", "Տանիքի ջերմամեկուսացում", "Утепление кровли", "Roof insulation"),
  ("insulation-waterproofing", "Ջրամեկուսացում (նկուղ, լոգասենյակ)", "Гидроизоляция (подвал, ванная)", "Waterproofing (basement, bathroom)"),
  ("insulation-sound", "Ձայնամեկուսացում", "Звукоизоляция", "Soundproofing"),
  ("insulation-spray-foam", "Պոլիուրեթանային փրփուրի փչում", "Напыление пенополиуретана", "Spray foam insulation"),
 ]),
 (12, "metalwork", "fence", "Մետաղական աշխատանքներ և եռակցում", "Металлоконструкции и сварка", "Metalwork and welding", [
  ("metalwork-railings", "Ճաղավանդակներ և բազրիքներ", "Ограждения и перила", "Railings and balustrades"),
  ("metalwork-gates-fences", "Դարպասներ և ցանկապատեր", "Ворота и заборы", "Gates and fences"),
  ("metalwork-structures", "Մետաղական կոնստրուկցիաներ", "Металлоконструкции", "Steel structures"),
  ("metalwork-canopies", "Ծածկեր և նավեսներ", "Навесы", "Canopies and carports"),
  ("metalwork-welding", "Եռակցման աշխատանքներ", "Сварочные работы", "Welding"),
  ("metalwork-forging", "Գեղարվեստական դարբնություն", "Художественная ковка", "Decorative forging"),
  ("metalwork-window-grilles", "Պատուհանների վանդակաճաղեր", "Решётки на окна", "Window grilles"),
 ]),
 (13, "carpentry", "hammer", "Ատաղձագործություն և կահույք", "Столярные работы и мебель", "Carpentry and furniture", [
  ("carpentry-kitchens", "Խոհանոցային կահույք պատվերով", "Кухни на заказ", "Custom kitchens"),
  ("carpentry-wardrobes", "Զգեստապահարաններ և ներկառուցված կահույք", "Шкафы и встроенная мебель", "Wardrobes and built-in furniture"),
  ("carpentry-assembly", "Կահույքի հավաքում", "Сборка мебели", "Furniture assembly"),
  ("carpentry-furniture-repair", "Կահույքի վերանորոգում և վերապաստառապատում", "Ремонт и перетяжка мебели", "Furniture repair and reupholstery"),
  ("carpentry-wooden-stairs", "Փայտե աստիճաններ", "Деревянные лестницы", "Wooden stairs"),
  ("carpentry-wooden-structures", "Փայտե կառույցներ (տաղավարներ, տեռասներ)", "Деревянные конструкции (беседки, террасы)", "Wooden structures (gazebos, decks)"),
  ("carpentry-countertops", "Սեղանի մակերեսներ (քար, փայտ)", "Столешницы (камень, дерево)", "Countertops (stone, wood)"),
 ]),
 (14, "earthworks", "shovel", "Հողային աշխատանքներ և քանդում", "Земляные работы и демонтаж", "Earthworks and demolition", [
  ("earthworks-excavation", "Փորման աշխատանքներ", "Земляные работы и котлованы", "Excavation"),
  ("earthworks-demolition", "Քանդման աշխատանքներ", "Демонтажные работы", "Demolition"),
  ("earthworks-waste-removal", "Շինարարական աղբի դուրսբերում", "Вывоз строительного мусора", "Construction waste removal"),
  ("earthworks-concrete-cutting", "Բետոնի կտրում և ալմաստե հորատում", "Резка бетона и алмазное бурение", "Concrete cutting and core drilling"),
  ("earthworks-equipment", "Շինտեխնիկայի վարձույթ", "Аренда спецтехники", "Construction equipment rental"),
  ("earthworks-septic", "Սեպտիկներ և դրենաժ", "Септики и дренаж", "Septic tanks and drainage"),
  ("earthworks-wells", "Ջրհորների հորատում", "Бурение скважин", "Well drilling"),
 ]),
 (15, "landscaping", "leaf", "Բակի և այգու բարեկարգում", "Благоустройство двора и сада", "Landscaping and garden", [
  ("landscaping-design", "Լանդշաֆտային դիզայն", "Ландшафтный дизайн", "Landscape design"),
  ("landscaping-paving", "Սալահատակում", "Укладка тротуарной плитки", "Paving"),
  ("landscaping-lawns", "Սիզամարգեր և կանաչապատում", "Газоны и озеленение", "Lawns and planting"),
  ("landscaping-irrigation", "Ոռոգման համակարգեր", "Системы полива", "Irrigation systems"),
  ("landscaping-trees", "Ծառերի էտ և հեռացում", "Обрезка и удаление деревьев", "Tree pruning and removal"),
  ("landscaping-pools", "Լողավազաններ", "Бассейны", "Swimming pools"),
  ("landscaping-retaining-walls", "Հենապատեր", "Подпорные стены", "Retaining walls"),
 ]),
 (16, "security-systems", "shield", "Անվտանգության համակարգեր և խելացի տուն", "Системы безопасности и умный дом", "Security and smart home", [
  ("security-cctv", "Տեսահսկման համակարգեր", "Видеонаблюдение", "CCTV"),
  ("security-alarms", "Ազդանշանային համակարգեր", "Охранная сигнализация", "Alarm systems"),
  ("security-intercoms", "Դոմոֆոններ", "Домофоны", "Intercoms"),
  ("security-access-control", "Մուտքի վերահսկման համակարգեր", "Системы контроля доступа", "Access control"),
  ("security-smart-home", "Խելացի տուն", "Умный дом", "Smart home"),
  ("security-networks", "Համակարգչային ցանցեր և ինտերնետ", "Компьютерные сети и интернет", "Network and internet cabling"),
  ("security-fire-alarms", "Հրդեհային ազդանշանային համակարգեր", "Пожарная сигнализация", "Fire alarm systems"),
 ]),
 (17, "design-engineering", "file", "Նախագծում և դիզայն", "Проектирование и дизайн", "Design and engineering", [
  ("design-architecture", "Ճարտարապետական նախագծում", "Архитектурное проектирование", "Architectural design"),
  ("design-interior", "Ինտերիերի դիզայն", "Дизайн интерьера", "Interior design"),
  ("design-structural", "Կոնստրուկտիվ նախագծում", "Конструктивное проектирование", "Structural engineering"),
  ("design-building-services", "Ինժեներական համակարգերի նախագծում", "Проектирование инженерных систем", "Building services design"),
  ("design-estimates", "Նախահաշիվներ", "Сметы", "Cost estimates"),
  ("design-supervision", "Տեխնիկական հսկողություն", "Технический надзор", "Construction supervision"),
  ("design-surveys", "Չափագրում և հետազննում", "Обмеры и обследование", "Surveys and measurements"),
 ]),
 (18, "handyman", "tools", "Մանր տնային վերանորոգում", "Мелкий бытовой ремонт", "Handyman and small repairs", [
  ("handyman-repairs", "Մանր վերանորոգման աշխատանքներ", "Мелкие ремонтные работы", "Small repairs"),
  ("handyman-mounting", "Կախում և ամրացում (հեռուստացույց, դարակներ, վարագույրներ)", "Навеска (телевизор, полки, карнизы)", "Mounting (TV, shelves, curtain rails)"),
  ("handyman-appliances", "Կենցաղային տեխնիկայի միացում", "Подключение бытовой техники", "Appliance installation"),
  ("handyman-locks", "Կողպեքների տեղադրում և փոխարինում", "Установка и замена замков", "Lock installation and replacement"),
  ("handyman-sealing", "Հերմետիկացում", "Герметизация швов", "Sealing and caulking"),
 ]),
]

import re
slugs = set()
def cs(s): return s.replace('\\', '\\\\').replace('"', '\\"')
from catalog_i18n import CATEGORIES as I18N

def more(slug):
    """Arabic, Persian and Hindi names as extra Text arguments."""
    return "".join(f', "{cs(x)}"' for x in I18N[slug])

out = []
out.append('''using HomeServices.Domain.Localization;

namespace HomeServices.Infrastructure.Persistence.Configurations;

// Seed for the service catalog: main categories and their subcategories. Staff manage them in the Back Office later.
internal static partial class CatalogSeed
{
    /// <summary>Main service categories (construction and cleaning). The first seven ids are from the launch catalog.</summary>
    internal static readonly (Guid Id, string Slug, string Icon, LocalizedText Name)[] Categories =
    [''')
for n, slug, icon, hy, ru, en, kids in TOP:
    assert slug not in slugs; slugs.add(slug)
    out.append(f'        (new("019a0000-0000-7000-8000-{n+100:012d}"), "{slug}", "{icon}", Text("{cs(hy)}", "{cs(ru)}", "{cs(en)}"{more(slug)})),')
out.append('''    ];

    /// <summary>Subcategories, each under one main category. Ids are "...0001PPCC" (parent PP, child CC).</summary>
    internal static readonly (Guid Id, Guid ParentId, string Slug, LocalizedText Name)[] Subcategories =
    [''')
total = 0
for n, slug, icon, hy, ru, en, kids in TOP:
    parent = f'019a0000-0000-7000-8000-{n+100:012d}'
    out.append(f'        // {en}')
    for i, (s, h, r, e) in enumerate(kids, 1):
        assert re.fullmatch(r'[a-z0-9]+(-[a-z0-9]+)*', s) and len(s) <= 64, s
        assert s not in slugs, s; slugs.add(s)
        out.append(f'        (new("019a0000-0000-7000-8000-00000001{n:02d}{i:02d}"), new("{parent}"), "{s}", Text("{cs(h)}", "{cs(r)}", "{cs(e)}"{more(s)})),')
        total += 1
out.append('''    ];
}
''')
import sys
open(sys.argv[1], 'w', encoding='utf-8', newline='\n').write('\n'.join(out))
print(len(TOP), total)
