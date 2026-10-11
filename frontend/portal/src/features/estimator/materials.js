/** Grades of materials an estimate can be priced with (backend MaterialGrade); none = labour only. */
export const MATERIAL_GRADES = ['Budget', 'Standard', 'Premium']

/**
 * The estimate's totals with materials added (the same amount at every end of the range, since materials are priced
 * per grade, not as a range). Without materials the labour totals.
 */
export function grandTotal(measurement) {
  const materials = measurement?.materialsTotal ?? 0
  return {
    min: (measurement?.totalMin ?? 0) + materials,
    typical: (measurement?.totalTypical ?? 0) + materials,
    max: (measurement?.totalMax ?? 0) + materials,
  }
}

/** True when the measurement counted materials and found some. */
export const hasMaterials = (measurement) => Boolean(measurement?.materialGrade && measurement.materials?.length)
