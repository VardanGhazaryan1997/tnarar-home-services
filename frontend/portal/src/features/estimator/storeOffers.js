import { hasMaterials } from './materials'

/** How many stores the "Where to buy" box lists. */
export const TOP_STORES = 3

/** The request for where to buy what a measurement needs (`{ grade, materials }`), or null without materials. */
export function whereToBuyRequest(measurement) {
  if (!hasMaterials(measurement)) return null
  return {
    grade: measurement.materialGrade,
    materials: measurement.materials.map((material) => ({ materialId: material.materialId, quantity: material.quantity })),
  }
}

/** The cheapest offer of a material (by its id), or undefined when no store sells it. */
export const cheapestOf = (data, materialId) => data?.cheapest?.find((offer) => offer.materialId === materialId)

/** The best stores to show: the server already puts the ones with most materials first, then the cheapest. */
export const topStores = (data) => (data?.stores ?? []).slice(0, TOP_STORES)
