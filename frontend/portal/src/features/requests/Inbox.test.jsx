import { screen, waitFor, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { http, HttpResponse } from 'msw'
import en from '@/i18n/locales/en/common.json'
import { CUSTOMER, PARTNER_USER, problem, signedInAs } from '@/test/auth'
import { inboxItem, inboxRequest, offer, page } from '@/test/fixtures'
import { renderRoute } from '@/test/renderWithProviders'
import { server } from '@/test/server'

function inboxApi({ items = [inboxItem()], request = inboxRequest(), offers = [] } = {}) {
  const calls = { list: [], declined: [], withdrawn: [] }
  server.use(
    http.get('*/api/v1/requests/inbox', ({ request: req }) => {
      calls.list.push(new URL(req.url).searchParams.get('status'))
      return HttpResponse.json(page(items))
    }),
    http.get('*/api/v1/requests/inbox/:id', ({ params }) => (params.id === request.id ? HttpResponse.json(request) : problem(404, 'request.not_found'))),
    http.get('*/api/v1/requests/:id/offers', () => HttpResponse.json(offers)),
    http.post('*/api/v1/requests/inbox/:id/decline', async ({ request: req }) => {
      calls.declined.push(await req.json())
      return HttpResponse.json({ ...request, myStatus: 'Declined' })
    }),
    http.post('*/api/v1/offers/:id/withdraw', ({ params }) => {
      calls.withdrawn.push(params.id)
      return HttpResponse.json(offer({ status: 'Withdrawn' }))
    }),
  )
  return calls
}

describe('Partner inbox', () => {
  it('lists received requests, new ones highlighted, filtered by status', async () => {
    const user = userEvent.setup()
    signedInAs(PARTNER_USER)
    const calls = inboxApi({
      items: [inboxItem(), inboxItem({ id: 'req-2', kind: 'Direct', myStatus: 'Declined', requestStatus: 'Cancelled', mediaCount: 0, preferredDate: null })],
    })
    renderRoute('/en/inbox')

    const cards = within(await within(await screen.findByRole('main')).findByRole('list')).getAllByRole('link')
    expect(cards[0]).toHaveAttribute('href', '/en/inbox/req-1')
    expect(cards[0]).toHaveTextContent('wanted Oct 7, 2026')
    expect(cards[0]).toHaveTextContent('2 photo(s)')
    expect(cards[0].parentElement).toHaveClass('request-card--highlight')
    expect(cards[1]).toHaveTextContent(en.inbox.direct)
    expect(cards[1]).toHaveTextContent(en.requests.status.Cancelled)

    await user.click(screen.getByRole('radio', { name: en.inbox.status.New }))
    await waitFor(() => expect(calls.list.at(-1)).toBe('New'))
  })

  it('shows an empty inbox', async () => {
    signedInAs(PARTNER_USER)
    inboxApi({ items: [] })
    renderRoute('/en/inbox')
    expect(await screen.findByText(en.inbox.emptyTitle)).toBeInTheDocument()
  })

  it('is only for partners', async () => {
    signedInAs(CUSTOMER)
    server.use(http.get('*/api/v1/requests/mine', () => HttpResponse.json(page([]))))
    const { router } = renderRoute('/en/inbox')
    await waitFor(() => expect(router.state.location.pathname).toBe('/en/requests'))
  })

  it('opens a request without the budget, and declines it with a reason', async () => {
    const user = userEvent.setup()
    signedInAs(PARTNER_USER)
    const calls = inboxApi()
    renderRoute('/en/inbox/req-1')

    expect(await screen.findByRole('heading', { level: 1, name: 'Plumbing' })).toBeInTheDocument()
    expect(screen.getByText('Ani')).toBeInTheDocument()
    expect(screen.queryByText(en.requests.fields.budget)).not.toBeInTheDocument()
    expect(screen.getByRole('link', { name: en.inbox.sendOffer })).toHaveAttribute('href', '/en/inbox/req-1/offer')

    await user.click(screen.getByRole('button', { name: en.inbox.decline }))
    await user.type(within(screen.getByRole('dialog')).getByLabelText(/Reason/), 'Too far')
    await user.click(within(screen.getByRole('dialog')).getByRole('button', { name: en.inbox.declineConfirm }))
    await waitFor(() => expect(calls.declined).toEqual([{ reason: 'Too far' }]))
  })

  it('shows the partner their offers on the request and lets them withdraw one', async () => {
    const user = userEvent.setup()
    signedInAs(PARTNER_USER)
    const calls = inboxApi({ request: inboxRequest({ myStatus: 'Responded', customerFirstName: null }), offers: [offer()] })
    renderRoute('/en/inbox/req-1')

    expect(await screen.findByRole('heading', { name: en.offers.yours })).toBeInTheDocument()
    expect(screen.getAllByText(en.inbox.customerNoName).length).toBeGreaterThan(0)
    expect(screen.queryByRole('button', { name: en.inbox.decline })).not.toBeInTheDocument()
    await user.click(screen.getByRole('button', { name: en.offers.withdraw }))
    await waitFor(() => expect(calls.withdrawn).toEqual(['offer-1']))
  })

  it('explains requests that ended or that the partner declined', async () => {
    signedInAs(PARTNER_USER)
    inboxApi({ request: inboxRequest({ requestStatus: 'Closed', myStatus: 'Declined', kind: 'Direct' }), offers: [offer({ status: 'Accepted', orderId: 'order-9' })] })
    renderRoute('/en/inbox/req-1')

    expect(await screen.findByText(en.inbox.requestEnded.Closed)).toBeInTheDocument()
    expect(screen.getByText(en.inbox.youDeclined)).toBeInTheDocument()
    expect(screen.queryByRole('link', { name: en.inbox.sendOffer })).not.toBeInTheDocument()
    expect(await screen.findByRole('link', { name: en.offers.openOrder })).toHaveAttribute('href', '/en/orders/order-9')
  })

  it('says when a received request does not exist', async () => {
    signedInAs(PARTNER_USER)
    inboxApi()
    renderRoute('/en/inbox/nope')
    expect(await screen.findByText(en.inbox.notFoundText)).toBeInTheDocument()
  })
})
