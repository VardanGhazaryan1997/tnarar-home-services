import { act, screen, waitFor, within } from '@testing-library/react'
import { http, HttpResponse } from 'msw'
import { CUSTOMER, signedInAs } from '@/test/auth'
import { myRequestItem, page } from '@/test/fixtures'
import { renderRoute } from '@/test/renderWithProviders'
import { server } from '@/test/server'
import { hub } from '@/test/signalr'
import { changeSeen, liveReducer, offerUpdated, requestUpdated, selectRequestChange } from './liveSlice'

const at = (minute) => `2026-10-05T12:${String(minute).padStart(2, '0')}:00Z`
const reduce = (...actions) => actions.reduce(liveReducer, undefined)
const change = (state, id = 'req-1') => selectRequestChange(id)({ live: state })

describe('live changes', () => {
  it('says how a request ended: cancelled, accepted with its order, or closed some other way', () => {
    const state = reduce(
      requestUpdated({ requestId: 'req-1', status: 'Cancelled', orderId: null, at: at(0) }),
      requestUpdated({ requestId: 'req-2', status: 'Closed', orderId: 'ord-1', at: at(0) }),
      requestUpdated({ requestId: 'req-3', status: 'Closed', orderId: null, at: at(0) }),
      requestUpdated({ requestId: 'req-4', status: 'Open', orderId: null, at: at(0) }),
    )

    expect(change(state)).toEqual({ change: 'cancelled', status: 'Cancelled', orderId: null, at: at(0) })
    expect(change(state, 'req-2')).toMatchObject({ change: 'accepted', orderId: 'ord-1' })
    expect(change(state, 'req-3')).toMatchObject({ change: 'closed' })
    expect(change(state, 'req-4')).toBeUndefined()
  })

  it('notes offers that arrive or are decided, but not over the request change they came with', () => {
    const offer = (status, minute, extra) => offerUpdated({ offerId: 'off-1', requestId: 'req-1', partnerId: 'p-1', status, orderId: null, at: at(minute), ...extra })

    expect(change(reduce(offer('Sent', 0)))).toMatchObject({ change: 'offer', offerId: 'off-1', partnerId: 'p-1' })
    expect(change(reduce(offer('Rejected', 0)))).toMatchObject({ change: 'offerRejected' })
    expect(change(reduce(offer('Withdrawn', 0)))).toMatchObject({ change: 'offerWithdrawn' })
    expect(change(reduce(offer('Expired', 0)))).toBeUndefined()

    // Accepting pushes the offer and the closed request at the same moment, in either order.
    const closed = requestUpdated({ requestId: 'req-1', status: 'Closed', orderId: 'ord-1', at: at(5) })
    expect(change(reduce(closed, offer('Accepted', 5, { orderId: 'ord-1' })))).toMatchObject({ change: 'accepted' })
    expect(change(reduce(offer('Accepted', 5, { orderId: 'ord-1' }), closed))).toMatchObject({ change: 'accepted' })
    // A newer offer after a cancellation still shows.
    const cancelled = requestUpdated({ requestId: 'req-1', status: 'Cancelled', orderId: null, at: at(5) })
    expect(change(reduce(cancelled, offer('Withdrawn', 6)))).toMatchObject({ change: 'offerWithdrawn' })
  })

  it('forgets a change once it has been seen', () => {
    const state = reduce(requestUpdated({ requestId: 'req-1', status: 'Cancelled', orderId: null, at: at(0) }), changeSeen('req-1'))
    expect(change(state)).toBeUndefined()
  })

  it('updates the requests page when a request changes elsewhere, without a reload', async () => {
    signedInAs(CUSTOMER)
    let status = 'Open'
    server.use(http.get('*/api/v1/requests/mine', () => HttpResponse.json(page([myRequestItem({ status })]))))
    const { store } = renderRoute('/en/requests')

    const list = await within(await screen.findByRole('main')).findByRole('list')
    expect(within(list).getByRole('link')).toHaveTextContent('Open')
    await waitFor(() => expect(hub.connections).toHaveLength(1))

    status = 'Cancelled'
    act(() => hub.emit('requestUpdated', { requestId: 'req-1', status: 'Cancelled', orderId: null, at: at(0) }))

    await waitFor(() => expect(within(list).getByRole('link')).toHaveTextContent('Cancelled'))
    expect(store.getState().live.requests['req-1'].change).toBe('cancelled')

    act(() => hub.emit('offerUpdated', { offerId: 'off-9', requestId: 'req-2', partnerId: 'p-1', status: 'Sent', orderId: null, at: at(1) }))
    expect(store.getState().live.requests['req-2'].change).toBe('offer')
  })
})
