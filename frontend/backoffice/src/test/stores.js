// Stores as the admin API returns them (E2). Money is whole drams; unit prices can be fractional.

export const store = (overrides = {}) => ({
  id: 'store-demo',
  slug: 'demo-hardware',
  name: 'Demo Hardware',
  description: { hy: 'Շինանյութեր', en: 'Building materials' },
  website: 'https://demo-hardware.example',
  phone: '+37411000000',
  logoFileId: null,
  logoUrl: null,
  sortOrder: 1,
  isActive: true,
  branchCount: 1,
  productCount: 2,
  linkedCount: 1,
  staleCount: 1,
  ...overrides,
})

export const STORES = [
  store(),
  store({ id: 'store-hidden', slug: 'hidden-shop', name: 'Hidden Shop', isActive: false, branchCount: 0, productCount: 0, linkedCount: 0, staleCount: 0, website: null, phone: null }),
]

export const branch = (overrides = {}) => ({
  id: 'branch-1',
  name: 'Kentron branch',
  cityId: 'city-yerevan',
  districtId: 'district-kentron',
  address: '1 Demo street',
  phone: '+37411000001',
  openingHours: '09:00–18:00',
  ...overrides,
})

/** GET /admin/stores/{id}: the store with its branches. */
export const storeDetail = (overrides = {}) => ({ ...store(), branches: [branch()], ...overrides })

export const product = (overrides = {}) => ({
  id: 'product-putty',
  storeId: 'store-demo',
  materialId: 'mat-putty',
  materialName: 'Finishing putty',
  unit: 'Kilogram',
  grade: 'Standard',
  name: 'Putty 25 kg',
  brand: 'DemoBrand',
  code: 'P-25',
  packSize: 25,
  packLabel: '25 kg bag',
  price: 5000,
  salePrice: null,
  saleEndsOn: null,
  saleActive: false,
  currentPrice: 5000,
  unitPrice: 200,
  inStock: true,
  photoFileId: null,
  photoUrl: null,
  isActive: true,
  priceUpdatedAt: '2026-10-05T09:00:00Z',
  isStale: false,
  ...overrides,
})

export const PRODUCTS = [
  product(),
  product({
    id: 'product-paint',
    materialId: null,
    materialName: null,
    unit: null,
    grade: null,
    name: 'Paint 10 L',
    brand: null,
    code: null,
    packSize: 10,
    packLabel: null,
    price: 9000,
    salePrice: 7500,
    saleEndsOn: '2026-11-01',
    saleActive: true,
    currentPrice: 7500,
    unitPrice: null,
    inStock: false,
    isStale: true,
    priceUpdatedAt: '2026-06-01T09:00:00Z',
  }),
]

/** GET /admin/stores/{id}/products */
export const productsPage = (items = PRODUCTS, overrides = {}) => ({ items, page: 1, pageSize: 25, totalCount: items.length, ...overrides })

/** GET /admin/stores/materials: what a product can be linked to. */
export const LINKABLE_MATERIALS = [
  { id: 'mat-putty', slug: 'finishing-putty', name: { hy: 'Վերջնական շպաքլ', en: 'Finishing putty' }, unit: 'Kilogram', isActive: true },
  { id: 'mat-primer', slug: 'primer', name: { hy: 'Գրունտ', en: 'Primer' }, unit: 'Liter', isActive: true },
]

/** GET /admin/catalog/materials/{id}/store-products */
export const MATERIAL_OFFERS = [
  { storeId: 'store-demo', storeName: 'Demo Hardware', storeIsActive: true, product: product() },
  { storeId: 'store-hidden', storeName: 'Hidden Shop', storeIsActive: false, product: product({ id: 'product-other', storeId: 'store-hidden', unitPrice: 12.5, currentPrice: 300, isStale: true }) },
]
