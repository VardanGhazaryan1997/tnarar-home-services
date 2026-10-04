import { act, screen, waitFor, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { http, HttpResponse } from 'msw'
import en from '@/i18n/locales/en/common.json'
import { CUSTOMER, PARTNER_USER, problem, signedInAs } from '@/test/auth'
import { conversation, message, myRequest, page } from '@/test/fixtures'
import { renderRoute } from '@/test/renderWithProviders'
import { server } from '@/test/server'
import { hub } from '@/test/signalr'
import { hubUrl } from './useChatConnection'

function chatApi({ conversations = [conversation()], detail = conversation(), messages = [message()], hasMore = false } = {}) {
  const calls = { sent: [], read: [], older: [], opened: [], lists: 0 }
  server.use(
    http.get('*/api/v1/conversations', () => {
      calls.lists += 1
      return HttpResponse.json(page(conversations))
    }),
    http.get('*/api/v1/conversations/:id', ({ params }) => (params.id === detail.id ? HttpResponse.json(detail) : problem(404, 'conversation.not_found'))),
    http.get('*/api/v1/conversations/:id/messages', ({ request }) => {
      const before = new URL(request.url).searchParams.get('before')
      if (before) {
        calls.older.push(before)
        return HttpResponse.json({ items: [message({ id: 'msg-0', body: 'Earlier message', sentAt: '2026-10-04T09:00:00Z' })], hasMore: false })
      }
      return HttpResponse.json({ items: messages, hasMore })
    }),
    http.post('*/api/v1/conversations/:id/messages', async ({ request }) => {
      const body = await request.json()
      calls.sent.push(body)
      return HttpResponse.json(message({ id: `sent-${calls.sent.length}`, senderRole: detail.myRole, body: body.body, sentAt: '2026-10-05T11:00:00Z' }), { status: 201 })
    }),
    http.post('*/api/v1/conversations/:id/read', ({ params }) => {
      calls.read.push(params.id)
      return HttpResponse.json({ readAt: '2026-10-05T11:00:00Z' })
    }),
    http.post('*/api/v1/requests/:id/conversation', async ({ request, params }) => {
      calls.opened.push({ requestId: params.id, ...(await request.json()) })
      return HttpResponse.json(detail)
    }),
  )
  return calls
}

describe('Messages', () => {
  it('lists conversations with the last message and unread counts', async () => {
    signedInAs(CUSTOMER)
    chatApi({
      conversations: [
        conversation({ unreadCount: 2 }),
        conversation({ id: 'conv-2', otherParty: { name: null }, lastMessage: { senderRole: 'Customer', excerpt: null, attachmentCount: 2, sentAt: '2026-10-04T10:00:00Z' } }),
        conversation({ id: 'conv-3', lastMessage: null }),
      ],
    })
    renderRoute('/en/messages')

    const list = await within(await screen.findByRole('main')).findByRole('list')
    const items = within(list).getAllByRole('link')
    expect(items[0]).toHaveAttribute('href', '/en/messages/conv-1')
    expect(items[0]).toHaveTextContent('Aram Plumbing')
    expect(items[0]).toHaveTextContent('When can I come?')
    expect(within(items[0]).getByLabelText('2 unread')).toHaveTextContent('2')
    expect(items[1]).toHaveTextContent('Specialist')
    expect(items[1]).toHaveTextContent('You: 2 file(s)')
    expect(items[2]).toHaveTextContent(en.chat.noMessages)
    expect(screen.getByText(en.chat.pickTitle)).toBeInTheDocument()
  })

  it('shows an empty list', async () => {
    signedInAs(CUSTOMER)
    chatApi({ conversations: [] })
    renderRoute('/en/messages')
    expect(await screen.findByText(en.chat.emptyTitle)).toBeInTheDocument()
  })

  it('opens a conversation, marks it read and sends messages with Enter', async () => {
    const user = userEvent.setup()
    signedInAs(CUSTOMER)
    const calls = chatApi({ detail: conversation({ unreadCount: 1, orderId: 'order-1' }) })
    renderRoute('/en/messages/conv-1')

    const thread = await screen.findByRole('list', { name: en.chat.messagesLabel })
    expect(within(thread).getByText('When can I come?')).toBeInTheDocument()
    expect(screen.getByRole('link', { name: 'Plumbing · Yerevan, Kentron' })).toHaveAttribute('href', '/en/requests/req-1')
    expect(screen.getByRole('link', { name: en.chat.openOrder })).toHaveAttribute('href', '/en/orders/order-1')
    await waitFor(() => expect(calls.read).toEqual(['conv-1']))

    await user.type(screen.getByLabelText(en.chat.placeholder), 'Tomorrow at 10{Enter}')
    expect(await within(thread).findByText('Tomorrow at 10')).toBeInTheDocument()
    expect(calls.sent).toEqual([{ body: 'Tomorrow at 10', fileIds: [] }])
    expect(screen.getByLabelText(en.chat.placeholder)).toHaveValue('')

    await user.type(screen.getByLabelText(en.chat.placeholder), 'line one{Shift>}{Enter}{/Shift}line two')
    expect(screen.getByLabelText(en.chat.placeholder)).toHaveValue('line one\nline two')
    await user.click(screen.getByRole('button', { name: en.chat.send }))
    await waitFor(() => expect(calls.sent).toHaveLength(2))
  })

  it('shows read receipts on my last message and loads earlier messages', async () => {
    const user = userEvent.setup()
    signedInAs(PARTNER_USER)
    const calls = chatApi({
      detail: conversation({ myRole: 'Partner', otherParty: { name: 'Ani' }, otherReadAt: '2026-10-05T10:30:00Z' }),
      messages: [message({ senderRole: 'Partner', body: 'Hello' }), message({ id: 'msg-2', senderRole: 'Customer', body: 'Hi', sentAt: '2026-10-05T10:05:00Z' }), message({ id: 'msg-3', senderRole: 'Partner', body: 'When can I come?', sentAt: '2026-10-05T10:10:00Z' })],
      hasMore: true,
    })
    renderRoute('/en/messages/conv-1')

    const thread = await screen.findByRole('list', { name: en.chat.messagesLabel })
    expect(within(thread).getAllByText(en.chat.seen)).toHaveLength(1)
    expect(within(thread).getByText('When can I come?').closest('li')).toHaveClass('thread__message--mine')
    expect(within(thread).getByText('Hi').closest('li')).not.toHaveClass('thread__message--mine')
    expect(screen.getByRole('link', { name: 'Plumbing · Yerevan, Kentron' })).toHaveAttribute('href', '/en/inbox/req-1')

    await user.click(screen.getByRole('button', { name: en.chat.older }))
    expect(await within(thread).findByText('Earlier message')).toBeInTheDocument()
    expect(calls.older).toEqual(['2026-10-05T10:00:00Z'])
    expect(screen.queryByRole('button', { name: en.chat.older })).not.toBeInTheDocument()
  })

  it('adds messages that arrive live and refreshes the list', async () => {
    signedInAs(CUSTOMER)
    const calls = chatApi()
    renderRoute('/en/messages/conv-1')

    const thread = await screen.findByRole('list', { name: en.chat.messagesLabel })
    await waitFor(() => expect(hub.connections).toHaveLength(1))
    expect(hub.connections[0].url).toBe(hubUrl())
    expect(hub.connections[0].options.accessTokenFactory()).toBe(`access-${CUSTOMER.id}`)
    const listsBefore = calls.lists

    act(() => hub.emit('messageReceived', message({ id: 'live-1', body: 'I am outside' })))
    act(() => hub.emit('messageReceived', message({ id: 'live-1', body: 'I am outside' })))
    expect(await within(thread).findByText('I am outside')).toBeInTheDocument()
    expect(within(thread).getAllByText('I am outside')).toHaveLength(1)
    await waitFor(() => expect(calls.lists).toBeGreaterThan(listsBefore))

    act(() => hub.emit('conversationRead', { conversationId: 'conv-1', readerRole: 'Partner', readAt: '2026-10-05T12:00:00Z' }))
    act(() => hub.reconnect())
  })

  it('sends files and shows files it refuses', async () => {
    const user = userEvent.setup({ applyAccept: false })
    signedInAs(CUSTOMER)
    const calls = chatApi()
    server.use(
      http.post('*/api/v1/files/uploads', () => HttpResponse.json({ fileId: 'f1', uploadUrl: 'https://storage.test/f1', method: 'PUT', headers: {} })),
      http.put('https://storage.test/f1', () => new HttpResponse(null, { status: 200 })),
      http.post('*/api/v1/files/f1/complete', () => HttpResponse.json({ id: 'f1' })),
    )
    renderRoute('/en/messages/conv-1')

    await user.upload(await screen.findByLabelText(en.chat.attach, { selector: 'input' }), new File(['x'], 'plan.pdf', { type: 'application/pdf' }))
    expect(await screen.findByText('plan.pdf')).toBeInTheDocument()
    await waitFor(() => expect(screen.getByRole('button', { name: en.chat.send })).toBeEnabled())
    await user.click(screen.getByRole('button', { name: en.chat.send }))
    await waitFor(() => expect(calls.sent).toEqual([{ body: null, fileIds: ['f1'] }]))
    await waitFor(() => expect(screen.queryByText('plan.pdf')).not.toBeInTheDocument())

    await user.upload(screen.getByLabelText(en.chat.attach, { selector: 'input' }), new File(['x'], 'notes.txt', { type: 'text/plain' }))
    await user.click(screen.getByRole('button', { name: 'Remove notes.txt' }))
  })

  it('shows send failures, closed conversations and missing ones', async () => {
    const user = userEvent.setup()
    signedInAs(CUSTOMER)
    chatApi()
    server.use(http.post('*/api/v1/conversations/:id/messages', () => problem(422, 'conversation.closed')))
    const { unmount } = renderRoute('/en/messages/conv-1')

    await user.type(await screen.findByLabelText(en.chat.placeholder), 'Hello{Enter}')
    expect(await screen.findByText(en.errors.conversation.closed)).toBeInTheDocument()
    unmount()

    chatApi({ detail: conversation({ canSend: false }), messages: [] })
    renderRoute('/en/messages/conv-1')
    expect(await screen.findByText(en.chat.closed)).toBeInTheDocument()
    expect(screen.getByText(en.chat.startHint)).toBeInTheDocument()
    expect(screen.queryByLabelText(en.chat.placeholder)).not.toBeInTheDocument()
  })

  it('says when a conversation does not exist', async () => {
    signedInAs(CUSTOMER)
    chatApi()
    renderRoute('/en/messages/nope')
    expect(await screen.findByText(en.chat.notFound)).toBeInTheDocument()
  })

  it('starts a conversation from a request and shows the unread badge', async () => {
    const user = userEvent.setup()
    signedInAs(CUSTOMER)
    const calls = chatApi()
    server.use(
      http.get('*/api/v1/requests/req-1', () => HttpResponse.json(myRequest())),
      http.get('*/api/v1/requests/req-1/offers', () => HttpResponse.json([])),
      http.get('*/api/v1/conversations/unread', () => HttpResponse.json({ conversations: 1, messages: 120 })),
    )
    const { router } = renderRoute('/en/requests/req-1')

    expect((await screen.findAllByLabelText('120 unread'))[0]).toHaveTextContent('99+')
    await user.click(await screen.findByRole('button', { name: en.chat.message }))
    await waitFor(() => expect(router.state.location.pathname).toBe('/en/messages/conv-1'))
    expect(calls.opened).toEqual([{ requestId: 'req-1', partnerId: 'partner-1' }])
  })

  it('keeps working when the live connection cannot start', async () => {
    hub.failStart = true
    signedInAs(CUSTOMER)
    chatApi()
    renderRoute('/en/messages')
    expect(await screen.findByText('Aram Plumbing')).toBeInTheDocument()
    expect(hub.connections).toHaveLength(0)
  })

  it('reports a failure to open a conversation on the button', async () => {
    const user = userEvent.setup()
    signedInAs(PARTNER_USER)
    server.use(
      http.get('*/api/v1/requests/inbox/:id', () => HttpResponse.json({ ...myRequest(), requestStatus: 'Open', myStatus: 'Viewed', customerFirstName: 'Ani', sentAt: '2026-10-05T09:00:00Z' })),
      http.get('*/api/v1/requests/:id/offers', () => HttpResponse.json([])),
      http.post('*/api/v1/requests/:id/conversation', () => problem(422, 'conversation.unavailable')),
    )
    renderRoute('/en/inbox/req-1')

    await user.click(await screen.findByRole('button', { name: en.chat.askCustomer }))
    expect(await screen.findByRole('button', { name: en.chat.openFailed })).toBeInTheDocument()
  })
})
