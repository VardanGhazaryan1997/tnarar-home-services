import { act, screen, waitFor, within } from '@testing-library/react'
import { http, HttpResponse } from 'msw'
import en from '@/i18n/locales/en/common.json'
import { CUSTOMER, PARTNER_USER, signedInAs } from '@/test/auth'
import { inboxItem, inboxRequest, myRequest, myRequestItem, page } from '@/test/fixtures'
import { renderRoute } from '@/test/renderWithProviders'
import { server } from '@/test/server'
import { hub } from '@/test/signalr'
import { changeSeen, customerChange, liveReducer, offerUpdated, partnerChange, requestUpdated } from './liveSlice'
import { SEEN_AFTER_MS } from './useLiveChange'

const at = (minute) => `2026-10-05T12:${String(minute).padStart(2, '0')}:00Z`
const reduce = (...actions) => actions.reduce(liveReducer, undefined)
const entry = (state, id = 'req-1') => state.requests[id]
const request = (status, orderId = null, minute = 0) => requestUpdated({ requestId: 'req-1', status, orderId, at: at(minute) })
const offer = (status, orderId = null, minute = 0) => offerUpdated({ offerId: 'off-1', requestId: 'req-1', partnerId: 'p-1', status, orderId, at: at(minute) })

async function connected() {
  await waitFor(() => expect(hub.connections).toHaveLength(1))
}

describe('live changes, as each side reads them', () => {
  it('tells the customer how a request ended, and about offers while it is open', () => {
    expect(customerChange(entry(reduce(request('Cancelled'))))).toEqual({ kind: 'cancelled', at: at(0) })
    expect(customerChange(entry(reduce(offer('Accepted', 'ord-1'), request('Closed', 'ord-1'))))).toEqual({ kind: 'accepted', orderId: 'ord-1', at: at(0) })
    expect(customerChange(entry(reduce(request('Closed'))))).toEqual({ kind: 'closed', at: at(0) })
    expect(customerChange(entry(reduce(offer('Sent'))))).toMatchObject({ kind: 'offer' })
    expect(customerChange(entry(reduce(offer('Withdrawn'))))).toMatchObject({ kind: 'offerWithdrawn' })
    // The customer's own decisions need no pointing out; nor does an unknown status.
    expect(customerChange(entry(reduce(offer('Rejected'))))).toBeUndefined()
    expect(customerChange(entry(reduce(request('Open'))))).toBeUndefined()
    expect(customerChange(undefined)).toBeUndefined()
  })

  it('tells a partner whether their offer won, lost, or the request went away', () => {
    expect(partnerChange(entry(reduce(request('Cancelled'), offer('Sent'))))).toMatchObject({ kind: 'cancelled' })
    // The chosen partner hears both; the others only that the request closed.
    expect(partnerChange(entry(reduce(offer('Accepted', 'ord-1'), request('Closed', 'ord-1'))))).toEqual({ kind: 'offerAccepted', orderId: 'ord-1', at: at(0) })
    expect(partnerChange(entry(reduce(request('Closed', 'ord-9'))))).toMatchObject({ kind: 'closedOther' })
    expect(partnerChange(entry(reduce(offer('Rejected'))))).toMatchObject({ kind: 'offerRejected' })
    // Their own sends and withdrawals need no pointing out.
    expect(partnerChange(entry(reduce(offer('Sent'))))).toBeUndefined()
    expect(partnerChange(entry(reduce(offer('Withdrawn'))))).toBeUndefined()
  })

  it('forgets a change once it has been seen', () => {
    expect(entry(reduce(request('Cancelled'), changeSeen('req-1')))).toBeUndefined()
  })
})

