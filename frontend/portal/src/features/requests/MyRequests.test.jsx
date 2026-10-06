import { screen, waitFor, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { http, HttpResponse } from 'msw'
import en from '@/i18n/locales/en/common.json'
import { CUSTOMER, PARTNER_USER, problem, signedInAs } from '@/test/auth'
import { myRequest, myRequestItem, offer, order, page } from '@/test/fixtures'
import { renderRoute } from '@/test/renderWithProviders'
import { server } from '@/test/server'

function requestsApi(items = [myRequestItem()], options) {
  const seen = []
  server.use(
    http.get('*/api/v1/requests/mine', ({ request }) => {
      seen.push(new URL(request.url).searchParams.toString())
      return HttpResponse.json(page(items, options))
    }),
  )
  return seen
}

function detailApi(request = myRequest(), offers = []) {
  const calls = { cancelled: [], accepted: [], rejected: [] }
  server.use(
    http.get('*/api/v1/requests/:id', ({ params }) => (params.id === request.id ? HttpResponse.json(request) : problem(404, 'request.not_found'))),
    http.get('*/api/v1/requests/:id/offers', () => HttpResponse.json(offers)),
    http.post('*/api/v1/requests/:id/cancel', async ({ request: req }) => {
      calls.cancelled.push(await req.json())
      return HttpResponse.json({ ...request, status: 'Cancelled', cancelledAt: '2026-10-05T12:00:00Z', cancelReason: 'Fixed it' })
    }),
    http.post('*/api/v1/offers/:id/accept', ({ params }) => {
      calls.accepted.push(params.id)
      return HttpResponse.json(order())
    }),
    http.post('*/api/v1/offers/:id/reject', async ({ params, request: req }) => {
      calls.rejected.push({ id: params.id, ...(await req.json()) })
      return HttpResponse.json(offer({ id: params.id, status: 'Rejected' }))
    }),
    http.get('*/api/v1/orders/:id', () => HttpResponse.json(order())),
  )
  return calls
}

describe('My requests', () => {
  it('lists requests with their status and progress, filtered by status in the URL', async () => {
    const user = userEvent.setup()
    signedInAs(CUSTOMER)
    const seen = requestsApi([
      myRequestItem(),
      myRequestItem({ id: 'req-2', kind: 'Direct', status: 'Cancelled', findingPartners: true, responded: 0 }),
    ])
    const { router } = renderRoute('/en/requests')

    const list = await within(await screen.findByRole('main')).findByRole('list')
    const cards = within(list).getAllByRole('link')
    expect(cards[0]).toHaveAttribute('href', '/en/requests/req-1')
    expect(cards[0]).toHaveTextContent('Plumbing · Yerevan, Kentron')
    expect(cards[0]).toHaveTextContent('Sent to 3 · 1 responded')
    expect(cards[1]).toHaveTextContent(en.requests.findingPartners)
    expect(cards[1]).toHaveTextContent(en.requests.kind.Direct)
    expect(screen.queryByRole('navigation', { name: en.requests.tabsLabel })).not.toBeInTheDocument()

    await user.click(screen.getByRole('radio', { name: en.requests.status.Cancelled }))
    await waitFor(() => expect(router.state.location.search).toBe('?status=Cancelled'))
    await waitFor(() => expect(seen.at(-1)).toContain('status=Cancelled'))
  })

  it('shows an empty state with a call to action, and pages', async () => {
    signedInAs(CUSTOMER)
    requestsApi([])
    renderRoute('/en/requests')
    expect(await screen.findByText(en.requests.emptyTitle)).toBeInTheDocument()
    expect(screen.getAllByRole('link', { name: en.requests.new })).toHaveLength(2)
  })

  it('pages through long lists and shows partners their other tabs', async () => {
    const user = userEvent.setup()
    signedInAs(PARTNER_USER)
    const seen = requestsApi([myRequestItem()], { totalCount: 45 })
    const { router } = renderRoute('/en/requests')

    expect(await screen.findByText('Page 1 of 3')).toBeInTheDocument()
    const tabs = screen.getByRole('navigation', { name: en.requests.tabsLabel })
    expect(within(tabs).getAllByRole('link').map((l) => l.getAttribute('href'))).toEqual(['/en/requests', '/en/inbox', '/en/offers', '/en/prices', '/en/commissions'])
    await user.click(screen.getByRole('button', { name: en.pagination.next }))
    await waitFor(() => expect(router.state.location.search).toBe('?page=2'))
    await waitFor(() => expect(seen.at(-1)).toContain('page=2'))
  })

  it('shows an error with a retry', async () => {
    signedInAs(CUSTOMER)
    server.use(http.get('*/api/v1/requests/mine', () => problem(500, 'internal_error')))
    renderRoute('/en/requests')
    expect(await screen.findByText(en.errors.generic)).toBeInTheDocument()
    expect(screen.getByRole('button', { name: en.common.retry })).toBeInTheDocument()
  })
})

describe('My request page', () => {
  it('shows the details, the budget, photos and who has it', async () => {
    signedInAs(CUSTOMER)
    detailApi()
    renderRoute('/en/requests/req-1')

    expect(await screen.findByRole('heading', { level: 1, name: 'Plumbing' })).toBeInTheDocument()
    expect(screen.getByText('Yerevan, Kentron')).toBeInTheDocument()
    expect(screen.getByText('10,000 ֏ – 30,000 ֏')).toBeInTheDocument()
    expect(screen.getByText('Oct 7, 2026, evenings')).toBeInTheDocument()
    expect(screen.getByRole('img', { name: 'leak.jpg' })).toHaveAttribute('src', 'https://files/leak-t.jpg')
    expect(screen.getByRole('img', { name: 'leak.mp4' })).toBeInTheDocument()
    expect(screen.getByText('Aram Plumbing')).toBeInTheDocument()
    expect(await screen.findByText(en.offers.none)).toBeInTheDocument()
  })

  it('cancels with an optional reason', async () => {
    const user = userEvent.setup()
    signedInAs(CUSTOMER)
    const calls = detailApi()
    renderRoute('/en/requests/req-1')

    await user.click(await screen.findByRole('button', { name: en.requests.cancel }))
    const dialog = screen.getByRole('dialog', { name: en.requests.cancelTitle })
    await user.type(within(dialog).getByLabelText(/Why are you cancelling/), 'Fixed it')
    await user.click(within(dialog).getByRole('button', { name: en.requests.cancelConfirm }))

    await waitFor(() => expect(screen.queryByRole('dialog')).not.toBeInTheDocument())
    expect(calls.cancelled).toEqual([{ reason: 'Fixed it' }])
  })

  it('explains cancelled requests and requests waiting for an operator', async () => {
    signedInAs(CUSTOMER)
    detailApi(myRequest({ status: 'Cancelled', cancelledAt: '2026-10-05T12:00:00Z', cancelReason: 'Fixed it', partners: [], budgetMin: null, budgetMax: null }))
    renderRoute('/en/requests/req-1')
    expect(await screen.findByText('Fixed it')).toBeInTheDocument()
    expect(screen.getByText(en.requests.noBudget)).toBeInTheDocument()
    expect(screen.queryByRole('button', { name: en.requests.cancel })).not.toBeInTheDocument()
  })

  it('tells the customer when no specialist matched yet', async () => {
    signedInAs(CUSTOMER)
    detailApi(myRequest({ findingPartners: true }))
    renderRoute('/en/requests/req-1')
    expect(await screen.findByText(en.requests.findingPartnersLong)).toBeInTheDocument()
  })

  it('says when a request does not exist', async () => {
    signedInAs(CUSTOMER)
    detailApi()
    renderRoute('/en/requests/other')
    expect(await screen.findByRole('heading', { level: 1, name: en.requests.notFound })).toBeInTheDocument()
  })

  it('compares offers and accepts one, which opens the new order', async () => {
    const user = userEvent.setup()
    signedInAs(CUSTOMER)
    const calls = detailApi(myRequest(), [
      offer(),
      offer({ id: 'offer-2', kind: 'Visit', price: 0, partner: { partnerId: 'p2', displayName: 'Gor Masters', slug: null }, lines: [], stages: [], visitAt: '2026-10-06T10:00:00Z' }),
      offer({ id: 'offer-3', status: 'Rejected', rejectReason: 'Too expensive', partner: { partnerId: 'p3', displayName: 'Ani Fix', slug: null } }),
    ])
    const { router } = renderRoute('/en/requests/req-1')

    const aram = await screen.findByRole('article', { name: 'Aram Plumbing' })
    expect(within(aram).getByText('100,000 ֏')).toBeInTheDocument()
    expect(within(aram).getByRole('list', { name: en.offers.included })).toHaveTextContent('Remove the old tap')
    expect(within(aram).getByRole('list', { name: en.offers.excluded })).toHaveTextContent('Tiling')
    expect(within(aram).getByRole('list', { name: en.offers.paymentPlan })).toHaveTextContent('Deposit30,000 ֏')
    expect(within(aram).getByText('2 day(s)')).toBeInTheDocument()
    const gor = screen.getByRole('article', { name: 'Gor Masters' })
    expect(within(gor).getByText(en.offers.free)).toBeInTheDocument()
    const ani = screen.getByRole('article', { name: 'Ani Fix' })
    expect(within(ani).queryByRole('button', { name: en.offers.accept })).not.toBeInTheDocument()
    expect(within(ani).getByText('Reason: Too expensive')).toBeInTheDocument()

    await user.click(within(aram).getByRole('button', { name: en.offers.accept }))
    const dialog = screen.getByRole('dialog', { name: en.offers.acceptTitle })
    expect(dialog).toHaveTextContent(en.offers.acceptWorkNote)
    await user.click(within(dialog).getByRole('button', { name: en.offers.acceptConfirm }))

    await waitFor(() => expect(router.state.location.pathname).toBe('/en/orders/order-1'))
    expect(calls.accepted).toEqual(['offer-1'])
    expect(await screen.findByText(en.orders.created)).toBeInTheDocument()
  })

  it('rejects an offer with a reason, and explains what accepting a visit means', async () => {
    const user = userEvent.setup()
    signedInAs(CUSTOMER)
    const calls = detailApi(myRequest(), [offer({ kind: 'Visit', price: 5000, lines: [], stages: [{ title: null, purpose: 'Final', amount: 5000 }], visitAt: '2026-10-06T10:00:00Z' })])
    renderRoute('/en/requests/req-1')

    const card = await screen.findByRole('article', { name: 'Aram Plumbing' })
    await user.click(within(card).getByRole('button', { name: en.offers.accept }))
    expect(screen.getByRole('dialog')).toHaveTextContent(en.offers.acceptVisitNote)
    await user.click(within(screen.getByRole('dialog')).getByRole('button', { name: en.common.back }))

    await user.click(within(card).getByRole('button', { name: en.offers.reject }))
    await user.type(screen.getByLabelText(/Reason/), 'Not now')
    await user.click(within(screen.getByRole('dialog')).getByRole('button', { name: en.offers.rejectConfirm }))
    await waitFor(() => expect(calls.rejected).toEqual([{ id: 'offer-1', reason: 'Not now' }]))
  })

  it('shows an accept failure', async () => {
    const user = userEvent.setup()
    signedInAs(CUSTOMER)
    detailApi(myRequest(), [offer()])
    server.use(http.post('*/api/v1/offers/:id/accept', () => problem(422, 'offer.expired')))
    renderRoute('/en/requests/req-1')

    await user.click(within(await screen.findByRole('article', { name: 'Aram Plumbing' })).getByRole('button', { name: en.offers.accept }))
    await user.click(within(screen.getByRole('dialog')).getByRole('button', { name: en.offers.acceptConfirm }))
    expect(await screen.findByText(en.errors.offer.expired)).toBeInTheDocument()
  })

  it('links accepted offers to their order', async () => {
    signedInAs(CUSTOMER)
    detailApi(myRequest({ status: 'Closed' }), [offer({ status: 'Accepted', orderId: 'order-1' })])
    renderRoute('/en/requests/req-1')
    expect(await screen.findByRole('link', { name: en.offers.openOrder })).toHaveAttribute('href', '/en/orders/order-1')
  })
})
