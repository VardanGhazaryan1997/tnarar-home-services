import { offer } from '@/test/fixtures'
import { fromOffer } from './offerPrefill'

const requestLines = [
  { id: 'line-1', roomName: 'Bathroom', name: 'Floor tiling', unit: 'SquareMeter', quantity: 6, workItemId: 'w-1' },
  { id: 'line-2', roomName: 'Bathroom', name: 'Sockets', unit: 'Point', quantity: 2, workItemId: 'w-2' },
]

describe('starting an offer from an earlier one', () => {
  it('puts the lines answering the request into the line-by-line rows and the others into extra lines', () => {
    const start = fromOffer(
      offer({
        lines: [
          { title: 'Bathroom · Floor tiling', included: true, requestLineId: 'line-1', quantity: 6.5, unitPrice: 7000, amount: 45500 },
          { title: 'Bathroom · Sockets', included: false, requestLineId: 'line-2', quantity: null, unitPrice: null, amount: null },
          { title: 'Taking the rubbish away', included: true, quantity: 1, unitPrice: 5000, amount: 5000 },
        ],
        price: 50500,
        materialsIncluded: false,
        stages: [{ title: null, purpose: 'Final', amount: 50500 }],
      }),
      requestLines,
      [{ workItemId: 'w-2', priceFrom: 1500, priceTo: null }],
    )

    expect(start.byLine).toBe(true)
    expect(start.rows.map(({ requestLineId, quantity, unitPrice, included, source }) => ({ requestLineId, quantity, unitPrice, included, source }))).toEqual([
      { requestLineId: 'line-1', quantity: '6.5', unitPrice: '7000', included: true, source: null },
      { requestLineId: 'line-2', quantity: '2', unitPrice: '1500', included: false, source: 'mine' },
    ])
    expect(start.lines).toEqual([expect.objectContaining({ title: 'Taking the rubbish away', included: true, price: '5000' })])
    expect(start.stages).toEqual([])
  })

  it('keeps a future visit time in local form and drops a past one', () => {
    const future = new Date(Date.now() + 3 * 24 * 60 * 60 * 1000)
    future.setSeconds(0, 0)
    const visit = (visitAt) => fromOffer(offer({ kind: 'Visit', price: 0, lines: [], stages: [], startDate: null, durationDays: null, visitAt }), [], [])

    const start = visit(future.toISOString())
    expect(start.kind).toBe('Visit')
    expect(new Date(start.form.visitAt).getTime()).toBe(future.getTime())
    expect(start.form.price).toBe('0')
    expect(visit('2020-01-01T10:00:00Z').form.visitAt).toBe('')
  })
})
