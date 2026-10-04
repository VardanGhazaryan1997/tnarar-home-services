import { fireEvent, screen, waitFor, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { http, HttpResponse } from 'msw'
import en from '@/i18n/locales/en/common.json'
import { PARTNER_USER, problem, signedInAs } from '@/test/auth'
import { inboxRequest, myOfferItem, offer, page } from '@/test/fixtures'
import { renderRoute } from '@/test/renderWithProviders'
import { server } from '@/test/server'

function builderApi(respond) {
  const sent = []
  server.use(
    http.get('*/api/v1/requests/inbox/:id', () => HttpResponse.json(inboxRequest())),
    http.get('*/api/v1/requests/:id/offers', () => HttpResponse.json([])),
    http.post('*/api/v1/requests/:id/offers', async ({ request }) => {
      const body = await request.json()
      sent.push(body)
      return respond ? respond(body) : HttpResponse.json(offer(), { status: 201 })
    }),
  )
  return sent
}

describe('Offer builder', () => {
  it('builds a work offer with items and a payment plan that must add up to the price', async () => {
    const user = userEvent.setup()
    signedInAs(PARTNER_USER)
    const sent = builderApi()
    const { router } = renderRoute('/en/inbox/req-1/offer')

    await user.type(await screen.findByLabelText(en.offers.fields.summary), 'Replace the tap and the pipes.')
    await user.click(screen.getByRole('button', { name: en.offers.addIncluded }))
    await user.type(screen.getByLabelText('Item 1'), 'Remove the old tap')
    await user.click(screen.getByRole('button', { name: en.offers.addExcluded }))
    await user.type(screen.getByLabelText('Item 2'), 'Tiling')
    expect(screen.getByLabelText('Item 2 is')).toHaveValue('out')
    await user.click(screen.getByRole('button', { name: en.offers.addIncluded }))
    await user.click(screen.getByRole('button', { name: 'Remove item 3' }))
    await user.click(screen.getByLabelText(en.offers.fields.materialsIncluded))
    await user.type(screen.getByLabelText(en.offers.fields.price), '100000')
    await user.type(screen.getByLabelText(/Duration/), '2')

    await user.click(screen.getByRole('button', { name: en.offers.addStage }))
    await user.type(screen.getByLabelText('Payment 1 amount (֏)'), '30000')
    await user.click(screen.getByRole('button', { name: en.offers.addStage }))
    expect(screen.getByLabelText('Payment 2 type')).toHaveValue('Final')
    await user.type(screen.getByLabelText('Payment 2 amount (֏)'), '60000')
    expect(screen.getByText('Payments add up to 90,000 ֏ of 100,000 ֏')).toHaveClass('offer-builder__sum--wrong')

    await user.click(screen.getByRole('button', { name: en.offers.send }))
    expect(screen.getByText(en.errors.stages.sum_mismatch)).toBeInTheDocument()
    expect(sent).toHaveLength(0)

    await user.clear(screen.getByLabelText('Payment 2 amount (֏)'))
    await user.type(screen.getByLabelText('Payment 2 amount (֏)'), '70000')
    await user.selectOptions(screen.getByLabelText(en.offers.fields.validDays), '14')
    await user.click(screen.getByRole('button', { name: en.offers.send }))

    await waitFor(() => expect(router.state.location.pathname).toBe('/en/inbox/req-1'))
    expect(sent).toEqual([
      {
        kind: 'Work',
        summary: 'Replace the tap and the pipes.',
        lines: [
          { title: 'Remove the old tap', included: true },
          { title: 'Tiling', included: false },
        ],
        price: 100000,
        materialsIncluded: true,
        materialsNote: null,
        startDate: null,
        durationDays: 2,
        visitAt: null,
        stages: [
          { title: null, purpose: 'Deposit', amount: 30000 },
          { title: null, purpose: 'Final', amount: 70000 },
        ],
        validDays: 14,
      },
    ])
    expect(await screen.findByText(en.offers.sent)).toBeInTheDocument()
  })

  it('proposes a free visit', async () => {
    const user = userEvent.setup()
    signedInAs(PARTNER_USER)
    const sent = builderApi()
    renderRoute('/en/inbox/req-1/offer')

    await user.click(await screen.findByRole('radio', { name: en.offers.kindChoice.Visit }))
    expect(screen.queryByRole('button', { name: en.offers.addStage })).not.toBeInTheDocument()
    await user.click(screen.getByRole('button', { name: en.offers.send }))
    expect(screen.getByText(en.offers.errors.visitAtRequired)).toBeInTheDocument()
    expect(screen.getByText('Write at least 10 characters.')).toBeInTheDocument()

    await user.type(screen.getByLabelText(en.offers.fields.visitSummary), 'I need to see the boiler first.')
    await user.type(screen.getByLabelText(en.offers.fields.fee), '0')
    // Typing into a datetime-local field one key at a time is unreliable in jsdom: set the value as a picker would.
    fireEvent.change(screen.getByLabelText(en.offers.fields.visitAt), { target: { value: '2026-10-08T10:30' } })
    await user.click(screen.getByRole('button', { name: en.offers.send }))

    await waitFor(() => expect(sent).toHaveLength(1))
    expect(sent[0]).toMatchObject({ kind: 'Visit', price: 0, lines: [], stages: [], startDate: null, durationDays: null, materialsIncluded: false, validDays: 7 })
    expect(new Date(sent[0].visitAt).toISOString()).toBe(new Date('2026-10-08T10:30').toISOString())
  })

  it('shows API errors on the fields and rule errors above the button', async () => {
    const user = userEvent.setup()
    signedInAs(PARTNER_USER)
    let call = 0
    builderApi(() => {
      call += 1
      return call === 1 ? problem(400, 'validation_failed', { errors: { price: ['price.invalid'] } }) : problem(422, 'offer.already_sent')
    })
    renderRoute('/en/inbox/req-1/offer')

    await user.type(await screen.findByLabelText(en.offers.fields.summary), 'Replace the tap and the pipes.')
    await user.type(screen.getByLabelText(en.offers.fields.price), '5')
    await user.click(screen.getByRole('button', { name: en.offers.send }))
    expect(await screen.findByText(en.errors.price.invalid)).toBeInTheDocument()

    await user.click(screen.getByRole('button', { name: en.offers.send }))
    expect(await screen.findByText(en.errors.offer.already_sent)).toBeInTheDocument()
  })

  it('checks line names before sending', async () => {
    const user = userEvent.setup()
    signedInAs(PARTNER_USER)
    builderApi()
    renderRoute('/en/inbox/req-1/offer')

    await user.type(await screen.findByLabelText(en.offers.fields.summary), 'Replace the tap and the pipes.')
    await user.type(screen.getByLabelText(en.offers.fields.price), '5000')
    await user.click(screen.getByRole('button', { name: en.offers.addIncluded }))
    await user.click(screen.getByRole('button', { name: en.offers.send }))
    expect(screen.getByText(en.errors.lines.invalid)).toBeInTheDocument()
    await user.click(screen.getByRole('button', { name: en.offers.addStage }))
    await user.click(screen.getByRole('button', { name: 'Remove payment 1' }))
  })
})

describe('Sent offers', () => {
  it('lists the partner offers by status, accepted ones linking to their order', async () => {
    const user = userEvent.setup()
    signedInAs(PARTNER_USER)
    const statuses = []
    server.use(
      http.get('*/api/v1/offers/mine', ({ request }) => {
        statuses.push(new URL(request.url).searchParams.get('status'))
        return HttpResponse.json(page([myOfferItem(), myOfferItem({ id: 'offer-2', kind: 'Visit', price: 0, status: 'Accepted', orderId: 'order-1' })]))
      }),
    )
    renderRoute('/en/offers')

    const cards = within(await within(await screen.findByRole('main')).findByRole('list')).getAllByRole('link')
    expect(cards[0]).toHaveAttribute('href', '/en/inbox/req-1')
    expect(cards[0]).toHaveTextContent('100,000 ֏ · sent Oct 5, 2026')
    expect(cards[1]).toHaveAttribute('href', '/en/orders/order-1')
    expect(cards[1]).toHaveTextContent(en.offers.free)

    await user.click(screen.getByRole('radio', { name: en.offers.status.Accepted }))
    await waitFor(() => expect(statuses.at(-1)).toBe('Accepted'))
  })

  it('shows an empty state', async () => {
    signedInAs(PARTNER_USER)
    server.use(http.get('*/api/v1/offers/mine', () => HttpResponse.json(page([]))))
    renderRoute('/en/offers')
    expect(await screen.findByText(en.offers.mineEmpty)).toBeInTheDocument()
  })
})
