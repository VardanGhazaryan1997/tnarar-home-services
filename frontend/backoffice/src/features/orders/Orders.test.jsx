import { screen, waitFor, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { http, HttpResponse } from 'msw'
import hy from '@/i18n/locales/hy/common.json'
import { problem, signedInAs, staffMember } from '@/test/auth'
import { orderDetail, orderPayment, orderRow } from '@/test/operations'
import { page } from '@/test/partners'
import { renderRoute } from '@/test/renderWithProviders'
import { server } from '@/test/server'

// Several dialogs in one test: slow machines need more than the default 60 s.
const SLOW = 180_000

describe('Orders page', () => {
  it('lists orders and filters them', async () => {
    const requests = []
    server.use(
      http.get('*/api/v1/admin/orders', ({ request }) => {
        requests.push(Object.fromEntries(new URL(request.url).searchParams))
        return HttpResponse.json(page([orderRow({ pendingChange: true })], { totalCount: 30 }))
      }),
    )
    const user = userEvent.setup()
    renderRoute('/orders')

    const table = await screen.findByRole('table')
    expect(await within(table).findByRole('link', { name: 'Ծորակի փոխարինում' })).toHaveAttribute('href', '/orders/order-1')
    expect(within(table).getByText(hy.orders.pendingChange)).toBeInTheDocument()
    expect(requests[0]).toEqual({ page: '1', pageSize: '20' })

    await user.click(screen.getAllByText(hy.orders.needsAttention)[0])
    await waitFor(() => expect(requests.at(-1)).toEqual({ needsAttention: 'true', page: '1', pageSize: '20' }))
    await user.click(screen.getByText(hy.orders.status.InProgress))
    await user.type(screen.getByRole('searchbox', { name: hy.orders.filters.search }), 'Անի{Enter}')
    await waitFor(() => expect(requests.at(-1)).toEqual({ status: 'InProgress', search: 'Անի', page: '1', pageSize: '20' }))
    await user.click(await screen.findByTitle('2'))
    await waitFor(() => expect(requests.at(-1)).toEqual({ status: 'InProgress', search: 'Անի', page: '2', pageSize: '20' }))
    await user.click(screen.getByText(hy.orders.filters.all))
    await waitFor(() => expect(requests.at(-1)).toEqual({ search: 'Անի', page: '1', pageSize: '20' }))
  })

  it('shows list errors', async () => {
    server.use(http.get('*/api/v1/admin/orders', () => problem(500, 'boom')))
    renderRoute('/orders?show=attention')
    expect(await screen.findByText(hy.errors.generic)).toBeInTheDocument()
  })
})

describe('Order detail', () => {
  it('shows the order and lets staff resolve it', async () => {
    let resolved = 0
    server.use(
      http.post('*/api/v1/admin/orders/:id/resolve', () => {
        resolved += 1
        return resolved === 1 ? HttpResponse.json(orderDetail({ needsAttentionSince: null, actions: [] })) : problem(422, 'order.not_flagged')
      }),
    )
    const user = userEvent.setup()
    renderRoute('/orders/order-1')

    expect(await screen.findByText('Ծորակի և խողովակների փոխարինում')).toBeInTheDocument()
    expect(screen.getByText(hy.orders.detail.attentionText)).toBeInTheDocument()
    expect(screen.getAllByText('Աշխատանքը կիսատ է').length).toBeGreaterThan(0)
    expect(screen.getByText('Չեմ վճարել')).toBeInTheDocument()
    expect(screen.getByText(hy.orders.changeKind.ExtraWork)).toBeInTheDocument()

    await user.click(screen.getByRole('button', { name: new RegExp(hy.orders.resolve.button) }))
    expect(await screen.findByText(hy.orders.resolve.done)).toBeInTheDocument()
    await user.click(await screen.findByRole('button', { name: new RegExp(hy.orders.resolve.button) }))
    expect(await screen.findByText(hy.errors.order.not_flagged)).toBeInTheDocument()
  }, SLOW)

  it('cancels an open order with a reason', async () => {
    const bodies = []
    server.use(
      http.get('*/api/v1/admin/orders/:id', () =>
        HttpResponse.json(orderDetail({ status: 'Confirmed', cancelReason: null, cancelledBy: null, needsAttentionSince: null, kind: 'Visit', visitAt: '2026-10-07T10:00:00Z', payments: [], actions: ['cancel'] })),
      ),
      http.post('*/api/v1/admin/orders/:id/cancel', async ({ request }) => {
        bodies.push(await request.json())
        return bodies.length === 1 ? HttpResponse.json(orderDetail()) : problem(500, 'boom')
      }),
    )
    const user = userEvent.setup()
    renderRoute('/orders/order-1')

    expect(await screen.findByText(hy.orders.detail.noPayments)).toBeInTheDocument()
    await user.click(screen.getByRole('button', { name: hy.orders.cancel.button }))
    let dialog = (await screen.findAllByRole('dialog')).at(-1)
    await user.type(within(dialog).getByRole('textbox'), 'Կեղծ պատվեր')
    await user.click(within(dialog).getByRole('button', { name: hy.orders.cancel.confirm }))
    await waitFor(() => expect(bodies).toEqual([{ reason: 'Կեղծ պատվեր' }]))
    expect(await screen.findByText(hy.orders.cancel.done)).toBeInTheDocument()

    await user.click(screen.getByRole('button', { name: hy.orders.cancel.button }))
    dialog = (await screen.findAllByRole('dialog')).at(-1)
    await user.type(within(dialog).getByRole('textbox'), 'Կրկին')
    await user.click(within(dialog).getByRole('button', { name: hy.orders.cancel.confirm }))
    expect(await screen.findByText(hy.errors.generic)).toBeInTheDocument()
  }, SLOW)

  it('decides a disputed payment from the order', async () => {
    const bodies = []
    server.use(
      http.post('*/api/v1/admin/payments/:id/resolve', async ({ request }) => {
        bodies.push(await request.json())
        return HttpResponse.json({ id: 'payment-1', orderId: 'order-1', status: 'Confirmed' })
      }),
    )
    const user = userEvent.setup()
    renderRoute('/orders/order-1')

    await user.click(await screen.findByRole('button', { name: hy.payments.resolve.button }))
    const dialog = await screen.findByRole('dialog')
    await user.click(within(dialog).getByRole('button', { name: hy.payments.resolve.confirm }))
    expect(await within(dialog).findByText(hy.payments.resolve.decisionRequired)).toBeInTheDocument()
    await user.click(within(dialog).getByLabelText(hy.payments.resolve.counts))
    await user.click(within(dialog).getByRole('button', { name: hy.payments.resolve.confirm }))
    await waitFor(() => expect(bodies).toEqual([{ counts: true, note: null }]))
    expect(await screen.findByText(hy.payments.resolve.doneCounts)).toBeInTheDocument()
  }, SLOW)

  it('moderates the review from the order', async () => {
    const calls = []
    server.use(
      http.get('*/api/v1/admin/orders/:id', () =>
        HttpResponse.json(
          orderDetail({
            status: 'Completed',
            needsAttentionSince: null,
            cancelReason: null,
            actions: [],
            payments: [orderPayment({ status: 'Confirmed', disputeReason: null })],
            review: { id: 'review-1', rating: 2, text: 'Վատ էր', submittedAt: '2026-10-09T10:00:00Z', reply: 'Կներեք', repliedAt: null, isHidden: calls.length > 0, hiddenReason: calls.length > 0 ? 'Վիրավորանք' : null },
          }),
        ),
      ),
      http.post('*/api/v1/admin/reviews/:id/:action', async ({ params, request }) => {
        calls.push({ action: params.action, body: params.action === 'hide' ? await request.json() : null })
        return HttpResponse.json({ id: 'review-1', orderId: 'order-1' })
      }),
    )
    const user = userEvent.setup()
    renderRoute('/orders/order-1')

    expect(await screen.findByText('Վատ էր')).toBeInTheDocument()
    await user.click(screen.getByRole('button', { name: hy.reviews.hide.button }))
    const dialog = await screen.findByRole('dialog')
    await user.type(within(dialog).getByRole('textbox'), 'Վիրավորանք')
    await user.click(within(dialog).getByRole('button', { name: hy.reviews.hide.confirm }))
    await waitFor(() => expect(calls).toEqual([{ action: 'hide', body: { reason: 'Վիրավորանք' } }]))
    await user.click(await screen.findByRole('button', { name: hy.reviews.restore.button }))
    await waitFor(() => expect(calls.at(-1)).toEqual({ action: 'restore', body: null }))
  }, SLOW)

  it('shows only what the staff member may do, and load errors', async () => {
    signedInAs(staffMember(['orders.view']))
    renderRoute('/orders/order-1')
    expect(await screen.findByText('Ծորակի և խողովակների փոխարինում')).toBeInTheDocument()
    expect(screen.queryByRole('button', { name: hy.orders.resolve.button })).not.toBeInTheDocument()
    expect(screen.queryByRole('button', { name: hy.payments.resolve.button })).not.toBeInTheDocument()
  })

  it('says when the order does not exist', async () => {
    server.use(http.get('*/api/v1/admin/orders/:id', () => problem(404, 'order.not_found')))
    renderRoute('/orders/missing')
    expect(await screen.findByText(hy.errors.order.not_found)).toBeInTheDocument()
  })
})
