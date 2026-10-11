import { branchPlaces, cheapestInStock, filterMaterials } from './materialPrices'

describe('filterMaterials', () => {
  const materials = [
    { slug: 'finishing-putty', name: 'Finishing putty' },
    { slug: 'primer', name: 'Primer' },
  ]

  it('keeps materials whose name or slug contains the text, ignoring case and spaces', () => {
    expect(filterMaterials(materials, ' PUTTY ').map((m) => m.slug)).toEqual(['finishing-putty'])
    expect(filterMaterials(materials, 'prim').map((m) => m.slug)).toEqual(['primer'])
    expect(filterMaterials(materials, 'zzz')).toEqual([])
  })

  it('keeps all of them without text, and copes with no data', () => {
    expect(filterMaterials(materials, '  ')).toBe(materials)
    expect(filterMaterials(undefined)).toEqual([])
  })
})

describe('branchPlaces', () => {
  it('lists each town once, in order', () => {
    expect(branchPlaces([{ city: 'Yerevan' }, { city: 'Gyumri' }, { city: 'Yerevan' }])).toBe('Yerevan, Gyumri')
    expect(branchPlaces()).toBe('')
  })
})

describe('cheapestInStock', () => {
  const offer = (id, unitPrice, inStock = true) => ({ product: { id, unitPrice, inStock } })

  it('finds the cheapest product that is in stock', () => {
    expect(cheapestInStock([offer('a', 300), offer('b', 100, false), offer('c', 200), offer('d', 250)])).toBe('c')
  })

  it('is null when nothing is in stock', () => {
    expect(cheapestInStock([offer('a', 100, false)])).toBeNull()
    expect(cheapestInStock()).toBeNull()
  })
})
