import { screen, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { http, HttpResponse } from 'msw'
import en from '@/i18n/locales/en/common.json'
import { CUSTOMER, signedInAs } from '@/test/auth'
import { conversation, message, page } from '@/test/fixtures'
import { renderRoute } from '@/test/renderWithProviders'
import { server } from '@/test/server'

const cancelled = (overrides = {}) =>
  conversation({
    requestStatus: 'Cancelled',
    requestCancelledAt: '2026-10-05T10:30:00Z',
    requestCancelReason: 'Found someone nearer',
    canSend: true,
    canSendUntil: '2026-10-12T10:30:00Z',
    ...overrides,
  })

const messages = [
  message({ id: 'before', body: 'When can you come?', senderRole: 'Customer', sentAt: '2026-10-05T10:00:00Z' }),
  message({ id: 'after-1', body: 'Sorry to hear that', sentAt: '2026-10-05T11:00:00Z' }),
  message({ id: 'after-2', body: 'Thanks anyway', senderRole: 'Customer', sentAt: '2026-10-05T11:05:00Z' }),
]

function chatApi(detail, items = messages) {
  server.use(
    http.get('*/api/v1/conversations', () => HttpResponse.json(page([detail]))),
    http.get('*/api/v1/conversations/:id', () => HttpResponse.json(detail)),
    http.get('*/api/v1/conversations/:id/messages', () => HttpResponse.json({ items, hasMore: false })),
    http.post('*/api/v1/conversations/:id/read', () => HttpResponse.json({ readAt: '2026-10-05T11:10:00Z' })),
  )
}

const marker = () => screen.getByRole('button', { name: /Request cancelled/ })

describe('Messages after a cancellation', () => {
  it('marks where the request was cancelled and sets later messages apart', async () => {
    signedInAs(CUSTOMER)
    chatApi(cancelled())
    renderRoute('/en/messages/conv-1')

    const thread = await screen.findByRole('list', { name: en.chat.messagesLabel })
    await within(thread).findByText('Thanks anyway')
    const texts = within(thread).getAllByRole('listitem').map((item) => item.textContent)
    const at = (text) => texts.findIndex((t) => t.includes(text))
    expect(at('When can you come?')).toBeLessThan(at('Request cancelled'))
    expect(at('Request cancelled')).toBeLessThan(at('Sorry to hear that'))

    expect(within(thread).getByText('When can you come?').closest('li')).not.toHaveClass('thread__message--after-cancel')
    expect(within(thread).getByText('Sorry to hear that').closest('li')).toHaveClass('thread__message--after-cancel')
    expect(within(thread).getByText('Thanks anyway').closest('li')).toHaveClass('thread__message--after-cancel')
    // Said in words once, on the first message after it.
    expect(within(thread).getAllByText(en.chat.cancel.after)).toHaveLength(1)
    expect(within(thread).getByText(en.chat.cancel.after).closest('li')).toHaveTextContent('Sorry to hear that')
  })

  it('opens the reason from the marker', async () => {
    const user = userEvent.setup()
    signedInAs(CUSTOMER)
    chatApi(cancelled())
    renderRoute('/en/messages/conv-1')
    await screen.findByText('Thanks anyway')

    expect(marker()).toHaveAttribute('aria-expanded', 'false')
    await user.click(marker())
    expect(marker()).toHaveAttribute('aria-expanded', 'true')
    expect(screen.getByText(en.chat.cancel.reason.replace('{{reason}}', 'Found someone nearer'))).toBeInTheDocument()

    await user.click(marker())
    expect(screen.queryByText(/Found someone nearer/)).not.toBeInTheDocument()
  })

  it('says when no reason was given', async () => {
    const user = userEvent.setup()
    signedInAs(CUSTOMER)
    chatApi(cancelled({ requestCancelReason: null }))
    renderRoute('/en/messages/conv-1')
    await screen.findByText('Thanks anyway')

    await user.click(marker())
    expect(screen.getByText(en.chat.cancel.noReason)).toBeInTheDocument()
  })

  it('says until when both sides can still write', async () => {
    signedInAs(CUSTOMER)
    chatApi(cancelled())
    renderRoute('/en/messages/conv-1')

    expect(await screen.findByText(/The request was cancelled. You can still write to each other until/)).toHaveTextContent('October 12')
    expect(screen.getByRole('textbox')).toBeInTheDocument()
  })

  it('explains that the conversation closed a week after the cancellation', async () => {
    signedInAs(CUSTOMER)
    chatApi(cancelled({ canSend: false }))
    renderRoute('/en/messages/conv-1')

    expect(await screen.findByText(en.chat.cancel.closed)).toBeInTheDocument()
    expect(screen.queryByRole('textbox')).not.toBeInTheDocument()
  })

  it('keeps the general closed message for requests that ended otherwise, and no marker without a cancellation', async () => {
    signedInAs(CUSTOMER)
    chatApi(conversation({ requestStatus: 'Closed', canSend: false }), [messages[0]])
    renderRoute('/en/messages/conv-1')

    expect(await screen.findByText(en.chat.closed)).toBeInTheDocument()
    expect(screen.queryByRole('button', { name: /Request cancelled/ })).not.toBeInTheDocument()
  })
})
