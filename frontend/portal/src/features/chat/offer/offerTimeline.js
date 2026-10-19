import { useEffect, useState } from 'react'

/** The offer the panel shows: the one still waiting, if any, otherwise the newest. */
export function currentOffer(offers = []) {
  return [...offers].reverse().find((offer) => offer.status === 'Sent') ?? offers.at(-1) ?? null
}

/** An offer's status right now: a waiting offer past its expiry is expired, even before the server says so. */
export function statusNow(offer, now = new Date()) {
  return offer.status === 'Sent' && new Date(offer.expiresAt) <= now ? 'Expired' : offer.status
}

/**
 * The offer events to show between the messages, oldest first: sent, then accepted, declined, withdrawn or
 * expired. `since`: leave out events before it (older messages aren't loaded yet).
 */
export function offerEvents(offers = [], now = new Date(), since = null) {
  const events = offers.flatMap((offer) => {
    const list = [{ id: `${offer.id}-sent`, kind: 'sent', at: offer.sentAt, offer }]
    const status = statusNow(offer, now)
    const decided = { Accepted: 'accepted', Rejected: 'rejected', Withdrawn: 'withdrawn' }[status]
    if (decided && offer.decidedAt) list.push({ id: `${offer.id}-${decided}`, kind: decided, at: offer.decidedAt, offer })
    if (status === 'Expired') list.push({ id: `${offer.id}-expired`, kind: 'expired', at: offer.expiresAt, offer })
    return list
  })
  return events.filter((event) => !since || new Date(event.at) >= new Date(since)).sort((a, b) => new Date(a.at) - new Date(b.at))
}

/** The current time, updated every `everyMs` while the component is shown (for countdowns). */
export function useNow(everyMs = 30_000) {
  const [now, setNow] = useState(() => new Date())
  useEffect(() => {
    const timer = setInterval(() => setNow(new Date()), everyMs)
    return () => clearInterval(timer)
  }, [everyMs])
  return now
}
