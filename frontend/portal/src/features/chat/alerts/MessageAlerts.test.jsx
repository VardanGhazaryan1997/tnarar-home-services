import { act, screen, waitFor, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { http, HttpResponse } from 'msw'
import en from '@/i18n/locales/en/common.json'
import { CUSTOMER, PARTNER_USER, signedInAs } from '@/test/auth'
import { conversation, message, page } from '@/test/fixtures'
import { renderRoute } from '@/test/renderWithProviders'
import { server } from '@/test/server'
import { hub } from '@/test/signalr'
import { ALERT_MS } from './MessageAlerts'
import { playMessageSound } from './sound'

vi.mock('./sound', async (importOriginal) => ({ ...(await importOriginal()), playMessageSound: vi.fn(() => Promise.resolve(true)) }))

const title = (name) => en.chat.alerts.title.replace('{{name}}', name)

function chatApi(detail = conversation()) {
  server.use(
    http.get('*/api/v1/conversations', () => HttpResponse.json(page([detail]))),
    http.get('*/api/v1/conversations/:id', () => HttpResponse.json(detail)),
    http.get('*/api/v1/conversations/:id/messages', () => HttpResponse.json({ items: [message()], hasMore: false })),
    http.post('*/api/v1/conversations/:id/read', () => HttpResponse.json({ readAt: '2026-10-05T11:00:00Z' })),
  )
}

/** The tab's state: visible or hidden, focused or not. */
function tab({ visible = true, focused = true } = {}) {
  vi.spyOn(document, 'visibilityState', 'get').mockReturnValue(visible ? 'visible' : 'hidden')
  vi.spyOn(document, 'hasFocus').mockReturnValue(focused)
}

async function connected() {
  await waitFor(() => expect(hub.connections).toHaveLength(1))
}

const alerts = () => screen.getByRole('region', { name: en.chat.alerts.region })

describe('New-message alerts', () => {
  beforeEach(() => playMessageSound.mockClear())

  it('tells a customer about a specialist’s message with a notice, a link to it and the sound', async () => {
    tab()
    signedInAs(CUSTOMER)
    chatApi()
    renderRoute('/en')
    await connected()

    act(() => hub.emit('messageReceived', message({ id: 'live-1', body: 'I can come at 5' })))

    const notice = await within(alerts()).findByRole('status')
    expect(notice).toHaveTextContent(title('Aram Plumbing'))
    expect(notice).toHaveTextContent('I can come at 5')
    expect(within(notice).getByRole('link', { name: en.chat.alerts.open })).toHaveAttribute('href', '/en/messages/conv-1')
    expect(playMessageSound).toHaveBeenCalledExactlyOnceWith('live-1')
  })

  it('tells a specialist about a customer’s message too, naming the customer by role when the name is hidden', async () => {
    tab()
    signedInAs(PARTNER_USER)
    chatApi(conversation({ myRole: 'Partner', otherParty: { name: null, partnerId: null, slug: null } }))
    renderRoute('/en')
    await connected()

    act(() => hub.emit('messageReceived', message({ id: 'live-2', senderRole: 'Customer', body: null, attachments: [{ id: 'f1' }, { id: 'f2' }] })))

    const notice = await within(alerts()).findByRole('status')
    expect(notice).toHaveTextContent(title(en.chat.role.Customer))
    expect(notice).toHaveTextContent('2 file(s)')
  })

  it('stays quiet about the user’s own messages, sent from another device', async () => {
    tab()
    signedInAs(CUSTOMER)
    chatApi()
    renderRoute('/en')
    await connected()

    act(() => hub.emit('messageReceived', message({ id: 'mine', senderRole: 'Customer' })))

    await waitFor(() => expect(playMessageSound).not.toHaveBeenCalled())
    expect(within(alerts()).queryByRole('status')).not.toBeInTheDocument()
  })

  it('stays quiet while the user reads that conversation in a visible, focused tab', async () => {
    tab()
    signedInAs(CUSTOMER)
    chatApi()
    renderRoute('/en/messages/conv-1')
    await screen.findByRole('list', { name: en.chat.messagesLabel })
    await connected()

    act(() => hub.emit('messageReceived', message({ id: 'read-live', body: 'Here now' })))

    expect(await screen.findByText('Here now')).toBeInTheDocument()
    expect(within(alerts()).queryByRole('status')).not.toBeInTheDocument()
    expect(playMessageSound).not.toHaveBeenCalled()
  })

  it.each([
    ['in a background tab', { visible: false, focused: false }],
    ['in an unfocused window', { visible: true, focused: false }],
  ])('still alerts about the open conversation %s', async (_case, state) => {
    tab(state)
    signedInAs(CUSTOMER)
    chatApi()
    renderRoute('/en/messages/conv-1')
    await screen.findByRole('list', { name: en.chat.messagesLabel })
    await connected()

    act(() => hub.emit('messageReceived', message({ id: 'bg-live', body: 'Are you there?' })))

    expect(await within(alerts()).findByRole('status')).toHaveTextContent('Are you there?')
    expect(playMessageSound).toHaveBeenCalledOnce()
  })

  it('stays quiet when another tab is showing that conversation', async () => {
    tab({ visible: false, focused: false })
    localStorage.setItem('tnarar:reading-conversation', JSON.stringify({ id: 'conv-1', tab: 'other', at: Date.now() }))
    signedInAs(CUSTOMER)
    chatApi()
    renderRoute('/en')
    await connected()

    act(() => hub.emit('messageReceived', message({ id: 'other-tab' })))
    act(() => hub.emit('messageReceived', message({ id: 'other-conv', conversationId: 'conv-2' })))

    await waitFor(() => expect(playMessageSound).toHaveBeenCalledExactlyOnceWith('other-conv'))
  })

  it('counts new messages in the tab title while the tab is hidden', async () => {
    tab({ visible: false, focused: false })
    signedInAs(CUSTOMER)
    chatApi()
    renderRoute('/en')
    await connected()
    const original = document.title

    act(() => hub.emit('messageReceived', message({ id: 'h-1' })))
    act(() => hub.emit('messageReceived', message({ id: 'h-2' })))

    await waitFor(() => expect(document.title).toBe(`${en.chat.alerts.tabTitle.replace('{{n}}', '2')} · ${original}`))
    tab({ visible: true })
    act(() => document.dispatchEvent(new Event('visibilitychange')))
    await waitFor(() => expect(document.title).toBe(original))
  })

  it('closes notices by hand and keeps the newest three', async () => {
    const user = userEvent.setup()
    tab()
    signedInAs(CUSTOMER)
    chatApi()
    renderRoute('/en')
    await connected()

    for (const id of ['a', 'b', 'c', 'd']) act(() => hub.emit('messageReceived', message({ id, body: `Message ${id}` })))
    await waitFor(() => expect(within(alerts()).getAllByRole('status')).toHaveLength(3))
    expect(alerts()).not.toHaveTextContent('Message a')

    await user.click(within(alerts()).getAllByRole('button', { name: en.chat.alerts.dismiss })[0])
    expect(alerts()).not.toHaveTextContent('Message b')
    expect(alerts()).toHaveTextContent('Message c')
  })

  it('closes notices on its own after a while, but not while the pointer is on one', async () => {
    tab()
    signedInAs(CUSTOMER)
    chatApi()
    renderRoute('/en')
    await connected()
    // Fake only the timers the notices use; real time still passes for the network and waitFor.
    vi.useFakeTimers({ shouldAdvanceTime: true, toFake: ['setTimeout', 'clearTimeout'] })
    try {
      act(() => hub.emit('messageReceived', message({ id: 'x', body: 'First' })))
      act(() => hub.emit('messageReceived', message({ id: 'y', body: 'Second' })))
      const [hovered] = await waitFor(() => {
        const notices = within(alerts()).getAllByRole('status')
        expect(notices).toHaveLength(2)
        return notices
      })

      act(() => hovered.dispatchEvent(new MouseEvent('mouseover', { bubbles: true })))
      act(() => vi.advanceTimersByTime(ALERT_MS + 100))
      expect(within(alerts()).getAllByRole('status')).toEqual([hovered])

      act(() => hovered.dispatchEvent(new MouseEvent('mouseout', { bubbles: true })))
      act(() => vi.advanceTimersByTime(ALERT_MS + 100))
      expect(within(alerts()).queryByRole('status')).not.toBeInTheDocument()
    } finally {
      vi.useRealTimers()
    }
  })

  it('lets the user mute message sounds, remembered on this device', async () => {
    const user = userEvent.setup()
    signedInAs(CUSTOMER)
    chatApi()
    renderRoute('/en')

    const toggle = await screen.findByRole('button', { name: en.chat.sound.on })
    expect(toggle).toHaveAttribute('aria-pressed', 'true')

    await user.click(toggle)

    expect(screen.getByRole('button', { name: en.chat.sound.off })).toHaveAttribute('aria-pressed', 'false')
    expect(localStorage.getItem('tnarar:message-sound')).toBe('off')

    await user.click(screen.getByRole('button', { name: en.chat.sound.off }))
    expect(localStorage.getItem('tnarar:message-sound')).toBeNull()
  })
})
