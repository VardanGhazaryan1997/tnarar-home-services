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
