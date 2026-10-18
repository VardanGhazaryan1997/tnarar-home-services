import { useSyncExternalStore } from 'react'

/**
 * The new-message sound (T2): a short, soft two-note chime made with Web Audio, so there is no file to load.
 * Browsers only let a page play sound after the user has clicked or typed on it, so the audio is prepared on the
 * first such gesture; before that, new messages show without sound. On by default; the choice is kept per device.
 */

const MUTE_KEY = 'tnarar:message-sound'
const PLAYED_KEY = 'tnarar:message-sound-played'
const LOCK = 'tnarar-message-sound'
const listeners = new Set()
let context = null

function storage() {
  try {
    return window.localStorage
  } catch {
    return null
  }
}

export function isMuted() {
  return storage()?.getItem(MUTE_KEY) === 'off'
}

export function setMuted(muted) {
  const store = storage()
  if (muted) store?.setItem(MUTE_KEY, 'off')
  else store?.removeItem(MUTE_KEY)
  listeners.forEach((listener) => listener())
}

function subscribe(listener) {
  listeners.add(listener)
  // Another tab changed it.
  const onStorage = (event) => event.key === MUTE_KEY && listener()
  window.addEventListener('storage', onStorage)
  return () => {
    listeners.delete(listener)
    window.removeEventListener('storage', onStorage)
  }
}

/** Whether message sounds are off, kept in step with other tabs. */
export function useMuted() {
  return useSyncExternalStore(subscribe, isMuted, () => false)
}

/** Prepares the audio on the user's first click, tap or key press (call once at start-up). */
export function unlockSoundOnFirstGesture() {
  const AudioContext = window.AudioContext ?? window.webkitAudioContext
  if (!AudioContext) return () => {}
  const unlock = () => {
    context ??= new AudioContext()
    context.resume?.()
    stop()
  }
  const events = ['pointerdown', 'keydown']
  const stop = () => events.forEach((name) => window.removeEventListener(name, unlock, true))
  events.forEach((name) => window.addEventListener(name, unlock, true))
  return stop
}

/** Two short sine notes, a fifth apart, gently faded in and out. */
function chime() {
  if (!context || context.state !== 'running') return false
  const start = context.currentTime
  ;[
    [880, 0],
    [1318.5, 0.12],
  ].forEach(([frequency, offset]) => {
    const oscillator = context.createOscillator()
    const gain = context.createGain()
    oscillator.type = 'sine'
    oscillator.frequency.value = frequency
    gain.gain.setValueAtTime(0.0001, start + offset)
    gain.gain.exponentialRampToValueAtTime(0.18, start + offset + 0.02)
    gain.gain.exponentialRampToValueAtTime(0.0001, start + offset + 0.35)
    oscillator.connect(gain).connect(context.destination)
    oscillator.start(start + offset)
    oscillator.stop(start + offset + 0.4)
  })
  return true
}

/** Remembers which messages already made a sound in any tab (the last few, for a minute). */
function claim(messageId, now = Date.now()) {
  const store = storage()
  if (!store) return true
  let played = {}
  try {
    played = JSON.parse(store.getItem(PLAYED_KEY) ?? '{}')
  } catch {
    played = {}
  }
  if (played[messageId]) return false
  const fresh = Object.fromEntries(Object.entries(played).filter(([, at]) => now - at < 60_000))
  store.setItem(PLAYED_KEY, JSON.stringify({ ...fresh, [messageId]: now }))
  return true
}

/**
 * Plays the chime for a new message once, however many tabs are open: tabs take turns through a Web Lock (held a
 * moment so the others, which got the same message, see it was played). Without Web Locks, the shared record alone
 * decides. Muted: nothing. Returns a promise of whether this tab played it.
 */
export async function playMessageSound(messageId) {
  if (isMuted()) return false
  const locks = navigator.locks
  if (!locks?.request) return claim(messageId) && chime()
  return locks.request(LOCK, { ifAvailable: true }, async (lock) => {
    if (!lock || !claim(messageId)) return false
    const played = chime()
    await new Promise((resolve) => setTimeout(resolve, 1500))
    return played
  })
}

/** For tests: forget the prepared audio. */
export function resetSound(audioContext = null) {
  context = audioContext
}
