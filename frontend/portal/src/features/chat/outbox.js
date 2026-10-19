import { useCallback, useSyncExternalStore } from 'react'
import { problemCode } from '@/api/errors'
import { useSendMessageMutation } from './chatApi'

/**
 * Messages on their way (O7), per conversation: "sending", or "failed" with the text and files kept so the user
 * can try again. Each has a client id the API recognises, so trying again never posts it twice. Kept in
 * localStorage too, so a failed message survives a reload (one still sending when the page closed counts as failed).
 */

const PREFIX = 'tnarar:outbox:'
const EMPTY = []
const listeners = new Set()
const outboxes = new Map()

function storage() {
  try {
    return window.localStorage
  } catch {
    return null
  }
}

function read(conversationId) {
  if (!outboxes.has(conversationId)) {
    let saved = []
    try {
      saved = JSON.parse(storage()?.getItem(PREFIX + conversationId) ?? '[]')
    } catch {
      saved = []
    }
    outboxes.set(conversationId, Array.isArray(saved) ? saved.map((item) => ({ ...item, status: 'failed' })) : [])
  }
  return outboxes.get(conversationId)
}

function write(conversationId, items) {
  outboxes.set(conversationId, items)
  const store = storage()
  if (items.length) store?.setItem(PREFIX + conversationId, JSON.stringify(items))
  else store?.removeItem(PREFIX + conversationId)
  listeners.forEach((listener) => listener())
}

const update = (conversationId, clientId, changes) =>
  write(
    conversationId,
    read(conversationId).map((item) => (item.clientId === clientId ? { ...item, ...changes } : item)),
  )

export const outbox = {
  add: (conversationId, item) => write(conversationId, [...read(conversationId), item]),
  remove: (conversationId, clientId) => write(conversationId, read(conversationId).filter((item) => item.clientId !== clientId)),
  update,
  /** For tests: forget everything. */
  reset: () => outboxes.clear(),
}

function subscribe(listener) {
  listeners.add(listener)
  return () => listeners.delete(listener)
}

/** The conversation's messages on their way, oldest first. */
export function useOutbox(conversationId) {
  return useSyncExternalStore(
    subscribe,
    () => (conversationId ? read(conversationId) : EMPTY),
    () => EMPTY,
  )
}

/** A random id for a new message (the API keeps it to recognise a retry). */
export function newClientId() {
  return crypto.randomUUID()
}

/**
 * Sending with a safety net: `send(item)` shows the message as sending and delivers it; on failure it stays as
 * failed with the reason. `retry(item)` delivers the same message again (same client id); `discard(item)` drops it.
 */
export function useOutboxSender(conversationId) {
  const [sendMessage] = useSendMessageMutation()

  const deliver = useCallback(
    async (item) => {
      outbox.update(conversationId, item.clientId, { status: 'sending', error: null })
      const result = await sendMessage({ id: conversationId, body: item.body, fileIds: item.fileIds, clientId: item.clientId })
      if (result.data) outbox.remove(conversationId, item.clientId)
      else outbox.update(conversationId, item.clientId, { status: 'failed', error: problemCode(result.error) })
    },
    [conversationId, sendMessage],
  )

  const send = useCallback(
    (item) => {
      outbox.add(conversationId, { ...item, status: 'sending', error: null })
      return deliver(item)
    },
    [conversationId, deliver],
  )

  const discard = useCallback((item) => outbox.remove(conversationId, item.clientId), [conversationId])

  return { send, retry: deliver, discard }
}
