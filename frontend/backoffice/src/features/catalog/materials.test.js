import { amountFor, filterMaterials, materialLineProblem, materialPriceProblem, toMaterialInputs, toMaterialLines } from './materials'

describe('material helpers', () => {
  it('checks material prices', () => {
    expect(materialPriceProblem({ priceBudget: 1, priceStandard: 2, pricePremium: 2 })).toBeNull()
    expect(materialPriceProblem({ priceBudget: 1, priceStandard: null, pricePremium: 2 })).toBe('catalog.materials.pricesRequired')
    expect(materialPriceProblem({ priceBudget: 0, priceStandard: 1, pricePremium: 2 })).toBe('catalog.materials.priceInvalid')
    expect(materialPriceProblem({ priceBudget: 3, priceStandard: 2, pricePremium: 4 })).toBe('catalog.materials.priceOrder')
  })

  it('filters by text and status', () => {
    const materials = [
      { slug: 'primer', name: { en: 'Primer' }, isActive: true, workItemCount: 0, pricesNeedChecking: true },
      { slug: 'putty', name: { hy: 'Շպաքլ' }, isActive: false, workItemCount: 3 },
    ]
    expect(filterMaterials(materials, { search: 'շպ' }).map((m) => m.slug)).toEqual(['putty'])
    expect(filterMaterials(materials, { status: 'hidden' }).map((m) => m.slug)).toEqual(['putty'])
    expect(filterMaterials(materials, { status: 'unused' }).map((m) => m.slug)).toEqual(['primer'])
    expect(filterMaterials(materials, { status: 'active' }).map((m) => m.slug)).toEqual(['primer'])
    expect(filterMaterials(materials, { status: 'outdated' }).map((m) => m.slug)).toEqual(['primer'])
  })

  it('edits and checks the materials of a work item', () => {
    const lines = toMaterialLines([{ materialId: 'a', quantityPerUnit: 1.2 }])
    expect(lines).toEqual([{ materialId: 'a', quantityPerUnit: 1.2, wastePercent: 0 }])
    expect(toMaterialInputs(lines)).toEqual(lines)
    expect(amountFor({ quantityPerUnit: 2, wastePercent: 10 }, 10)).toBeCloseTo(22)
    expect(materialLineProblem({ quantityPerUnit: 0, wastePercent: 0 })).toBe('catalog.workItemMaterials.amountInvalid')
    expect(materialLineProblem({ quantityPerUnit: 1, wastePercent: 101 })).toBe('catalog.workItemMaterials.wasteInvalid')
    expect(materialLineProblem({ quantityPerUnit: 1, wastePercent: 5 })).toBeNull()
  })
})
