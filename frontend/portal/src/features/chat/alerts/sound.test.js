import { act, renderHook } from '@testing-library/react'
import { isReading, watchOpenConversation } from './focus'
import { isMuted, playMessageSound, resetSound, setMuted, unlockSoundOnFirstGesture, useMuted } from './sound'

/** A stand-in for Web Audio that records the notes it was asked to play. */
class FakeAudioContext {
  static notes = []
  state = 'running'
  currentTime = 0
  destination = {}
  resume = vi.fn()
  createGain() {
    return { gain: { setValueAtTime() {}, exponentialRampToValueAtTime() {} }, connect: (to) => to }
  }
  createOscillator() {
    const oscillator = { frequency: {}, connect: (gain) => gain, start: () => FakeAudioContext.notes.push(oscillator.frequency.value), stop() {} }
    return oscillator
  }
}

describe('message sound', () => {
  beforeEach(() => {
    FakeAudioContext.notes = []
    resetSound(new FakeAudioContext())
    Object.defineProperty(navigator, 'locks', { value: undefined, configurable: true })
  })

  afterEach(() => resetSound(null))

  it('plays a two-note chime once per message, across tabs sharing this browser', async () => {
    expect(await playMessageSound('m-1')).toBe(true)
    expect(FakeAudioContext.notes).toEqual([880, 1318.5])

    // Another tab got the same message: already played.
    expect(await playMessageSound('m-1')).toBe(false)
    expect(await playMessageSound('m-2')).toBe(true)
    expect(FakeAudioContext.notes).toHaveLength(4)
  })

  it('takes turns through a Web Lock so two tabs never chime together', async () => {
    let held = false
    const request = vi.fn(async (_name, _options, callback) => {
      if (held) return callback(null)
      held = true
      try {
        return await callback({ name: 'lock' })
      } finally {
        held = false
      }
    })
    Object.defineProperty(navigator, 'locks', { value: { request }, configurable: true })
    vi.useFakeTimers()
    try {
      const first = playMessageSound('m-3')
      const second = await playMessageSound('m-4')
      await vi.advanceTimersByTimeAsync(1500)

      expect(await first).toBe(true)
      expect(second).toBe(false)
      expect(request).toHaveBeenCalledWith('tnarar-message-sound', { ifAvailable: true }, expect.any(Function))
    } finally {
      vi.useRealTimers()
    }
  })

  it('stays silent when muted, and before the user has interacted with the page', async () => {
    setMuted(true)
    expect(isMuted()).toBe(true)
    expect(await playMessageSound('m-5')).toBe(false)

    setMuted(false)
    resetSound(null)
    expect(await playMessageSound('m-6')).toBe(false)
    expect(FakeAudioContext.notes).toEqual([])
  })

  it('prepares the audio on the first click or key press', async () => {
    resetSound(null)
    window.AudioContext = FakeAudioContext
    try {
      const stop = unlockSoundOnFirstGesture()
      window.dispatchEvent(new KeyboardEvent('keydown'))
      expect(await playMessageSound('m-7')).toBe(true)
      stop()
    } finally {
      delete window.AudioContext
    }
    expect(unlockSoundOnFirstGesture()).toBeTypeOf('function')
  })

  it('keeps the mute switch in step with other tabs', () => {
    const { result } = renderHook(() => useMuted())
    expect(result.current).toBe(false)

    act(() => {
      localStorage.setItem('tnarar:message-sound', 'off')
      window.dispatchEvent(new StorageEvent('storage', { key: 'tnarar:message-sound' }))
    })
    expect(result.current).toBe(true)
  })

  it('copes with a damaged record of played messages', async () => {
    localStorage.setItem('tnarar:message-sound-played', '{not json')
    expect(await playMessageSound('m-8')).toBe(true)
  })
})

describe('reading a conversation', () => {
  const tab = (visible, focused) => {
    vi.spyOn(document, 'visibilityState', 'get').mockReturnValue(visible ? 'visible' : 'hidden')
    vi.spyOn(document, 'hasFocus').mockReturnValue(focused)
  }

  it('counts only while the tab is visible and focused, and tells other tabs', () => {
    tab(true, true)
    const stop = watchOpenConversation('conv-1')
    expect(isReading('conv-1')).toBe(true)
    expect(isReading('conv-2')).toBe(false)
    expect(JSON.parse(localStorage.getItem('tnarar:reading-conversation'))).toMatchObject({ id: 'conv-1' })

    tab(false, false)
    act(() => document.dispatchEvent(new Event('visibilitychange')))
    expect(isReading('conv-1')).toBe(false)
    expect(localStorage.getItem('tnarar:reading-conversation')).toBeNull()

    stop()
  })

  it('ignores a note another tab left long ago', () => {
    tab(false, false)
    localStorage.setItem('tnarar:reading-conversation', JSON.stringify({ id: 'conv-1', tab: 'gone', at: Date.now() - 10 * 60_000 }))
    expect(isReading('conv-1')).toBe(false)
    localStorage.setItem('tnarar:reading-conversation', '{broken')
    expect(isReading('conv-1')).toBe(false)
  })

  it('leaves another tab’s note alone when this tab stops reading', () => {
    tab(false, false)
    const stop = watchOpenConversation('conv-1')
    localStorage.setItem('tnarar:reading-conversation', JSON.stringify({ id: 'conv-1', tab: 'other', at: Date.now() }))
    stop()
    expect(isReading('conv-1')).toBe(true)
  })
})
