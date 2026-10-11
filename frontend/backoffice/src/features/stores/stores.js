import dayjs from 'dayjs'
import { cleanNames } from '@/features/catalog/names'

export const GRADES = ['Budget', 'Standard', 'Premium']
export const GRADE_COLORS = { Budget: 'default', Standard: 'blue', Premium: 'gold' }
export const STORE_STATUSES = ['all', 'active', 'hidden']
export const MAX_BRANCHES = 50
export const PRICE_LIMIT = 10_000_000

/** True for an http or https address. */
export function isWebAddress(value) {
  try {
    const { protocol } = new URL(String(value).trim())
    return protocol === 'http:' || protocol === 'https:'
  } catch {
    return false
  }
}

const orNull = (text) => {
  const trimmed = (text ?? '').trim()
  return trimmed.length > 0 ? trimmed : null
}

/** A store form's values as the API takes them. */
export function toStoreBody(values) {
  return {
    slug: values.slug.trim(),
    name: values.name.trim(),
    description: cleanNames(values.description ?? {}),
    website: orNull(values.website),
    phone: orNull(values.phone),
    logoFileId: values.logo?.fileId ?? null,
    sortOrder: values.sortOrder ?? 0,
    isActive: Boolean(values.isActive),
  }
}

/** A store as the form edits it. */
export const toStoreValues = (store) =>
  store
    ? {
        slug: store.slug,
        name: store.name,
        description: store.description ?? {},
        website: store.website ?? '',
        phone: store.phone ?? '',
        logo: store.logoFileId ? { fileId: store.logoFileId, url: store.logoUrl } : null,
        sortOrder: store.sortOrder,
        isActive: store.isActive,
      }
    : { slug: '', name: '', description: {}, website: '', phone: '', logo: null, sortOrder: 0, isActive: true }

export const blankBranch = () => ({ name: '', cityId: undefined, districtId: undefined, address: '', phone: '', openingHours: '' })

/** Saved branches as form rows. */
export const toBranchRows = (branches = []) =>
  branches.map((branch) => ({
    name: branch.name,
    cityId: branch.cityId,
    districtId: branch.districtId ?? undefined,
    address: branch.address,
    phone: branch.phone ?? '',
    openingHours: branch.openingHours ?? '',
  }))

/** Form rows as the API takes them. */
export const toBranchInputs = (rows = []) =>
  rows.map((row) => ({
    name: row.name.trim(),
    cityId: row.cityId,
    districtId: row.districtId ?? null,
    address: row.address.trim(),
    phone: orNull(row.phone),
    openingHours: orNull(row.openingHours),
  }))

/** Towns for a select, grouped by region; towns without a region go last under `otherLabel`. */
export function townOptions(cities = [], regions = [], otherLabel = '') {
  const option = (city) => ({ value: city.id, label: city.name })
  const groups = regions
    .map((region) => ({ label: region.name, options: cities.filter((city) => city.regionId === region.id).map(option) }))
    .filter((group) => group.options.length > 0)
  const loose = cities.filter((city) => !regions.some((region) => region.id === city.regionId))
  return loose.length > 0 ? [...groups, { label: otherLabel, options: loose.map(option) }] : groups
}

/** What's wrong with a product's prices (an i18n key), or null: a sale needs both fields and a price below the usual one. */
export function productPriceProblem({ price, salePrice, saleEndsOn }) {
  const hasSale = salePrice != null
  if (hasSale !== Boolean(saleEndsOn)) return 'stores.product.saleNeedsBoth'
  if (hasSale && price != null && salePrice >= price) return 'stores.product.saleTooHigh'
  return null
}

/** A product form's values as the API takes them. */
export function toProductBody(values) {
  const linked = Boolean(values.materialId)
  return {
    name: values.name.trim(),
    brand: orNull(values.brand),
    code: orNull(values.code),
    packSize: values.packSize,
    packLabel: orNull(values.packLabel),
    price: values.price,
    salePrice: values.salePrice ?? null,
    saleEndsOn: values.saleEndsOn ? values.saleEndsOn.format('YYYY-MM-DD') : null,
    inStock: Boolean(values.inStock),
    photoFileId: values.photo?.fileId ?? null,
    materialId: linked ? values.materialId : null,
    grade: linked ? values.grade : null,
    isActive: Boolean(values.isActive),
  }
}

/** A product as the form edits it (null: a new product). */
export const toProductValues = (product) =>
  product
    ? {
        name: product.name,
        brand: product.brand ?? '',
        code: product.code ?? '',
        packSize: product.packSize,
        packLabel: product.packLabel ?? '',
        price: product.price,
        salePrice: product.salePrice ?? undefined,
        saleEndsOn: product.saleEndsOn ? dayjs(product.saleEndsOn) : null,
        inStock: product.inStock,
        photo: product.photoFileId ? { fileId: product.photoFileId, url: product.photoUrl } : null,
        materialId: product.materialId ?? undefined,
        grade: product.grade ?? undefined,
        isActive: product.isActive,
      }
    : { name: '', brand: '', code: '', packLabel: '', saleEndsOn: null, photo: null, inStock: true, isActive: true }
