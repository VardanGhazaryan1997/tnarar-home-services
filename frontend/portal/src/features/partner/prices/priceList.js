/** The highest price the API accepts (100 million drams). */
export const PRICE_LIMIT = 100_000_000

const EMPTY_ROW = { from: '', to: '', materials: false }

/** "12 000", "12,000" or "12000" → 12000; blank → null; anything else → NaN. */
export function parsePrice(text) {
  const cleaned = String(text ?? '').replace(/[\s,.  ]/g, '')
  if (cleaned === '') return null
  return /^\d+$/.test(cleaned) ? Number(cleaned) : Number.NaN
}

/** The editable rows for the items: { [workItemId]: { from, to, materials } } as text fields. */
export function draftFrom(items) {
  return Object.fromEntries(
    items.map((item) => [
      item.workItemId,
      {
        from: item.priceFrom == null ? '' : String(item.priceFrom),
        to: item.priceTo == null ? '' : String(item.priceTo),
        materials: Boolean(item.includesMaterials),
      },
    ]),
  )
}

export const rowOf = (draft, id) => draft[id] ?? EMPTY_ROW

/** What's wrong with a row (an i18n key), or null. An empty row is fine: the item has no price. */
export function rowError(row) {
  const from = parsePrice(row.from)
  const to = parsePrice(row.to)
  if (from === null) return to === null ? null : 'partner.prices.errors.fromRequired'
  if (Number.isNaN(from) || from < 1 || (to !== null && Number.isNaN(to))) return 'partner.prices.errors.invalid'
  if (from > PRICE_LIMIT || (to !== null && to > PRICE_LIMIT)) return 'partner.prices.errors.tooHigh'
  if (to !== null && to < from) return 'partner.prices.errors.toBelowFrom'
  return null
}

/** { [workItemId]: errorKey } for the rows with a problem. */
export function draftErrors(draft) {
  return Object.fromEntries(
    Object.entries(draft)
      .map(([id, row]) => [id, rowError(row)])
      .filter(([, error]) => error),
  )
}

/** The rows with a price, as the API takes them. A "to" equal to "from" is a single price. */
export function toPayload(draft) {
  return Object.entries(draft)
    .filter(([, row]) => parsePrice(row.from) !== null)
    .map(([workItemId, row]) => {
      const priceFrom = parsePrice(row.from)
      const priceTo = parsePrice(row.to)
      return { workItemId, priceFrom, priceTo: priceTo === priceFrom ? null : priceTo, includesMaterials: row.materials }
    })
}

/** True when the draft would save something different from the items' current prices. */
export function isChanged(draft, items) {
  return JSON.stringify(toPayload(draft)) !== JSON.stringify(toPayload(draftFrom(items)))
}

/** How many rows have a price. */
export const pricedCount = (draft) => Object.values(draft).filter((row) => parsePrice(row.from) !== null).length

/** Fills every empty row that has a usual market price with it. Returns the new draft. */
export function fillWithTypical(draft, items) {
  const next = { ...draft }
  for (const item of items) {
    const row = rowOf(draft, item.workItemId)
    if (item.marketTypical != null && row.from.trim() === '' && row.to.trim() === '') {
      next[item.workItemId] = { ...row, from: String(item.marketTypical), to: '' }
    }
  }
  return next
}

/**
 * The items as main category → subcategory → items, keeping the API's order. `search` keeps items whose name, slug or
 * category matches.
 */
export function groupItems(items, search = '') {
  const term = search.trim().toLocaleLowerCase()
  const matches = (item) =>
    !term ||
    [item.name, item.slug, item.categoryName, item.mainCategoryName].some((text) => text?.toLocaleLowerCase().includes(term))

  const groups = []
  for (const item of items.filter(matches)) {
    let main = groups.find((group) => group.id === item.mainCategoryId)
    if (!main) {
      main = { id: item.mainCategoryId, name: item.mainCategoryName, subcategories: [] }
      groups.push(main)
    }
    let sub = main.subcategories.find((group) => group.id === item.categoryId)
    if (!sub) {
      sub = { id: item.categoryId, name: item.categoryName, items: [] }
      main.subcategories.push(sub)
    }
    sub.items.push(item)
  }
  return groups
}
