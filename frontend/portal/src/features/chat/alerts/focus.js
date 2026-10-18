/**
 * Which conversation the user is reading right now: open in a tab that is visible and focused. Shared with the
 * user's other tabs through localStorage, so a background tab doesn't chime for a message the user is reading
 * in another one. Anything else — a background tab, another conversation — still gets the new-message alert.
 */

const KEY = 'tnarar:reading-conversation'
// A tab that went away without saying so (crash, closed laptop) stops counting after this long.
const FRESH_MS = 2 * 60_000
const tabId = Math.random().toString(36).slice(2)
let open = null

function storage() {
  try {
    return window.localStorage
  } catch {
    return null
  }
}

const tabIsActive = () => document.visibilityState === 'visible' && document.hasFocus()

function publish() {
  const store = storage()
  if (!store) return
  const reading = open && tabIsActive() ? open : null
  const current = read()
  if (reading) store.setItem(KEY, JSON.stringify({ id: reading, tab: tabId, at: Date.now() }))
  else if (current?.tab === tabId) store.removeItem(KEY)
}

function read() {
  try {
    return JSON.parse(storage()?.getItem(KEY) ?? 'null')
  } catch {
    return null
  }
}

/** Whether the user is reading this conversation in this tab or another one. */
export function isReading(conversationId) {
  if (open === conversationId && tabIsActive()) return true
  const other = read()
  return other?.id === conversationId && Date.now() - other.at < FRESH_MS
}

/**
 * The thread calls this while it shows a conversation (and with null when it closes). Keeps the shared note in
 * step with this tab's focus and visibility until then.
 */
export function watchOpenConversation(conversationId) {
  open = conversationId
  publish()
  const events = ['focus', 'blur', 'visibilitychange']
  const targets = { focus: window, blur: window, visibilitychange: document }
  events.forEach((name) => targets[name].addEventListener(name, publish))
  // Keep the note fresh while the user stays on the page.
  const timer = setInterval(publish, FRESH_MS / 2)
  return () => {
    events.forEach((name) => targets[name].removeEventListener(name, publish))
    clearInterval(timer)
    if (open === conversationId) open = null
    publish()
  }
}
