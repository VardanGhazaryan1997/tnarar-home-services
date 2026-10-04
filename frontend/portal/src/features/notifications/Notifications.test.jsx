import { act, screen, waitFor, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { http, HttpResponse } from 'msw'
import en from '@/i18n/locales/en/common.json'
import { CUSTOMER, signedInAs } from '@/test/auth'
import { notification, page } from '@/test/fixtures'
import { renderRoute } from '@/test/renderWithProviders'
import { server } from '@/test/server'
import { hub } from '@/test/signalr'

function notificationsApi(items) {
  const state = { items, reads: [], readAll: 0 }
  const unread = () => state.items.filter((item) => !item.readAt).length
  server.use(
    http.get('*/api/v1/notifications', ({ request }) => {
      const unreadOnly = new URL(request.url).searchParams.get('unreadOnly') === 'true'
      return HttpResponse.json(page(unreadOnly ? state.items.filter((item) => !item.readAt) : state.items))
    }),
    http.get('*/api/v1/notifications/unread-count', () => HttpResponse.json({ count: unread() })),
    http.post('*/api/v1/notifications/read-all', () => {
      state.readAll += 1
      const count = unread()
      state.items = state.items.map((item) => ({ ...item, readAt: item.readAt ?? '2026-10-06T10:00:00Z' }))
      return HttpResponse.json({ count })
    }),
    http.post('*/api/v1/notifications/:id/read', ({ params }) => {
      state.reads.push(params.id)
      state.items = state.items.map((item) => (item.id === params.id ? { ...item, readAt: '2026-10-06T10:00:00Z' } : item))
      return HttpResponse.json(state.items.find((item) => item.id === params.id))
    }),
  )
  return state
}

describe('Notifications', () => {
  it('shows the unread count on the bell and updates it live', async () => {
    signedInAs(CUSTOMER)
    const state = notificationsApi([notification()])
    renderRoute('/en/orders')

    const bell = await screen.findByRole('link', { name: 'Notifications: 1 unread' })
    expect(bell).toHaveAttribute('href', '/en/notifications')

    state.items = [notification({ id: 'notification-2', type: 'PaymentRecorded', params: { name: 'Aram', amount: '5000' } }), ...state.items]
    await waitFor(() => expect(hub.connections.length).toBeGreaterThan(0))
    act(() => hub.emit('notificationReceived', state.items[0]))
    expect(await screen.findByRole('link', { name: 'Notifications: 2 unread' })).toBeInTheDocument()
  })

  it('lists notifications in words and opens the page they are about', async () => {
    const user = userEvent.setup()
    signedInAs(CUSTOMER)
    const state = notificationsApi([
      notification(),
      notification({ id: 'notification-2', type: 'OrderCompleted', link: '/orders/order-1', params: { auto: 'true', by: 'System' }, readAt: '2026-10-05T10:00:00Z' }),
    ])
    server.use(http.get('*/api/v1/requests/:id', () => HttpResponse.json({ status: 404, code: 'request.not_found' }, { status: 404 })))
    const { router } = renderRoute('/en/notifications')

    const main = await screen.findByRole('main')
    expect(await within(main).findByText('Aram Plumbing sent an offer for 25,000 ֏ on your request.')).toBeInTheDocument()
    expect(within(main).getByText(en.notifications.types.OrderAutoCompleted)).toBeInTheDocument()
    expect(within(main).getByText('Unread: 1')).toBeInTheDocument()

    await user.click(within(main).getByRole('button', { name: /sent an offer/ }))
    await waitFor(() => expect(state.reads).toEqual(['notification-1']))
    await waitFor(() => expect(router.state.location.pathname).toBe('/en/requests/req-1'))
  })

  it('marks everything as read and filters unread ones', async () => {
    const user = userEvent.setup()
    signedInAs(CUSTOMER)
    const state = notificationsApi([notification(), notification({ id: 'notification-2', type: 'PartnerApproved', link: '/partner' })])
    renderRoute('/en/notifications')

    const main = await screen.findByRole('main')
    await user.click(await within(main).findByRole('radio', { name: en.notifications.filter.unread }))
    expect(await within(main).findByText(en.notifications.types.PartnerApproved)).toBeInTheDocument()

    await user.click(within(main).getByRole('button', { name: en.notifications.markAll }))
    await waitFor(() => expect(state.readAll).toBe(1))
    expect(await within(main).findByText(en.notifications.emptyTitle)).toBeInTheDocument()
    expect(await screen.findByRole('link', { name: en.notifications.title })).toBeInTheDocument()
  })
})
