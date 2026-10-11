import { grandTotal, hasMaterials } from './materials'

describe('estimate materials', () => {
  it('adds materials to the labour totals', () => {
    expect(grandTotal({ totalMin: 10, totalTypical: 20, totalMax: 30, materialsTotal: 5 })).toEqual({ min: 15, typical: 25, max: 35 })
    expect(grandTotal({ totalMin: 10, totalTypical: 20, totalMax: 30 })).toEqual({ min: 10, typical: 20, max: 30 })
    expect(grandTotal(null)).toEqual({ min: 0, typical: 0, max: 0 })
  })

  it('knows when materials were counted', () => {
    expect(hasMaterials({ materialGrade: 'Standard', materials: [{}] })).toBe(true)
    expect(hasMaterials({ materialGrade: 'Standard', materials: [] })).toBe(false)
    expect(hasMaterials({ materials: [{}] })).toBe(false)
  })
})
