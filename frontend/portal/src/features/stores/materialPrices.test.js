import { branchPlaces, brandsOf, cheapestInStock, filterMaterials, filterOffers, sortOffers } from './materialPrices'

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

describe('brandsOf, filterOffers and sortOffers', () => {
  const offer = (id, brand, currentPrice, priceUpdatedAt) => ({ product: { id, brand, currentPrice, priceUpdatedAt } })
  const offers = [
    offer('a', 'Knauf', 300, '2026-10-01T00:00:00Z'),
    offer('b', null, 100, '2026-10-05T00:00:00Z'),
    offer('c', 'bosch', 200, '2026-10-03T00:00:00Z'),
    offer('d', 'knauf ', 250, '2026-10-02T00:00:00Z'),
  ]
  const ids = (list) => list.map((item) => item.product.id)

  it('lists each brand once, sorted ignoring case, without empty ones', () => {
    expect(brandsOf(offers)).toEqual(['bosch', 'Knauf'])
    expect(brandsOf()).toEqual([])
  })

  it('keeps the offers of one brand, ignoring case, or all of them', () => {
    expect(ids(filterOffers(offers, { brand: 'Knauf' }))).toEqual(['a', 'd'])
    expect(filterOffers(offers, { brand: '' })).toBe(offers)
    expect(filterOffers(undefined, { brand: 'x' })).toEqual([])
  })

  it('sorts by pack price or by the latest check, and keeps the server order by default', () => {
    expect(ids(sortOffers(offers, 'pack'))).toEqual(['b', 'c', 'd', 'a'])
    expect(ids(sortOffers(offers, 'updated'))).toEqual(['b', 'c', 'd', 'a'])
    expect(sortOffers(offers, 'unit')).toBe(offers)
    expect(ids(offers)).toEqual(['a', 'b', 'c', 'd'])
  })
})
