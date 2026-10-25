export const GRADES = ['Budget', 'Standard', 'Premium']

/** Materials whose name or slug contains `search` (ignoring case and surrounding spaces); all of them for an empty search. */
export function filterMaterials(materials = [], search = '') {
  const text = search.trim().toLowerCase()
  if (!text) return materials
  return materials.filter((material) => material.name.toLowerCase().includes(text) || material.slug.includes(text))
}

/** The distinct towns of some branches, in order: "Yerevan, Gyumri". */
export function branchPlaces(branches = []) {
  return [...new Set(branches.map((branch) => branch.city))].join(', ')
}

/** The id of the cheapest product in stock per unit (null when none is in stock); offers are `{ product }` objects. */
export function cheapestInStock(offers = []) {
  const inStock = offers.filter((offer) => offer.product.inStock)
  if (inStock.length === 0) return null
  return inStock.reduce((best, offer) => (offer.product.unitPrice < best.product.unitPrice ? offer : best)).product.id
}

export const SORTS = ['unit', 'pack', 'updated']

/** The distinct non-empty brands of some offers, sorted ignoring case (the first spelling of each is kept). */
export function brandsOf(offers = []) {
  const seen = new Map()
  for (const { product } of offers) {
    const brand = product.brand?.trim()
    if (brand && !seen.has(brand.toLowerCase())) seen.set(brand.toLowerCase(), brand)
  }
  return [...seen.values()].sort((a, b) => a.localeCompare(b, undefined, { sensitivity: 'base' }))
}

/** Only the offers of one brand (ignoring case); all of them without a brand. */
export function filterOffers(offers = [], { brand = '' } = {}) {
  if (!brand) return offers
  return offers.filter((offer) => offer.product.brand?.trim().toLowerCase() === brand.toLowerCase())
}

/** Offers in another order: `pack` = current pack price, lowest first; `updated` = most recently checked first; `unit` keeps the server order. */
export function sortOffers(offers = [], sort = 'unit') {
  if (sort === 'pack') return [...offers].sort((a, b) => a.product.currentPrice - b.product.currentPrice)
  if (sort === 'updated') return [...offers].sort((a, b) => new Date(b.product.priceUpdatedAt) - new Date(a.product.priceUpdatedAt))
  return offers
}
