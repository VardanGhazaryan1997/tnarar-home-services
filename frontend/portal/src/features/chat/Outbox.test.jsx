import { act, screen, waitFor, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { delay, http, HttpResponse } from 'msw'
import en from '@/i18n/locales/en/common.json'
import { CUSTOMER, signedInAs } from '@/test/auth'
import { conversation, message, page } from '@/test/fixtures'
import { renderRoute } from '@/test/renderWithProviders'
import { server } from '@/test/server'
import { hub } from '@/test/signalr'
import { outbox } from './outbox'

/**
 * The chat API, with a server that remembers sent messages by client id like the real one. `failures`: how many
 * sends fail with a network error; `saveFailed`: whether a failed send still reached the server (a lost answer).
 */
function chatApi({ failures = 0, saveFailed = false, slow = false } = {}) {
  const saved = new Map()
  const posts = []
  let failuresLeft = failures
  server.use(
    http.get('*/api/v1/conversations', () => HttpResponse.json(page([conversation()]))),
    http.get('*/api/v1/conversations/:id', () => HttpResponse.json(conversation())),
    http.get('*/api/v1/conversations/:id/messages', () => HttpResponse.json({ items: [message(), ...saved.values()], hasMore: false })),
    http.post('*/api/v1/conversations/:id/read', () => HttpResponse.json({ readAt: '2026-10-05T11:00:00Z' })),
    http.post('*/api/v1/conversations/:id/messages', async ({ request }) => {
      const body = await request.json()
      posts.push(body)
      if (slow) await delay(300)
      const sent = saved.get(body.clientId) ?? message({ id: `srv-${saved.size + 1}`, senderRole: 'Customer', body: body.body, clientId: body.clientId, sentAt: '2026-10-05T11:00:00Z' })
      if (failuresLeft > 0) {
        failuresLeft -= 1
        if (saveFailed) saved.set(body.clientId, sent)
        return HttpResponse.error()
      }
      saved.set(body.clientId, sent)
      return HttpResponse.json(sent, { status: 201 })
    }),
  )
  return posts
}

async function openAndSend(user, text) {
  signedInAs(CUSTOMER)
  const view = renderRoute('/en/messages/conv-1')
  await user.type(await screen.findByLabelText(en.chat.placeholder), `${text}{Enter}`)
  return view
}

const pending = () => screen.queryByRole('list', { name: en.chat.outbox.label })

describe('Messages that fail to send', () => {
  it('keeps a failed message with its text, says why, and retries it with the same id', async () => {
    const user = userEvent.setup()
    const posts = chatApi({ failures: 1 })
    await openAndSend(user, 'Can you come at 10?')

    const failed = await screen.findByRole('alert')
    expect(failed).toHaveTextContent(`${en.chat.outbox.failed} ${en.errors.network}`)
    expect(within(pending()).getByText('Can you come at 10?')).toBeInTheDocument()
    expect(screen.getByLabelText(en.chat.placeholder)).toHaveValue('')

    await user.click(screen.getByRole('button', { name: en.chat.outbox.retry }))

    const thread = screen.getByRole('list', { name: en.chat.messagesLabel })
    expect(await within(thread).findByText('Can you come at 10?')).toBeInTheDocument()
    await waitFor(() => expect(pending()).not.toBeInTheDocument())
    expect(posts).toHaveLength(2)
    expect(posts[1].clientId).toBe(posts[0].clientId)
  })

  it('shows the message once when the first try reached the server but its answer was lost', async () => {
    const user = userEvent.setup()
    const posts = chatApi({ failures: 1, saveFailed: true })
    await openAndSend(user, 'Only once, please')

    await user.click(await screen.findByRole('button', { name: en.chat.outbox.retry }))

    await waitFor(() => expect(pending()).not.toBeInTheDocument())
    expect(screen.getAllByText('Only once, please')).toHaveLength(1)
    expect(posts.map((post) => post.clientId)).toEqual([posts[0].clientId, posts[0].clientId])
  })

  it('lets the user discard a failed message', async () => {
    const user = userEvent.setup()
    chatApi({ failures: 1 })
    await openAndSend(user, 'Never mind')

    await user.click(await screen.findByRole('button', { name: en.chat.outbox.discard }))

    expect(pending()).not.toBeInTheDocument()
    expect(screen.queryByText('Never mind')).not.toBeInTheDocument()
    expect(localStorage.getItem('tnarar:outbox:conv-1')).toBeNull()
  })

  it('keeps a failed message across a reload', async () => {
    const user = userEvent.setup()
    chatApi({ failures: 1 })
    const { unmount } = await openAndSend(user, 'Still here after a reload')
    await screen.findByRole('alert')
    unmount()
    outbox.reset()

    renderRoute('/en/messages/conv-1')

    expect(within(await screen.findByRole('list', { name: en.chat.outbox.label })).getByText('Still here after a reload')).toBeInTheDocument()
    expect(screen.getByRole('button', { name: en.chat.outbox.retry })).toBeInTheDocument()
  })

  it('shows a message as sending until the server has it, and once when its live copy comes first', async () => {
    const user = userEvent.setup()
    const posts = chatApi({ slow: true })
    await openAndSend(user, 'On my way')

    expect(await within(pending()).findByText(en.chat.outbox.sending)).toBeInTheDocument()
    await waitFor(() => expect(hub.connections).toHaveLength(1))
    act(() =>
      hub.emit('messageReceived', message({ id: 'srv-1', senderRole: 'Customer', body: 'On my way', clientId: posts[0].clientId, sentAt: '2026-10-05T11:00:00Z' })),
    )

    // The live copy replaces the one sending at once; the answer then adds nothing new.
    await waitFor(() => expect(screen.getAllByText('On my way')).toHaveLength(1))
    await waitFor(() => expect(pending()).not.toBeInTheDocument())
    expect(screen.getAllByText('On my way')).toHaveLength(1)
  })
})
