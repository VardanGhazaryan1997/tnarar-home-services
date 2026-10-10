import { allPriced, initialRows, parseAmount, rowAmount, suggestedPrice, toOfferLines, total } from './linePricing'

const LINES = [
  { id: 'l1', roomName: 'Bath', workItemId: 'tiles', name: 'Floor tiling', unit: 'SquareMeter', quantity: 4 },
  { id: 'l2', roomName: 'Bath', workItemId: 'toilet', name: 'Toilet', unit: 'Piece', quantity: null },
  { id: 'l3', roomName: 'Hall', workItemId: 'paint', name: 'Painting', unit: 'SquareMeter', quantity: 20 },
]
const PRICES = [
  { workItemId: 'tiles', priceFrom: 6000, priceTo: 8000, marketTypical: 6500 },
  { workItemId: 'toilet', priceFrom: null, priceTo: null, marketTypical: 15000 },
]

describe('pricing an offer line by line', () => {
  it('suggests the partner’s price, else the market’s', () => {
    expect(suggestedPrice(PRICES[0])).toEqual({ price: 7000, source: 'mine' })
    expect(suggestedPrice({ priceFrom: 5000, priceTo: null })).toEqual({ price: 5000, source: 'mine' })
    expect(suggestedPrice(PRICES[1])).toEqual({ price: 15000, source: 'market' })
    expect(suggestedPrice(undefined)).toEqual({ price: null, source: null })
  })

  it('starts rows from the request and the price list', () => {
    const rows = initialRows(LINES, PRICES)
    expect(rows.map((row) => [row.quantity, row.unitPrice, row.source])).toEqual([
      ['4', '7000', 'mine'],
      ['', '15000', 'market'],
      ['20', '', null],
    ])
    expect(allPriced(rows)).toBe(false)
  })

  it('adds up amounts and builds offer lines', () => {
    const rows = initialRows(LINES, PRICES)
    rows[1].quantity = '1'
    rows[2].included = false

    expect(rows.map(rowAmount)).toEqual([28000, 15000, null])
    expect(allPriced(rows)).toBe(true)
    expect(total(rows)).toBe(43000)
    expect(toOfferLines(rows)).toEqual([
      { title: 'Bath · Floor tiling', included: true, requestLineId: 'l1', quantity: 4, unitPrice: 7000 },
      { title: 'Bath · Toilet', included: true, requestLineId: 'l2', quantity: 1, unitPrice: 15000 },
      { title: 'Hall · Painting', included: false, requestLineId: 'l3', quantity: null, unitPrice: null },
    ])
  })

  it('reads amounts as typed', () => {
    expect(parseAmount('1 500')).toBe(1500)
    expect(parseAmount('2,5')).toBe(2.5)
    expect(parseAmount('')).toBeNull()
    expect(parseAmount('x')).toBeNaN()
    expect(rowAmount({ included: true, quantity: '0', unitPrice: '100' })).toBeNull()
  })
})
