import { draftProblem, isChanged, isPending, toDraft, toItems } from './quickPrices'

const putty = { id: 'p1', price: 5000, salePrice: null, saleEndsOn: null, inStock: true }
const paint = { id: 'p2', price: 9000, salePrice: 7500, saleEndsOn: '2026-11-01', inStock: false }

describe('quick price editing helpers', () => {
  it('knows when a draft changes a product or only confirms its price', () => {
    expect(isChanged(putty, toDraft(putty))).toBe(false)
    expect(isChanged(putty, { ...toDraft(putty), price: 4800 })).toBe(true)
    expect(isChanged(paint, { ...toDraft(paint), saleEndsOn: '2026-11-02' })).toBe(true)
    expect(isPending(putty, undefined)).toBe(false)
    expect(isPending(putty, { ...toDraft(putty), confirm: true })).toBe(true)
  })

  it('checks drafts by the server’s rules', () => {
    expect(draftProblem(toDraft(putty))).toBeNull()
    expect(draftProblem({ ...toDraft(putty), price: null })).toBe('stores.product.priceRequired')
    expect(draftProblem({ ...toDraft(putty), price: 0 })).toBe('stores.quickEdit.priceInvalid')
    expect(draftProblem({ ...toDraft(putty), salePrice: 4000 })).toBe('stores.product.saleNeedsBoth')
    expect(draftProblem({ ...toDraft(paint), salePrice: 9500 })).toBe('stores.product.saleTooHigh')
  })

  it('sends only changed or confirmed rows, confirming only unchanged ones', () => {
    const drafts = { p1: { ...toDraft(putty), price: 4800, confirm: true }, p2: { ...toDraft(paint), confirm: true } }
    expect(toItems([putty, paint, { id: 'p3', price: 1, inStock: true }], drafts)).toEqual([
      { productId: 'p1', price: 4800, salePrice: null, saleEndsOn: null, inStock: true, confirm: false },
      { productId: 'p2', price: 9000, salePrice: 7500, saleEndsOn: '2026-11-01', inStock: false, confirm: true },
    ])
  })
})
