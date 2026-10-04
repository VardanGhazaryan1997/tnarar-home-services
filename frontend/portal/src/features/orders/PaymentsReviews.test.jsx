import { screen, waitFor, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { http, HttpResponse } from 'msw'
import en from '@/i18n/locales/en/common.json'
import { CUSTOMER, PARTNER_USER, problem, signedInAs } from '@/test/auth'
import { order, orderReview, payment } from '@/test/fixtures'
import { renderRoute } from '@/test/renderWithProviders'
import { server } from '@/test/server'

/** Serves `detail` and answers POSTs to /orders/:id/* and /payments/:id/* with `respond(action, body, current)`. */
function orderApi(detail, respond = (_action, _body, current) => current) {
  const calls = []
  let current = detail
  const handle = async ({ request }) => {
    const action = new URL(request.url).pathname.replace(/^.*\/api\/v1\//, '')
    const body = request.headers.get('content-type')?.includes('json') ? await request.json() : null
    calls.push({ action, body })
    const result = respond(action, body, current)
    if (result instanceof Response) return result
    current = result
    return HttpResponse.json(current)
  }
  server.use(
    http.get('*/api/v1/orders/:id', () => HttpResponse.json(current)),
    http.post('*/api/v1/orders/:id/*', handle),
    http.post('*/api/v1/payments/:id/*', handle),
  )
  return calls
}

const paymentsCard = async () => (await screen.findByRole('heading', { name: en.orders.pay.heading })).closest('section')

describe('Order payments', () => {
  it('lets the customer record a payment', async () => {
    const user = userEvent.setup()
    signedInAs(CUSTOMER)
    const stages = [
      { id: 'stage-1', title: null, purpose: 'Deposit', amount: 40000 },
      { id: 'stage-2', title: null, purpose: 'Final', amount: 60000 },
    ]
    const calls = orderApi(order({ stages, actions: ['proposeChange', 'cancel', 'recordPayment'] }), (_action, body, current) => ({
      ...current,
      payments: [payment({ amount: body.amount, recordedBy: 'Customer', mine: true, method: body.method })],
    }))
    renderRoute('/en/orders/order-1')

    const card = await paymentsCard()
    expect(within(card).getByText(en.orders.pay.none.Customer)).toBeInTheDocument()
    await user.click(within(card).getByRole('button', { name: en.orders.pay.record.Customer }))
    const dialog = await screen.findByRole('dialog')
    await user.click(within(dialog).getByRole('button', { name: en.orders.pay.save }))
    expect(within(dialog).getByText(en.orders.pay.errors.amount)).toBeInTheDocument()

    await user.type(within(dialog).getByLabelText(en.orders.pay.amount, { exact: false }), '40000')
    await user.selectOptions(within(dialog).getByLabelText(en.orders.pay.method, { exact: false }), 'BankTransfer')
    await user.selectOptions(within(dialog).getByLabelText(en.orders.pay.stage, { exact: false }), 'stage-1')
    await user.click(within(dialog).getByRole('button', { name: en.orders.pay.save }))

    await waitFor(() => expect(calls).toHaveLength(1))
    expect(calls[0].action).toBe('orders/order-1/payments')
    expect(calls[0].body).toMatchObject({ amount: 40000, method: 'BankTransfer', stageId: 'stage-1', note: null })
    expect(await within(card).findByText(en.orders.pay.status.Pending)).toBeInTheDocument()
    expect(within(card).getByRole('button', { name: en.orders.pay.withdraw })).toBeInTheDocument()
  })

  it('does not let the customer record more than is left to pay', async () => {
    const user = userEvent.setup()
    signedInAs(CUSTOMER)
    orderApi(order({ payments: [payment({ amount: 90000, status: 'Confirmed' })], paidAmount: 90000, actions: ['recordPayment'] }))
    renderRoute('/en/orders/order-1')

    const card = await paymentsCard()
    expect(within(card).getByRole('progressbar')).toHaveAttribute('aria-valuenow', '90')
    await user.click(within(card).getByRole('button', { name: en.orders.pay.record.Customer }))
    const dialog = await screen.findByRole('dialog')
    await user.type(within(dialog).getByLabelText(en.orders.pay.amount, { exact: false }), '20000')
    await user.click(within(dialog).getByRole('button', { name: en.orders.pay.save }))
    expect(within(dialog).getByText(/payments can't add up to more than the price/)).toBeInTheDocument()
  })

  it('lets the other side confirm or dispute a payment', async () => {
    const user = userEvent.setup()
    signedInAs(CUSTOMER)
    const calls = orderApi(
      order({
        payments: [payment(), payment({ id: 'payment-2', amount: 10000 })],
        actions: ['recordPayment'],
      }),
      (action, body, current) => ({
        ...current,
        payments: current.payments.map((item) =>
          action.startsWith(`payments/${item.id}/confirm`)
            ? { ...item, status: 'Confirmed' }
            : action.startsWith(`payments/${item.id}/dispute`)
              ? { ...item, status: 'Disputed', disputeReason: body.reason }
              : item,
        ),
        paidAmount: action.includes('confirm') ? 40000 : current.paidAmount,
      }),
    )
    renderRoute('/en/orders/order-1')

    const card = await paymentsCard()
    const [first] = within(card).getAllByRole('button', { name: en.orders.pay.confirm.Customer })
    await user.click(first)
    await waitFor(() => expect(calls.map((call) => call.action)).toEqual(['payments/payment-1/confirm']))
    expect(await within(card).findByText(en.orders.pay.status.Confirmed)).toBeInTheDocument()

    await user.click(within(card).getByRole('button', { name: en.orders.pay.dispute }))
    const dialog = await screen.findByRole('dialog')
    await user.type(within(dialog).getByRole('textbox'), 'I paid only 5 000')
    await user.click(within(dialog).getByRole('button', { name: en.orders.pay.dispute }))
    await waitFor(() => expect(calls.at(-1)).toEqual({ action: 'payments/payment-2/dispute', body: { reason: 'I paid only 5 000' } }))
    expect(await within(card).findByText(en.orders.pay.withTeam)).toBeInTheDocument()
  })

  it('shows why a payment can no longer be answered', async () => {
    const user = userEvent.setup()
    signedInAs(PARTNER_USER)
    orderApi(order({ myRole: 'Partner', payments: [payment({ recordedBy: 'Customer' })], actions: ['recordPayment'] }), () =>
      problem(422, 'payment.not_pending'),
    )
    renderRoute('/en/orders/order-1')

    const card = await paymentsCard()
    await user.click(within(card).getByRole('button', { name: en.orders.pay.confirm.Partner }))
    expect(await within(card).findByText(en.errors.payment.not_pending)).toBeInTheDocument()
  })
})

describe('Order review', () => {
  it('lets the customer review a completed order', async () => {
    const user = userEvent.setup()
    signedInAs(CUSTOMER)
    const calls = orderApi(order({ status: 'Completed', actions: ['recordPayment', 'review'] }), (_action, body, current) => ({
      ...current,
      review: orderReview({ rating: body.rating, text: body.text }),
      actions: ['recordPayment'],
    }))
    renderRoute('/en/orders/order-1')

    const button = await screen.findByRole('button', { name: en.reviews.submit })
    await user.click(button)
    expect(screen.getByText(en.reviews.ratingRequired)).toBeInTheDocument()

    await user.click(screen.getByRole('radio', { name: '4 out of 5 stars' }))
    await user.type(screen.getByLabelText(en.reviews.text, { exact: false }), 'Clean work')
    await user.click(button)

    await waitFor(() => expect(calls).toEqual([{ action: 'orders/order-1/review', body: { rating: 4, text: 'Clean work' } }]))
    expect(await screen.findByRole('img', { name: '4 out of 5' })).toBeInTheDocument()
    expect(screen.getByText('Clean work')).toBeInTheDocument()
    expect(screen.queryByRole('button', { name: en.reviews.submit })).not.toBeInTheDocument()
  })

  it('lets the partner reply once', async () => {
    const user = userEvent.setup()
    signedInAs(PARTNER_USER)
    const calls = orderApi(order({ myRole: 'Partner', status: 'Completed', review: orderReview(), actions: ['replyReview'] }), (_action, body, current) => ({
      ...current,
      review: { ...current.review, reply: body.text },
      actions: [],
    }))
    renderRoute('/en/orders/order-1')

    expect(await screen.findByText('Quick and tidy work.')).toBeInTheDocument()
    await user.type(screen.getByLabelText(en.reviews.replyLabel, { exact: false }), 'Thank you!')
    await user.click(screen.getByRole('button', { name: en.reviews.reply }))

    await waitFor(() => expect(calls).toEqual([{ action: 'orders/order-1/review/reply', body: { text: 'Thank you!' } }]))
    expect(await screen.findByText('Reply from Aram Plumbing')).toBeInTheDocument()
    expect(screen.queryByRole('button', { name: en.reviews.reply })).not.toBeInTheDocument()
  })
})
