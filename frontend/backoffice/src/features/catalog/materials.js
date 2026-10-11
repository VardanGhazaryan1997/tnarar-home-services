/** Units a material is priced per (backend MaterialUnit). */
export const MATERIAL_UNITS = ['Kilogram', 'Liter', 'SquareMeter', 'RunningMeter', 'CubicMeter', 'Piece']

/** Price fields of a material, cheapest grade first. */
export const MATERIAL_PRICES = ['priceBudget', 'priceStandard', 'pricePremium']

export const MATERIAL_PRICE_LIMIT = 10_000_000
export const MAX_QUANTITY_PER_UNIT = 1000
export const MAX_WASTE = 100
export const MAX_MATERIALS_PER_ITEM = 30

/** What's wrong with a material's prices (an i18n key), or null: all 1+, budget ≤ standard ≤ premium. */
export function materialPriceProblem({ priceBudget, priceStandard, pricePremium }) {
  const prices = [priceBudget, priceStandard, pricePremium]
  if (prices.some((price) => price == null)) return 'catalog.materials.pricesRequired'
  if (prices.some((price) => price < 1 || price > MATERIAL_PRICE_LIMIT)) return 'catalog.materials.priceInvalid'
  if (priceBudget > priceStandard || priceStandard > pricePremium) return 'catalog.materials.priceOrder'
  return null
}

/**
 * Materials matching the text in the slug or any name, and the status ("all", "active", "hidden", "unused",
 * "outdated": prices not checked for 90 days).
 */
export function filterMaterials(materials, { search = '', status = 'all' } = {}) {
  const term = search.trim().toLocaleLowerCase()
  return materials.filter((material) => {
    if (status === 'active' && !material.isActive) return false
    if (status === 'hidden' && material.isActive) return false
    if (status === 'unused' && material.workItemCount > 0) return false
    if (status === 'outdated' && !material.pricesNeedChecking) return false
    return !term || [material.slug, ...Object.values(material.name ?? {})].some((text) => text?.toLocaleLowerCase().includes(term))
  })
}

/** A work item's materials from the API as editable lines. */
export const toMaterialLines = (items = []) =>
  items.map((item) => ({ materialId: item.materialId, quantityPerUnit: item.quantityPerUnit, wastePercent: item.wastePercent ?? 0 }))

/** Lines as the API takes them. */
export const toMaterialInputs = (lines) =>
  lines.map((line) => ({ materialId: line.materialId, quantityPerUnit: line.quantityPerUnit, wastePercent: line.wastePercent ?? 0 }))

/** What's wrong with a line (an i18n key), or null. */
export function materialLineProblem(line) {
  if (line.quantityPerUnit == null || line.quantityPerUnit <= 0 || line.quantityPerUnit > MAX_QUANTITY_PER_UNIT) {
    return 'catalog.workItemMaterials.amountInvalid'
  }
  if (line.wastePercent == null || line.wastePercent < 0 || line.wastePercent > MAX_WASTE) return 'catalog.workItemMaterials.wasteInvalid'
  return null
}

/** How much material `quantity` units of work use, waste included. */
export const amountFor = (line, quantity) => (quantity * line.quantityPerUnit * (100 + (line.wastePercent ?? 0))) / 100
