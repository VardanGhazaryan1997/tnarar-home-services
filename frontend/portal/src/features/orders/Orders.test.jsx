import { fireEvent, screen, waitFor, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { http, HttpResponse } from 'msw'
import en from '@/i18n/locales/en/common.json'
import { CUSTOMER, PARTNER_USER, problem, signedInAs } from '@/test/auth'
import { order, orderChange, orderItem, page } from '@/test/fixtures'
import { renderRoute } from '@/test/renderWithProviders'
import { server } from '@/test/server'

function ordersApi(items = [orderItem()], detail = order()) {
  const sides = []
  server.use(
    http.get('*/api/v1/orders', ({ request }) => {
      sides.push(new URL(request.url).searchParams.get('as'))
      return HttpResponse.json(page(items))
    }),
    http.get('*/api/v1/orders/:id', ({ params }) => (params.id === detail.id ? HttpResponse.json(detail) : problem(404, 'order.not_found'))),
  )
  return sides
}

describe('Orders', () => {
  it('lists the customer orders', async () => {
    signedInAs(CUSTOMER)
    ordersApi([
      orderItem(),
      orderItem({
        id: 'order-2',
        kind: 'Visit',
        price: 0,
        startDate: null,
        visitAt: '2026-10-06T10:00:00Z',
        otherParty: null,
      }),
    ])
    renderRoute('/en/orders')

    const cards = within(await within(await screen.findByRole('main')).findByRole('list')).getAllByRole('link')
    expect(cards[0]).toHaveAttribute('href', '/en/orders/order-1')
    expect(cards[0]).toHaveTextContent('Aram Plumbing · 100,000 ֏ · Oct 8, 2026')
    expect(cards[1]).toHaveTextContent(`${en.offers.free} · Oct 6, 2026`)
    expect(screen.queryByRole('radiogroup')).not.toBeInTheDocument()
  })

  it('lets partners show one side', async () => {
    const user = userEvent.setup()
    signedInAs(PARTNER_USER)
    const sides = ordersApi([orderItem({ myRole: 'Partner', otherParty: 'Ani Petrosyan' })])
    renderRoute('/en/orders')

    expect(await screen.findByText(en.orders.as.Partner)).toBeInTheDocument()
    await user.click(screen.getByRole('radio', { name: en.orders.as.Customer }))
    await waitFor(() => expect(sides.at(-1)).toBe('Customer'))
  })

  it('shows an empty list', async () => {
    signedInAs(CUSTOMER)
    ordersApi([])
    renderRoute('/en/orders')
    expect(await screen.findByText(en.orders.emptyTitle)).toBeInTheDocument()
  })

  it('shows the customer the agreed terms and the specialist contacts', async () => {
    signedInAs(CUSTOMER)
    ordersApi()
    renderRoute('/en/orders/order-1')

    expect(
      await screen.findByRole('heading', {
        level: 1,
        name: en.orders.workTitle,
      }),
    ).toBeInTheDocument()
    expect(screen.getByText('Replace the tap and the pipes under the sink.')).toBeInTheDocument()
    expect(screen.getByRole('heading', { name: en.orders.yourSpecialist })).toBeInTheDocument()
    expect(screen.getByRole('link', { name: '+37499111222' })).toHaveAttribute('href', 'tel:+37499111222')
    expect(screen.getByText(en.offers.startToAgree)).toBeInTheDocument()
    expect(screen.getAllByText(en.orders.status.Confirmed).length).toBeGreaterThan(0)
  })

  it('shows the partner the customer, and a visit order with its time', async () => {
    signedInAs(PARTNER_USER)
    ordersApi(
      [],
      order({
        kind: 'Visit',
        myRole: 'Partner',
        price: 0,
        customer: { userId: 'user-1', fullName: null, phone: '+37491234567' },
        terms: {
          ...order().terms,
          visitAt: '2026-10-06T10:00:00Z',
          stages: [],
        },
      }),
    )
    renderRoute('/en/orders/order-1')

    expect(
      await screen.findByRole('heading', {
        level: 1,
        name: en.orders.visitTitle,
      }),
    ).toBeInTheDocument()
    expect(screen.getByRole('heading', { name: en.orders.yourCustomer })).toBeInTheDocument()
    expect(screen.getByRole('link', { name: '+37491234567' })).toBeInTheDocument()
    expect(screen.getByText(en.offers.free)).toBeInTheDocument()
  })

  it('says when an order does not exist', async () => {
    signedInAs(CUSTOMER)
    ordersApi()
    renderRoute('/en/orders/nope')
    expect(await screen.findByText(en.orders.notFoundText)).toBeInTheDocument()
  })
})

const t = (text, values = {}) => Object.entries(values).reduce((out, [key, value]) => out.replace(`{{${key}}}`, value), text)

/** Serves one order and answers each action with `respond(action, body)`; records the calls. */
function orderActions(detail, respond) {
  const calls = []
  let current = detail
  server.use(
    http.get('*/api/v1/orders/:id', () => HttpResponse.json(current)),
    http.post('*/api/v1/orders/:id/*', async ({ request }) => {
      const action = new URL(request.url).pathname.replace(/^.*\/orders\/[^/]+\//, '')
      const body = request.headers.get('content-type')?.includes('json') ? await request.json() : null
      calls.push({ action, body })
      const result = respond(action, body, current)
      if (result instanceof Response) return result
      current = result
      return HttpResponse.json(current)
    }),
  )
  return calls
}

describe('Order steps', () => {
  it('lets the partner start and mark the work as done', async () => {
    const user = userEvent.setup()
    signedInAs(PARTNER_USER)
    const calls = orderActions(order({ myRole: 'Partner', actions: ['start', 'requestCompletion', 'proposeChange', 'cancel'] }), (action, _body, current) =>
      action === 'start'
        ? { ...current, status: 'InProgress', actions: ['requestCompletion', 'proposeChange', 'cancel'], history: [...current.history, { status: 'InProgress', changedAt: '2026-10-06T08:00:00Z', by: 'Partner', note: null }] }
        : { ...current, status: 'CompletionRequested', autoCompleteAt: '2026-10-13T08:00:00Z', actions: ['cancel'], history: [...current.history, { status: 'CompletionRequested', changedAt: '2026-10-06T18:00:00Z', by: 'Partner', note: null }] },
    )
    renderRoute('/en/orders/order-1')

    expect(await screen.findByText(en.orders.next.Partner.Confirmed)).toBeInTheDocument()
    const progress = screen.getByRole('list', { name: en.orders.progressLabel })
    expect(within(progress).getAllByRole('listitem')[0]).toHaveAttribute('aria-current', 'step')
    await user.click(screen.getByRole('button', { name: en.orders.actions.start }))
    expect(await screen.findByText(en.orders.next.Partner.InProgress)).toBeInTheDocument()
    expect(screen.getByText(en.orders.event.InProgress)).toBeInTheDocument()
    expect(screen.queryByRole('button', { name: en.orders.actions.start })).not.toBeInTheDocument()

    await user.click(screen.getByRole('button', { name: en.orders.actions.requestCompletion }))
    expect(await screen.findByText(t(en.orders.next.Partner.CompletionRequested, { date: 'Oct 13, 2026' }))).toBeInTheDocument()
    expect(calls.map((c) => c.action)).toEqual(['start', 'request-completion'])
    expect(screen.queryByRole('button', { name: en.orders.actions.proposeChange })).not.toBeInTheDocument()
  })

  it('lets the customer confirm, or send it back with a reason', async () => {
    const user = userEvent.setup()
    signedInAs(CUSTOMER)
    const done = order({
      status: 'CompletionRequested',
      autoCompleteAt: '2026-10-13T08:00:00Z',
      actions: ['confirmCompletion', 'rejectCompletion', 'cancel'],
      history: [
        { status: 'Confirmed', changedAt: '2026-10-05T11:00:00Z', by: 'Customer', note: null },
        { status: 'CompletionRequested', changedAt: '2026-10-06T18:00:00Z', by: 'Partner', note: null },
      ],
    })
    const calls = orderActions(done, (action, body, current) =>
      action === 'reject-completion'
        ? { ...current, status: 'InProgress', actions: ['proposeChange', 'cancel'], history: [...current.history, { status: 'InProgress', changedAt: '2026-10-07T09:00:00Z', by: 'Customer', note: body.reason }] }
        : { ...current, status: 'Completed', actions: [] },
    )
    renderRoute('/en/orders/order-1')

    expect(await screen.findByText(t(en.orders.next.Customer.CompletionRequested, { date: 'Oct 13, 2026' }))).toBeInTheDocument()
    await user.click(screen.getByRole('button', { name: en.orders.actions.rejectCompletion }))
    const dialog = screen.getByRole('dialog', { name: en.orders.rejectCompletion.title })
    await user.click(within(dialog).getByRole('button', { name: en.orders.rejectCompletion.confirm }))
    expect(within(dialog).getByText(en.orders.reasonRequired)).toBeInTheDocument()
    await user.type(within(dialog).getByLabelText(en.orders.rejectCompletion.label), 'The tap still drips')
    await user.click(within(dialog).getByRole('button', { name: en.orders.rejectCompletion.confirm }))

    await waitFor(() => expect(screen.queryByRole('dialog')).not.toBeInTheDocument())
    expect(calls[0]).toEqual({ action: 'reject-completion', body: { reason: 'The tap still drips' } })
    expect(screen.getByText(en.orders.event.Reopened)).toBeInTheDocument()
    expect(screen.getByText('The tap still drips')).toBeInTheDocument()
  })

  it('confirms completion and shows action errors', async () => {
    const user = userEvent.setup()
    signedInAs(CUSTOMER)
    let attempt = 0
    orderActions(order({ status: 'CompletionRequested', autoCompleteAt: '2026-10-13T08:00:00Z', actions: ['confirmCompletion', 'rejectCompletion', 'cancel'] }), (_action, _body, current) => {
      attempt += 1
      return attempt === 1 ? problem(409, 'concurrency_conflict') : { ...current, status: 'Completed', actions: [] }
    })
    renderRoute('/en/orders/order-1')

    await user.click(await screen.findByRole('button', { name: en.orders.actions.confirmCompletion }))
    expect(await screen.findByText(en.errors.concurrency_conflict)).toBeInTheDocument()
    await user.click(screen.getByRole('button', { name: en.orders.actions.confirmCompletion }))
    expect(await screen.findByText(en.orders.next.Customer.Completed)).toBeInTheDocument()
    expect(screen.queryByRole('heading', { name: en.orders.manage })).not.toBeInTheDocument()
  })

  it('cancels with a reason and shows who cancelled', async () => {
    const user = userEvent.setup()
    signedInAs(CUSTOMER)
    const calls = orderActions(order({ status: 'InProgress', actions: ['proposeChange', 'cancel'] }), (_action, body, current) => ({
      ...current,
      status: 'Cancelled',
      cancelledBy: 'Customer',
      cancelledAt: '2026-10-07T09:00:00Z',
      cancelReason: body.reason,
      needsAttentionSince: '2026-10-07T09:00:00Z',
      actions: [],
    }))
    renderRoute('/en/orders/order-1')

    await user.click(await screen.findByRole('button', { name: en.orders.actions.cancel }))
    const dialog = screen.getByRole('dialog', { name: en.orders.cancel.title })
    expect(within(dialog).getByText(en.orders.cancel.textStarted)).toBeInTheDocument()
    await user.type(within(dialog).getByLabelText(en.orders.cancel.label), 'We are moving')
    await user.click(within(dialog).getByRole('button', { name: en.orders.cancel.confirm }))

    expect(await screen.findByText(/Cancelled by the customer on/)).toBeInTheDocument()
    expect(screen.getByText('We are moving')).toBeInTheDocument()
    expect(screen.getByText(en.orders.teamWillHelp)).toBeInTheDocument()
    expect(calls).toEqual([{ action: 'cancel', body: { reason: 'We are moving' } }])
  })

  it('shows a cancel error inside the dialog', async () => {
    const user = userEvent.setup()
    signedInAs(CUSTOMER)
    orderActions(order(), () => problem(422, 'order.cannot_cancel'))
    renderRoute('/en/orders/order-1')

    await user.click(await screen.findByRole('button', { name: en.orders.actions.cancel }))
    const dialog = screen.getByRole('dialog')
    expect(within(dialog).getByText(en.orders.cancel.text)).toBeInTheDocument()
    await user.type(within(dialog).getByLabelText(en.orders.cancel.label), 'No longer needed')
    await user.click(within(dialog).getByRole('button', { name: en.orders.cancel.confirm }))
    expect(await within(dialog).findByText(en.errors.order.cannot_cancel)).toBeInTheDocument()
    await user.click(within(dialog).getByRole('button', { name: en.common.back }))
    expect(screen.queryByRole('dialog')).not.toBeInTheDocument()
  })
})

describe('Order changes', () => {
  it('lets the customer accept extra work, which raises the price and adds a payment', async () => {
    const user = userEvent.setup()
    signedInAs(CUSTOMER)
    const stages = [
      { id: 's1', title: 'Deposit', purpose: 'Deposit', amount: 50000 },
      { id: 's2', title: null, purpose: 'Final', amount: 50000 },
    ]
    const calls = orderActions(order({ stages, changeRequests: [orderChange()], actions: ['answerChange', 'cancel'] }), (_action, _body, current) => ({
      ...current,
      price: 120000,
      stages: [stages[0], { id: 's3', title: 'Replace the siphon', purpose: 'Stage', amount: 20000 }, stages[1]],
      changeRequests: [orderChange({ status: 'Accepted', decidedAt: '2026-10-06T10:00:00Z' })],
      actions: ['proposeChange', 'cancel'],
    }))
    renderRoute('/en/orders/order-1')

    const card = (await screen.findByRole('heading', { name: en.orders.change.kind.ExtraWork })).closest('section')
    expect(within(card).getByText(en.orders.change.waitingForYou)).toBeInTheDocument()
    expect(within(card).getByText('+20,000 ֏')).toBeInTheDocument()
    expect(within(card).getByText(t(en.orders.change.extraWorkHint, { total: '120,000 ֏' }))).toBeInTheDocument()
    expect(screen.getByText(en.orders.change.oneAtATime)).toBeInTheDocument()
    await user.click(within(card).getByRole('button', { name: en.orders.change.accept }))

    expect(await screen.findByText(t(en.orders.priceChanged, { agreed: '100,000 ֏' }))).toBeInTheDocument()
    expect(calls).toEqual([{ action: 'change-requests/change-1/accept', body: null }])
    const plan = screen.getByRole('heading', { name: en.orders.payments }).closest('section')
    expect(within(plan).getAllByRole('listitem').map((li) => li.textContent)).toEqual(['1Deposit' + en.offers.purpose.Deposit + '50,000 ֏', '2Replace the siphon' + en.offers.purpose.Stage + '20,000 ֏', '3' + en.offers.purpose.Final + '50,000 ֏'])
    expect(within(plan).getByText('120,000 ֏')).toBeInTheDocument()
    const changes = screen.getByRole('heading', { name: en.orders.changes }).closest('section')
    expect(within(changes).getByText(en.orders.change.status.Accepted)).toBeInTheDocument()
  })

  it('declines a change with an optional note, and withdraws your own', async () => {
    const user = userEvent.setup()
    signedInAs(PARTNER_USER)
    const schedule = orderChange({ id: 'change-2', kind: 'Schedule', proposedBy: 'Customer', title: null, amount: null, newStartDate: '2026-10-12', newDurationDays: 3, description: null })
    const calls = orderActions(order({ myRole: 'Partner', changeRequests: [schedule], actions: ['answerChange', 'cancel'] }), (action, body, current) =>
      action.endsWith('reject')
        ? { ...current, changeRequests: [{ ...schedule, status: 'Rejected', responseNote: body.note }], actions: ['proposeChange', 'cancel'] }
        : current,
    )
    renderRoute('/en/orders/order-1')

    const card = (await screen.findByRole('heading', { name: en.orders.change.kind.Schedule })).closest('section')
    expect(within(card).getByText(t(en.orders.change.newStartDate, { date: 'Oct 12, 2026' }))).toBeInTheDocument()
    expect(within(card).getByText(t(en.orders.change.newDuration, { n: 3 }))).toBeInTheDocument()
    await user.click(within(card).getByRole('button', { name: en.orders.change.decline }))
    const dialog = screen.getByRole('dialog', { name: en.orders.change.declineTitle })
    await user.type(within(dialog).getByLabelText(new RegExp(en.orders.change.declineNote)), 'Fully booked')
    await user.click(within(dialog).getByRole('button', { name: en.orders.change.decline }))

    await waitFor(() => expect(calls).toEqual([{ action: 'change-requests/change-2/reject', body: { note: 'Fully booked' } }]))
    expect(await screen.findByText(/answer: Fully booked/)).toBeInTheDocument()
  })

  it('lets the proposer withdraw a waiting change', async () => {
    const user = userEvent.setup()
    signedInAs(PARTNER_USER)
    const calls = orderActions(order({ myRole: 'Partner', changeRequests: [orderChange({ mine: true })], actions: ['withdrawChange', 'cancel'] }), (_action, _body, current) => ({
      ...current,
      changeRequests: [orderChange({ mine: true, status: 'Withdrawn' })],
      actions: ['proposeChange', 'cancel'],
    }))
    renderRoute('/en/orders/order-1')

    expect(await screen.findByText(en.orders.change.waitingForThem)).toBeInTheDocument()
    expect(screen.queryByRole('button', { name: en.orders.change.accept })).not.toBeInTheDocument()
    await user.click(screen.getByRole('button', { name: en.orders.change.withdraw }))
    expect(await screen.findByText(en.orders.change.status.Withdrawn)).toBeInTheDocument()
    expect(calls[0].action).toBe('change-requests/change-1/withdraw')
  })

  it('proposes extra work or a new schedule, checking the fields first', async () => {
    const user = userEvent.setup()
    signedInAs(CUSTOMER)
    let attempt = 0
    const calls = orderActions(order(), (_action, body, current) => {
      attempt += 1
      if (attempt === 1) return problem(400, 'validation_failed', { errors: { amount: ['amount.invalid'] } })
      return { ...current, changeRequests: [orderChange({ ...body, id: `change-${attempt}`, mine: true, proposedBy: 'Customer' })], actions: ['withdrawChange', 'cancel'] }
    })
    renderRoute('/en/orders/order-1')

    await user.click(await screen.findByRole('button', { name: en.orders.actions.proposeChange }))
    const dialog = screen.getByRole('dialog', { name: en.orders.change.proposeTitle })
    await user.click(within(dialog).getByRole('button', { name: en.orders.change.send }))
    expect(within(dialog).getByText(en.orders.change.errors.title)).toBeInTheDocument()
    expect(within(dialog).getByText(en.orders.change.errors.amount)).toBeInTheDocument()

    await user.type(within(dialog).getByLabelText(en.orders.change.fields.title), 'Paint the wall')
    await user.type(within(dialog).getByLabelText(en.orders.change.fields.amount), '15000')
    await user.click(within(dialog).getByRole('button', { name: en.orders.change.send }))
    expect(await within(dialog).findByText(en.errors.amount.invalid)).toBeInTheDocument()

    await user.click(within(dialog).getByRole('radio', { name: en.orders.change.kind.Schedule }))
    await user.click(within(dialog).getByRole('button', { name: en.orders.change.send }))
    expect(within(dialog).getByText(en.orders.change.errors.schedule)).toBeInTheDocument()
    await user.type(within(dialog).getByLabelText(new RegExp(en.orders.change.fields.newDurationDays.replace(/[()]/g, '.'))), '4')
    await user.type(within(dialog).getByLabelText(new RegExp(en.orders.change.fields.description)), 'Weekends only')
    await user.click(within(dialog).getByRole('button', { name: en.orders.change.send }))

    await waitFor(() => expect(screen.queryByRole('dialog')).not.toBeInTheDocument())
    expect(calls[1].body).toEqual({ kind: 'Schedule', title: null, amount: null, description: 'Weekends only', newStartDate: null, newDurationDays: 4, newVisitAt: null })
    expect(await screen.findByText(en.orders.change.waitingForThem)).toBeInTheDocument()
  })

  it('moves a visit to a new time', async () => {
    const user = userEvent.setup()
    signedInAs(CUSTOMER)
    const calls = orderActions(order({ kind: 'Visit', price: 0, stages: [], terms: { ...order().terms, visitAt: '2026-10-06T10:00:00Z', stages: [] } }), (_action, body, current) => ({
      ...current,
      changeRequests: [orderChange({ kind: 'Schedule', title: null, amount: null, newVisitAt: body.newVisitAt, mine: true })],
      actions: ['withdrawChange', 'cancel'],
    }))
    renderRoute('/en/orders/order-1')

    await user.click(await screen.findByRole('button', { name: en.orders.actions.proposeChange }))
    const dialog = screen.getByRole('dialog')
    expect(within(dialog).queryByRole('radiogroup')).not.toBeInTheDocument()
    await user.click(within(dialog).getByRole('button', { name: en.orders.change.send }))
    expect(within(dialog).getByText(en.orders.change.errors.visitAt)).toBeInTheDocument()
    fireEvent.change(within(dialog).getByLabelText(en.orders.change.fields.newVisitAt), { target: { value: '2026-10-09T15:00' } })
    await user.click(within(dialog).getByRole('button', { name: en.orders.change.send }))

    await waitFor(() => expect(calls).toHaveLength(1))
    expect(calls[0].body).toMatchObject({ kind: 'Schedule', newVisitAt: new Date('2026-10-09T15:00').toISOString(), newStartDate: null, newDurationDays: null })
  })
})
