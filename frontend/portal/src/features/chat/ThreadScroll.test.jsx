import { act, fireEvent, screen, waitFor, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { http, HttpResponse } from 'msw'
import en from '@/i18n/locales/en/common.json'
import { CUSTOMER, signedInAs } from '@/test/auth'
import { conversation, message, page } from '@/test/fixtures'
import { renderRoute } from '@/test/renderWithProviders'
import { server } from '@/test/server'
import { hub } from '@/test/signalr'

const history = [
  message({ id: 'm1', body: 'Hello', senderRole: 'Customer', sentAt: '2026-10-05T09:00:00Z' }),
  message({ id: 'm2', body: 'Hi, how can I help?', sentAt: '2026-10-05T09:05:00Z' }),
  message({ id: 'm3', body: 'I can come tomorrow', sentAt: '2026-10-05T09:10:00Z' }),
  message({ id: 'm4', body: 'Or on Friday', sentAt: '2026-10-05T09:15:00Z' }),
]

function chatApi({ unreadCount = 0 } = {}) {
  let detail = conversation({ unreadCount })
  server.use(
    http.get('*/api/v1/conversations', () => HttpResponse.json(page([detail]))),
    http.get('*/api/v1/conversations/:id', () => HttpResponse.json(detail)),
    http.get('*/api/v1/conversations/:id/messages', () => HttpResponse.json({ items: history, hasMore: false })),
    http.post('*/api/v1/conversations/:id/read', () => {
      detail = { ...detail, unreadCount: 0 }
      return HttpResponse.json({ readAt: '2026-10-05T10:00:00Z' })
    }),
    http.post('*/api/v1/conversations/:id/messages', async ({ request }) =>
      HttpResponse.json(message({ id: 'mine-1', senderRole: 'Customer', body: (await request.json()).body, sentAt: '2026-10-05T10:05:00Z' }), { status: 201 }),
    ),
  )
}

/** Pretends the user scrolled the thread up, away from the latest message. */
function scrollUp(box) {
  Object.defineProperty(box, 'scrollHeight', { value: 2000, configurable: true })
  Object.defineProperty(box, 'clientHeight', { value: 500, configurable: true })
  Object.defineProperty(box, 'scrollTop', { value: 300, configurable: true, writable: true })
  fireEvent.scroll(box)
}

const live = (id, body) => act(() => hub.emit('messageReceived', message({ id, body, sentAt: '2026-10-05T10:00:00Z' })))

describe('Thread scrolling', () => {
  let scrolled
  beforeEach(() => {
    scrolled = vi.fn()
    Element.prototype.scrollIntoView = scrolled
  })
  afterEach(() => {
    delete Element.prototype.scrollIntoView
  })

  async function openThread(options) {
    signedInAs(CUSTOMER)
    chatApi(options)
    renderRoute('/en/messages/conv-1')
    const thread = await screen.findByRole('list', { name: en.chat.messagesLabel })
    await within(thread).findByText('Or on Friday')
    await waitFor(() => expect(hub.connections).toHaveLength(1))
    return thread
  }

  it('opens at the first unread message, under a "New messages" divider', async () => {
    const thread = await openThread({ unreadCount: 2 })

    const items = within(thread).getAllByRole('listitem').map((item) => item.textContent)
    const divider = items.indexOf(en.chat.newMessages)
    expect(divider).toBeGreaterThan(-1)
    expect(items[divider + 1]).toContain('I can come tomorrow')
    await waitFor(() => expect(scrolled.mock.contexts.at(-1)).toHaveAttribute('id', 'message-m3'))
    expect(scrolled).toHaveBeenLastCalledWith({ block: 'center' })
  })

  it('opens at the latest message when nothing is unread, and follows new ones while there', async () => {
    const thread = await openThread()
    expect(within(thread).queryByText(en.chat.newMessages)).not.toBeInTheDocument()
    await waitFor(() => expect(scrolled).toHaveBeenCalledWith({ block: 'end' }))
    scrolled.mockClear()

    live('n1', 'Are you home?')

    expect(await within(thread).findByText('Are you home?')).toBeInTheDocument()
    await waitFor(() => expect(scrolled).toHaveBeenCalledWith({ block: 'end' }))
    expect(screen.queryByRole('button', { name: new RegExp(en.chat.latest) })).not.toBeInTheDocument()
  })

  it('leaves a user reading older messages where they are, and offers to jump to the new ones', async () => {
    const user = userEvent.setup()
    const thread = await openThread()
    const box = thread.parentElement
    scrollUp(box)
    scrolled.mockClear()

    live('n1', 'Are you home?')
    live('n2', 'I am outside')

    expect(await within(thread).findByText('I am outside')).toBeInTheDocument()
    expect(scrolled).not.toHaveBeenCalled()
    const latest = await screen.findByRole('button', { name: en.chat.latestCount.replace('{{n}}', '2') })
    const items = within(thread).getAllByRole('listitem').map((item) => item.textContent)
    expect(items[items.indexOf(en.chat.newMessages) + 1]).toContain('Are you home?')

    await user.click(latest)

    expect(scrolled).toHaveBeenCalledWith({ block: 'end' })
    expect(screen.queryByRole('button', { name: new RegExp(en.chat.latest) })).not.toBeInTheDocument()
  })

  it('shows "Latest" without a count when the user scrolled up but nothing new came, and clears once back down', async () => {
    const thread = await openThread()
    const box = thread.parentElement
    scrollUp(box)
    expect(await screen.findByRole('button', { name: en.chat.latest })).toBeInTheDocument()

    Object.defineProperty(box, 'scrollTop', { value: 1500, configurable: true })
    fireEvent.scroll(box)
    await waitFor(() => expect(screen.queryByRole('button', { name: en.chat.latest })).not.toBeInTheDocument())
  })

  it('scrolls to the end after the user sends a message, even from further up', async () => {
    const user = userEvent.setup()
    const thread = await openThread()
    scrollUp(thread.parentElement)
    scrolled.mockClear()

    await user.type(screen.getByRole('textbox'), 'See you then{Enter}')

    expect(await within(thread).findByText('See you then')).toBeInTheDocument()
    await waitFor(() => expect(scrolled).toHaveBeenCalledWith({ block: 'end' }))
  })
})
