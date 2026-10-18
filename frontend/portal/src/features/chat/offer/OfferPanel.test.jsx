import { screen, waitFor, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { http, HttpResponse } from 'msw'
import en from '@/i18n/locales/en/common.json'
import { CUSTOMER, PARTNER_USER, signedInAs } from '@/test/auth'
import { conversation, message, myRequest, offer as fullOffer, order, page } from '@/test/fixtures'
import { renderRoute } from '@/test/renderWithProviders'
import { server } from '@/test/server'
import { currentOffer, offerEvents, statusNow } from './offerTimeline'

const DAY = 24 * 60 * 60 * 1000
const inDays = (days) => new Date(Date.now() + days * DAY).toISOString()

const chatOffer = (overrides = {}) => ({
  id: 'off-1',
  kind: 'Work',
  status: 'Sent',
  price: 100000,
  summary: 'Replace the tap and the pipes under the sink.',
  visitAt: null,
  expiresAt: inDays(7.1),
  sentAt: '2026-10-05T09:30:00Z',
  decidedAt: null,
  orderId: null,
  ...overrides,
})

/** The chat API with one conversation; `answers` change what accepting and rejecting return. */
function chatApi(detail) {
  const calls = { accepted: [], rejected: [] }
  let current = detail
  server.use(
    http.get('*/api/v1/conversations', () => HttpResponse.json(page([current]))),
    http.get('*/api/v1/conversations/:id', () => HttpResponse.json(current)),
    http.get('*/api/v1/conversations/:id/messages', () => HttpResponse.json({ items: [message()], hasMore: false })),
    http.post('*/api/v1/conversations/:id/read', () => HttpResponse.json({ readAt: '2026-10-05T11:00:00Z' })),
    http.post('*/api/v1/offers/:id/accept', ({ params }) => {
      calls.accepted.push(params.id)
      current = { ...current, offers: current.offers.map((o) => ({ ...o, status: 'Accepted', orderId: 'ord-1', decidedAt: '2026-10-05T12:00:00Z' })) }
      return HttpResponse.json(order({ id: 'ord-1' }))
    }),
    http.post('*/api/v1/offers/:id/reject', async ({ params, request }) => {
      calls.rejected.push({ id: params.id, ...(await request.json()) })
      current = { ...current, offers: current.offers.map((o) => ({ ...o, status: 'Rejected', decidedAt: '2026-10-05T12:00:00Z' })) }
      return HttpResponse.json(fullOffer({ id: params.id, status: 'Rejected' }))
    }),
  )
  return calls
}

async function panel() {
  return screen.findByRole('region', { name: /Offer from Aram Plumbing|Your offer/ })
}

describe('Offer in the chat', () => {
  it('lets the customer accept the offer right in the chat and stay there, with a link to the new order', async () => {
    const user = userEvent.setup()
    signedInAs(CUSTOMER)
    const calls = chatApi(conversation({ offers: [chatOffer()] }))
    const { router } = renderRoute('/en/messages/conv-1')

    const offer = await panel()
    expect(offer).toHaveTextContent('100,000 ֏')
    expect(offer).toHaveTextContent(en.chat.offer.expiresIn.replace('{{when}}', 'in 7 days'))

    await user.click(within(offer).getByRole('button', { name: en.offers.accept }))
    const dialog = await screen.findByRole('dialog', { name: en.offers.acceptTitle })
    expect(dialog).toHaveTextContent('Aram Plumbing')
    await user.click(within(dialog).getByRole('button', { name: en.offers.acceptConfirm }))

    await waitFor(() => expect(within(offer).getByText(en.chat.offer.accepted)).toBeInTheDocument())
    expect(calls.accepted).toEqual(['off-1'])
    expect(router.state.location.pathname).toBe('/en/messages/conv-1')
    expect(within(offer).getByRole('link', { name: en.offers.openOrder })).toHaveAttribute('href', '/en/orders/ord-1')
    expect(within(offer).queryByRole('button', { name: en.offers.accept })).not.toBeInTheDocument()
    expect(await screen.findByText(en.chat.offer.events.accepted)).toBeInTheDocument()
  })

  it('lets the customer decline the offer, saying why', async () => {
    const user = userEvent.setup()
    signedInAs(CUSTOMER)
    const calls = chatApi(conversation({ offers: [chatOffer()] }))
    renderRoute('/en/messages/conv-1')

    const offer = await panel()
    await user.click(within(offer).getByRole('button', { name: en.offers.reject }))
    const dialog = await screen.findByRole('dialog', { name: en.offers.rejectTitle })
    await user.type(within(dialog).getByLabelText(new RegExp(en.offers.rejectReason)), 'Too expensive')
    await user.click(within(dialog).getByRole('button', { name: en.offers.rejectConfirm }))

    await waitFor(() => expect(calls.rejected).toEqual([{ id: 'off-1', reason: 'Too expensive' }]))
    expect(await within(offer).findByText(en.chat.offer.rejected)).toBeInTheDocument()
  })

  it('shows an expired offer as expired, explains why, and won’t accept it', async () => {
    signedInAs(CUSTOMER)
    chatApi(conversation({ offers: [chatOffer({ expiresAt: inDays(-1) })] }))
    renderRoute('/en/messages/conv-1')

    const offer = await panel()
    expect(offer).toHaveTextContent(en.offers.status.Expired)
    expect(offer).toHaveTextContent(en.chat.offer.expiredText)
    expect(within(offer).getByRole('button', { name: en.offers.accept })).toBeDisabled()
    expect(within(offer).queryByRole('button', { name: en.offers.reject })).not.toBeInTheDocument()
    expect(offer).toHaveClass('offer-panel--warning')
  })

  it('warns when the offer expires within a day', async () => {
    signedInAs(CUSTOMER)
    chatApi(conversation({ offers: [chatOffer({ expiresAt: new Date(Date.now() + 5 * 60 * 60 * 1000 + 60_000).toISOString() })] }))
    renderRoute('/en/messages/conv-1')

    const offer = await panel()
    const status = within(offer).getByText(en.chat.offer.expiresIn.replace('{{when}}', 'in 5 hours'))
    expect(status.closest('p')).toHaveClass('offer-panel__status--soon')
  })

  it('offers nothing to decide once the request was cancelled', async () => {
    signedInAs(CUSTOMER)
    chatApi(conversation({ requestStatus: 'Cancelled', offers: [chatOffer({ status: 'Closed' })] }))
    renderRoute('/en/messages/conv-1')

    const offer = await panel()
    expect(offer).toHaveTextContent(en.chat.offer.closed)
    expect(within(offer).queryByRole('button', { name: en.offers.accept })).not.toBeInTheDocument()
  })

  it('shows the specialist where their offer stands, without decisions, linking to the request', async () => {
    signedInAs(PARTNER_USER)
    chatApi(conversation({ myRole: 'Partner', otherParty: { name: 'Ani', partnerId: null, slug: null }, offers: [chatOffer()] }))
    renderRoute('/en/messages/conv-1')

    const offer = await panel()
    expect(offer).toHaveTextContent(en.chat.offer.mine)
    expect(offer).toHaveTextContent(en.chat.offer.waiting)
    expect(within(offer).queryByRole('button', { name: en.offers.accept })).not.toBeInTheDocument()
    expect(within(offer).getByRole('link', { name: en.chat.offer.details })).toHaveAttribute('href', '/en/inbox/req-1')
  })

  it('links to the offer on the request page and opens its details on demand', async () => {
    const user = userEvent.setup()
    signedInAs(CUSTOMER)
    chatApi(conversation({ offers: [chatOffer()] }))
    renderRoute('/en/messages/conv-1')

    const offer = await panel()
    expect(within(offer).getByRole('link', { name: en.chat.offer.details })).toHaveAttribute('href', '/en/requests/req-1#offer-off-1')
    expect(offer).not.toHaveTextContent('Replace the tap')

    const toggle = within(offer).getByRole('button', { name: en.chat.offer.showMore })
    expect(toggle).toHaveAttribute('aria-expanded', 'false')
    await user.click(toggle)

    expect(within(offer).getByRole('button', { name: en.chat.offer.showLess })).toHaveAttribute('aria-expanded', 'true')
    expect(offer).toHaveTextContent('Replace the tap and the pipes under the sink.')
    expect(offer).toHaveClass('offer-panel--expanded')
  })

  it('shows what happened to offers between the messages, and no panel without offers', async () => {
    signedInAs(CUSTOMER)
    chatApi(conversation({ offers: [chatOffer({ id: 'old', status: 'Rejected', price: 150000, sentAt: '2026-10-05T09:00:00Z', decidedAt: '2026-10-05T09:10:00Z' })] }))
    renderRoute('/en/messages/conv-1')

    const thread = await screen.findByRole('list', { name: en.chat.messagesLabel })
    expect(await within(thread).findByText(en.chat.offer.events.sent.replace('{{price}}', '150,000 ֏'))).toBeInTheDocument()
    expect(within(thread).getByText(en.chat.offer.events.rejected)).toBeInTheDocument()
    // Oldest first: the offer (9:00, 9:10) before the message (10:00).
    const items = within(thread).getAllByRole('listitem').map((li) => li.textContent)
    expect(items.findIndex((text) => text.includes('Offer declined'))).toBeLessThan(items.findIndex((text) => text.includes('When can I come?')))
  })

  it('has no panel when there is no offer', async () => {
    signedInAs(CUSTOMER)
    chatApi(conversation())
    renderRoute('/en/messages/conv-1')
    await screen.findByRole('list', { name: en.chat.messagesLabel })
    expect(screen.queryByRole('region', { name: /Offer from/ })).not.toBeInTheDocument()
  })

  it('scrolls to the offer the chat linked to', async () => {
    signedInAs(CUSTOMER)
    const scrolled = vi.fn()
    Element.prototype.scrollIntoView = scrolled
    server.use(
      http.get('*/api/v1/requests/:id', () => HttpResponse.json(myRequest())),
      http.get('*/api/v1/requests/:id/offers', () => HttpResponse.json([fullOffer({ id: 'off-1' })])),
    )
    renderRoute('/en/requests/req-1#offer-off-1')

    await waitFor(() => expect(scrolled).toHaveBeenCalled())
    expect(document.getElementById('offer-off-1')).toBeInTheDocument()
    delete Element.prototype.scrollIntoView
  })
})

describe('offer timeline', () => {
  const now = new Date('2026-10-06T00:00:00Z')
  const offerAt = (overrides) => chatOffer({ expiresAt: '2026-10-10T00:00:00Z', ...overrides })

  it('shows the waiting offer, else the newest', () => {
    expect(currentOffer([])).toBeNull()
    expect(currentOffer(undefined)).toBeNull()
    expect(currentOffer([offerAt({ id: 'a', status: 'Sent' }), offerAt({ id: 'b', status: 'Rejected' })]).id).toBe('a')
    expect(currentOffer([offerAt({ id: 'a', status: 'Rejected' }), offerAt({ id: 'b', status: 'Accepted' })]).id).toBe('b')
  })

  it('treats a waiting offer past its expiry as expired', () => {
    expect(statusNow(offerAt({ expiresAt: '2026-10-05T00:00:00Z' }), now)).toBe('Expired')
    expect(statusNow(offerAt(), now)).toBe('Sent')
    expect(statusNow(offerAt({ status: 'Accepted', expiresAt: '2026-10-05T00:00:00Z' }), now)).toBe('Accepted')
  })

  it('lists events in time order, leaving out those before the oldest loaded message', () => {
    const events = offerEvents(
      [
        offerAt({ id: 'a', sentAt: '2026-10-01T10:00:00Z', expiresAt: '2026-10-03T00:00:00Z' }),
        offerAt({ id: 'b', status: 'Withdrawn', sentAt: '2026-10-02T10:00:00Z', decidedAt: '2026-10-02T11:00:00Z' }),
        offerAt({ id: 'c', status: 'Accepted', sentAt: '2026-10-04T10:00:00Z', decidedAt: null }),
      ],
      now,
    )
    expect(events.map((e) => e.id)).toEqual(['a-sent', 'b-sent', 'b-withdrawn', 'a-expired', 'c-sent'])
    expect(offerEvents([offerAt({ sentAt: '2026-10-01T10:00:00Z' })], now, '2026-10-02T00:00:00Z')).toEqual([])
  })
})