describe('live requests pages', () => {
  it('updates the customer’s list without a reload and points out a cancellation in words and with an icon', async () => {
    signedInAs(CUSTOMER)
    let status = 'Open'
    server.use(http.get('*/api/v1/requests/mine', () => HttpResponse.json(page([myRequestItem({ status })]))))
    const { store } = renderRoute('/en/requests')

    const list = await within(await screen.findByRole('main')).findByRole('list')
    expect(within(list).getByRole('link')).toHaveTextContent(en.requests.status.Open)
    await connected()

    status = 'Cancelled'
    act(() => hub.emit('requestUpdated', { requestId: 'req-1', status: 'Cancelled', orderId: null, at: at(0) }))

    const note = await within(list).findByRole('status')
    expect(note).toHaveTextContent(en.live.customer.cancelled)
    expect(note.querySelector('svg')).toBeInTheDocument()
    expect(list.querySelector('li')).toHaveClass('request-card--live-danger')
    await waitFor(() => expect(within(list).getByRole('link')).toHaveTextContent(en.requests.status.Cancelled))
    expect(store.getState().live.requests['req-1'].request.status).toBe('Cancelled')
  })

  it('says "Accepted" with a link to the order when an offer was accepted, and "Closed" otherwise', async () => {
    signedInAs(CUSTOMER)
    server.use(
      http.get('*/api/v1/requests/mine', () =>
        HttpResponse.json(page([myRequestItem({ status: 'Closed', orderId: 'ord-1' }), myRequestItem({ id: 'req-2', status: 'Closed', orderId: null })])),
      ),
    )
    renderRoute('/en/requests')

    const list = await within(await screen.findByRole('main')).findByRole('list')
    const [accepted, closed] = within(list).getAllByRole('link')
    expect(accepted).toHaveTextContent(en.requests.status.Accepted)
    expect(closed).toHaveTextContent(en.requests.status.Ended)
    expect(closed).not.toHaveTextContent(en.requests.status.Accepted)
    await connected()

    act(() => hub.emit('offerUpdated', { offerId: 'off-1', requestId: 'req-1', partnerId: 'p-1', status: 'Accepted', orderId: 'ord-1', at: at(1) }))
    act(() => hub.emit('requestUpdated', { requestId: 'req-1', status: 'Closed', orderId: 'ord-1', at: at(1) }))

    const note = await within(list).findByRole('status')
    expect(note).toHaveTextContent(en.live.customer.accepted)
    expect(within(note).getByRole('link', { name: en.live.openOrder })).toHaveAttribute('href', '/en/orders/ord-1')
    expect(list.querySelector('li')).toHaveClass('request-card--live-success')
  })

  it('points out a new offer, then lets it fade once the user has had time to see it', async () => {
    signedInAs(CUSTOMER)
    server.use(http.get('*/api/v1/requests/mine', () => HttpResponse.json(page([myRequestItem()]))))
    renderRoute('/en/requests')
    const list = await within(await screen.findByRole('main')).findByRole('list')
    await connected()
    vi.useFakeTimers({ shouldAdvanceTime: true, toFake: ['setTimeout', 'clearTimeout'] })
    try {
      act(() => hub.emit('offerUpdated', { offerId: 'off-1', requestId: 'req-1', partnerId: 'p-1', status: 'Sent', orderId: null, at: at(2) }))
      expect(await within(list).findByRole('status')).toHaveTextContent(en.live.customer.offer)
      expect(list.querySelector('li')).toHaveClass('request-card--live-info')

      act(() => vi.advanceTimersByTime(SEEN_AFTER_MS + 100))
      await waitFor(() => expect(within(list).queryByRole('status')).not.toBeInTheDocument())
      expect(list.querySelector('li')).not.toHaveClass('request-card--live-info')
    } finally {
      vi.useRealTimers()
    }
  })

  it('keeps a change pointed out while the tab is hidden, and counts down once the user is back', async () => {
    const visibility = vi.spyOn(document, 'visibilityState', 'get').mockReturnValue('hidden')
    signedInAs(CUSTOMER)
    server.use(http.get('*/api/v1/requests/mine', () => HttpResponse.json(page([myRequestItem()]))))
    renderRoute('/en/requests')
    const list = await within(await screen.findByRole('main')).findByRole('list')
    await connected()
    vi.useFakeTimers({ shouldAdvanceTime: true, toFake: ['setTimeout', 'clearTimeout'] })
    try {
      act(() => hub.emit('requestUpdated', { requestId: 'req-1', status: 'Cancelled', orderId: null, at: at(3) }))
      await within(list).findByRole('status')
      act(() => vi.advanceTimersByTime(SEEN_AFTER_MS * 2))
      expect(within(list).getByRole('status')).toBeInTheDocument()

      visibility.mockReturnValue('visible')
      act(() => document.dispatchEvent(new Event('visibilitychange')))
      act(() => vi.advanceTimersByTime(SEEN_AFTER_MS + 100))
      await waitFor(() => expect(within(list).queryByRole('status')).not.toBeInTheDocument())
    } finally {
      vi.useRealTimers()
    }
  })

  it('shows the change on the request’s own page too', async () => {
    signedInAs(CUSTOMER)
    server.use(
      http.get('*/api/v1/requests/:id', () => HttpResponse.json(myRequest())),
      http.get('*/api/v1/requests/:id/offers', () => HttpResponse.json([])),
    )
    renderRoute('/en/requests/req-1')
    await screen.findByRole('heading', { level: 1 })
    await connected()

    act(() => hub.emit('requestUpdated', { requestId: 'req-1', status: 'Cancelled', orderId: null, at: at(4) }))

    expect(await screen.findByRole('status')).toHaveTextContent(en.live.customer.cancelled)
  })

  it('tells a partner in their inbox that their offer won, or that the customer chose someone else', async () => {
    signedInAs(PARTNER_USER)
    server.use(http.get('*/api/v1/requests/inbox', () => HttpResponse.json(page([inboxItem(), inboxItem({ id: 'req-2' })]))))
    renderRoute('/en/inbox')
    const list = await within(await screen.findByRole('main')).findByRole('list')
    await connected()

    act(() => hub.emit('offerUpdated', { offerId: 'off-1', requestId: 'req-1', partnerId: 'p-1', status: 'Accepted', orderId: 'ord-1', at: at(5) }))
    act(() => hub.emit('requestUpdated', { requestId: 'req-1', status: 'Closed', orderId: 'ord-1', at: at(5) }))
    act(() => hub.emit('requestUpdated', { requestId: 'req-2', status: 'Closed', orderId: 'ord-2', at: at(5) }))

    await waitFor(() => expect(within(list).getAllByRole('status')).toHaveLength(2))
    const [won, lost] = within(list).getAllByRole('status')
    expect(won).toHaveTextContent(en.live.partner.offerAccepted)
    expect(within(won).getByRole('link', { name: en.live.openOrder })).toHaveAttribute('href', '/en/orders/ord-1')
    expect(lost).toHaveTextContent(en.live.partner.closedOther)
    const [wonCard, lostCard] = list.querySelectorAll('li')
    expect(wonCard).toHaveClass('request-card--live-success')
    expect(lostCard).toHaveClass('request-card--live-neutral')
  })

  it('tells a partner on the request page that their offer was declined', async () => {
    signedInAs(PARTNER_USER)
    server.use(
      http.get('*/api/v1/requests/inbox/:id', () => HttpResponse.json(inboxRequest())),
      http.get('*/api/v1/requests/:id/offers', () => HttpResponse.json([])),
    )
    renderRoute('/en/inbox/req-1')
    await screen.findByRole('heading', { level: 1 })
    await connected()

    act(() => hub.emit('offerUpdated', { offerId: 'off-1', requestId: 'req-1', partnerId: 'p-1', status: 'Rejected', orderId: null, at: at(6) }))

    expect(await screen.findByRole('status')).toHaveTextContent(en.live.partner.offerRejected)
  })
})
