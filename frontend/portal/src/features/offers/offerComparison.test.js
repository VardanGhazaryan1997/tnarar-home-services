import { compareOffers } from './offerComparison'

const LINES = [
  { id: 'l1', roomName: 'Bath', name: 'Tiling', estimateMin: 20000, estimateMax: 36000 },
  { id: 'l2', roomName: 'Bath', name: 'Toilet', estimateMin: 10000, estimateMax: 20000 },
]
const offer = (id, lines, status = 'Sent') => ({ id, kind: 'Work', status, price: 0, partner: { displayName: id }, lines })

describe('comparing offers line by line', () => {
  it('lines up each offer’s price per request line and marks the cheapest', () => {
    const table = compareOffers(LINES, [
      offer('a', [
        { requestLineId: 'l1', included: true, amount: 28000 },
        { requestLineId: 'l2', included: true, amount: 15000 },
        { requestLineId: null, included: true, title: 'Cleanup', amount: 5000 },
      ]),
      offer('b', [
        { requestLineId: 'l1', included: true, amount: 24000 },
        { requestLineId: 'l2', included: false, amount: null },
      ]),
      offer('withdrawn', [{ requestLineId: 'l1', included: true, amount: 1 }], 'Withdrawn'),
      { id: 'visit', kind: 'Visit', status: 'Sent', lines: [] },
    ])

    expect(table.offers.map((o) => o.id)).toEqual(['a', 'b'])
    expect(table.rows[0].best).toBe(24000)
    expect(table.rows[1].best).toBeNull()
    expect(table.rows[1].entries[1]).toEqual({ requestLineId: 'l2', included: false, amount: null })
    expect(table.extras).toEqual([5000, 0])
    expect(table.estimate).toEqual({ min: 30000, max: 56000 })
  })

  it('is empty without lines or priced offers', () => {
    expect(compareOffers([], [offer('a', [{ requestLineId: 'l1', included: true, amount: 1 }])])).toBeNull()
    expect(compareOffers(LINES, [offer('a', [{ requestLineId: null, included: true, title: 'All', amount: null }])])).toBeNull()
  })
})
