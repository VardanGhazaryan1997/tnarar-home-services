/** Catalog data as GET /admin/catalog/categories and /cities return it. */
export const CATEGORIES = [
  {
    id: 'cat-renovation',
    slug: 'renovation',
    name: { hy: 'Վերանորոգում', ru: 'Ремонт', en: 'Renovation' },
    icon: 'brush',
    parentId: null,
    sortOrder: 1,
    isActive: true,
    children: [
      {
        id: 'cat-tiling',
        slug: 'tiling',
        name: { hy: 'Սալիկապատում', en: 'Tiling' },
        icon: null,
        parentId: 'cat-renovation',
        sortOrder: 1,
        isActive: false,
        children: [],
      },
    ],
  },
  {
    id: 'cat-plumbing',
    slug: 'plumbing',
    name: { hy: 'Սանտեխնիկա', en: 'Plumbing', fr: 'Plomberie' },
    icon: 'pipe',
    parentId: null,
    sortOrder: 2,
    isActive: true,
    children: [],
  },
]

export const REGIONS = [
  { id: 'region-yerevan', slug: 'yerevan', name: { hy: 'Երևան', en: 'Yerevan' }, sortOrder: 1 },
  { id: 'region-ararat', slug: 'ararat', name: { hy: 'Արարատ', en: 'Ararat' }, sortOrder: 2 },
]

export const CITIES = [
  {
    id: 'city-yerevan',
    slug: 'yerevan',
    name: { hy: 'Երևան', en: 'Yerevan' },
    sortOrder: 1,
    isActive: true,
    regionId: 'region-yerevan',
    kind: 'City',
    districts: [
      { id: 'district-kentron', cityId: 'city-yerevan', slug: 'kentron', name: { hy: 'Կենտրոն', en: 'Kentron' }, sortOrder: 1, isActive: true },
    ],
  },
  { id: 'city-masis', slug: 'masis', name: { hy: 'Մասիս' }, sortOrder: 2, isActive: false, regionId: 'region-ararat', kind: 'City', districts: [] },
]

/** Work items as GET /admin/catalog/work-items returns them (in catalog order). */
export const WORK_ITEMS = [
  {
    id: 'wi-plastering',
    categoryId: 'cat-tiling',
    slug: 'wall-plastering',
    name: { hy: 'Պատերի սվաղում', en: 'Wall plastering', ar: 'لياسة الجدران' },
    unit: 'SquareMeter',
    surface: 'Wall',
    sortOrder: 1,
    isActive: true,
    priceMin: 2500,
    priceTypical: 3500,
    priceMax: 5000,
    isPriceLocked: true,
    partnerCount: 4,
  },
  {
    id: 'wi-floor-tiling',
    categoryId: 'cat-tiling',
    slug: 'floor-tiling',
    name: { hy: 'Հատակի սալիկապատում', en: 'Floor tiling' },
    unit: 'SquareMeter',
    surface: 'Floor',
    sortOrder: 2,
    isActive: false,
    priceMin: 5000,
    priceTypical: 6500,
    priceMax: 9000,
    isPriceLocked: false,
    partnerCount: 6,
    marketMin: 6000,
    marketTypical: 7000,
    marketMax: 8000,
    marketSource: 'Partners',
    marketPartnerCount: 6,
  },
  {
    id: 'wi-leaks',
    categoryId: 'cat-plumbing',
    slug: 'leak-repair',
    name: { hy: 'Արտահոսքի վերացում', en: 'Leak repair' },
    unit: 'Fixed',
    surface: 'None',
    sortOrder: 1,
    isActive: true,
    priceMin: null,
    priceTypical: null,
    priceMax: null,
    isPriceLocked: false,
  },
]
