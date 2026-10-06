import { formatMoney } from '@/i18n/format'
import { localizedName } from './names'

/** Units a work item is priced per, in the order staff pick them. */
export const UNITS = ['SquareMeter', 'RunningMeter', 'Piece', 'Point', 'CubicMeter', 'Hour', 'Fixed']

/** Room surfaces (for working out quantities from room sizes). */
export const SURFACES = ['None', 'Floor', 'Wall', 'Ceiling']

/** True when the item has a price range. */
export const hasPrice = (item) => item.priceTypical != null

/** "2,500 – 5,000 ֏" (min – max), or null without prices. */
export function priceRangeText(item, language) {
  if (!hasPrice(item)) return null
  return item.priceMin === item.priceMax
    ? formatMoney(item.priceTypical, language)
    : `${formatMoney(item.priceMin, language).replace(' ֏', '')} – ${formatMoney(item.priceMax, language)}`
}

/** The market range partners' prices give ("10,000 – 20,000 ֏"), or null without one. */
export const marketRangeText = (item, language) =>
  item.marketTypical == null ? null : priceRangeText({ priceMin: item.marketMin, priceTypical: item.marketTypical, priceMax: item.marketMax }, language)

/**
 * Subcategories grouped under their main categories, for a select: [{ label: main, options: [{ value, label }] }].
 * Hidden categories are marked, so staff notice items that customers can't see.
 */
export function subcategoryGroups(categories, nameOf, hiddenLabel) {
  const label = (category) => (category.isActive ? nameOf(category) : `${nameOf(category)} (${hiddenLabel})`)
  return categories
    .filter((main) => main.children?.length)
    .map((main) => ({
      label: label(main),
      title: nameOf(main),
      options: main.children.map((sub) => ({ value: sub.id, label: label(sub) })),
    }))
}

/** Maps every category id to { name, main } (main = the main category's name, for subcategories). */
export function categoryLookup(categories, lng, defaultLng) {
  const lookup = new Map()
  for (const main of categories) {
    const mainName = localizedName(main.name, lng, defaultLng)
    lookup.set(main.id, { name: mainName, main: null, ids: [main.id, ...(main.children ?? []).map((c) => c.id)] })
    for (const sub of main.children ?? []) {
      lookup.set(sub.id, { name: localizedName(sub.name, lng, defaultLng), main: mainName, ids: [sub.id] })
    }
  }
  return lookup
}

/** Keeps the items matching the category (a main category includes its subcategories), status and text. */
export function filterWorkItems(items, { categoryIds, status, search }) {
  const term = search.trim().toLocaleLowerCase()
  return items.filter((item) => {
    if (categoryIds && !categoryIds.includes(item.categoryId)) return false
    if (status === 'active' && !item.isActive) return false
    if (status === 'hidden' && item.isActive) return false
    if (status === 'unpriced' && hasPrice(item)) return false
    if (!term) return true
    return item.slug.includes(term) || Object.values(item.name).some((name) => name.toLocaleLowerCase().includes(term))
  })
}

/** All three prices or none, in order: min ≤ typical ≤ max. Returns an error key or null. */
export function priceProblem({ priceMin, priceTypical, priceMax }) {
  const given = [priceMin, priceTypical, priceMax].filter((value) => value != null)
  if (given.length === 0) return null
  if (given.length < 3) return 'catalog.workItems.priceIncomplete'
  if (!(priceMin <= priceTypical && priceTypical <= priceMax)) return 'catalog.workItems.priceOrder'
  return null
}
