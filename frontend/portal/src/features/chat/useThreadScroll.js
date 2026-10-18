import { useCallback, useEffect, useRef, useState } from 'react'

/** Within this many pixels of the end counts as "at the latest message". */
const NEAR_END_PX = 80

/**
 * Where the thread is scrolled, and what to do when messages arrive (O3):
 * - opening the conversation shows the first unread message (under a "New messages" divider), else the end;
 * - a new message scrolls to the end only if the user was already there, or sent it themselves;
 * - otherwise it stays put, counts as missed and the "Latest" button offers to jump down.
 * `messages`: the loaded messages, oldest first; `myRole`: Customer or Partner; `unread`: the unread count when the
 * conversation opened (null until known).
 */
export function useThreadScroll(messages, myRole, unread) {
  const scrollRef = useRef(null)
  const endRef = useRef(null)
  const [atEnd, setAtEnd] = useState(true)
  const [seen, setSeen] = useState({ newest: null, missed: 0, dividerId: null, scrollTo: null, turn: 0 })
  const ready = Boolean(myRole) && unread !== null && messages.length > 0
  const newest = messages.at(-1)?.id ?? null

  // New messages since the last render: decide where to go (adjusting state while rendering, as React suggests).
  if (ready && newest !== seen.newest) {
    if (seen.newest === null) {
      const fromOther = messages.filter((message) => message.senderRole !== myRole)
      const firstUnread = unread > 0 ? fromOther.at(-Math.min(unread, fromOther.length)) : null
      setSeen({ newest, missed: 0, dividerId: firstUnread?.id ?? null, scrollTo: firstUnread ? `message-${firstUnread.id}` : 'end', turn: 1 })
    } else {
      const arrived = messages.slice(messages.findIndex((message) => message.id === seen.newest) + 1)
      const theirs = arrived.filter((message) => message.senderRole !== myRole)
      const sentByMe = arrived.at(-1)?.senderRole === myRole
      if (atEnd || sentByMe) {
        setSeen({ ...seen, newest, missed: 0, scrollTo: 'end', turn: seen.turn + 1 })
      } else {
        setSeen({ ...seen, newest, missed: seen.missed + theirs.length, dividerId: seen.dividerId ?? theirs[0]?.id ?? null })
      }
    }
  }

  // Scroll where the decision above asked to (the page is the outside world here).
  useEffect(() => {
    if (seen.scrollTo === 'end') endRef.current?.scrollIntoView?.({ block: 'end' })
    else if (seen.scrollTo) document.getElementById(seen.scrollTo)?.scrollIntoView?.({ block: 'center' })
  }, [seen.scrollTo, seen.turn])

  const onScroll = useCallback(() => {
    const box = scrollRef.current
    if (!box) return
    const end = box.scrollHeight - box.scrollTop - box.clientHeight <= NEAR_END_PX
    setAtEnd(end)
    if (end) setSeen((current) => (current.missed ? { ...current, missed: 0 } : current))
  }, [])

  const scrollToEnd = useCallback(() => {
    endRef.current?.scrollIntoView?.({ block: 'end' })
    setAtEnd(true)
    setSeen((current) => ({ ...current, missed: 0 }))
  }, [])

  return { scrollRef, endRef, onScroll, scrollToEnd, missed: seen.missed, dividerId: seen.dividerId, showLatest: !atEnd }
}
