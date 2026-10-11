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
