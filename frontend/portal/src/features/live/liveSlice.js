import { createSlice } from '@reduxjs/toolkit'

/**
 * What just changed on a request while the user had the Portal open (T1): per request, the newest status change
 * of the request itself and of an offer on it, so pages can point them out (T5). The request and the offer are
 * kept apart because the two sides read them differently: when a customer accepts an offer, every partner hears
 * that the request closed, but only the chosen one hears that their offer was accepted.
 */
const liveSlice = createSlice({
  name: 'live',
  initialState: { requests: {} },
  reducers: {
    requestUpdated(state, { payload }) {
      const entry = (state.requests[payload.requestId] ??= {})
      entry.request = { status: payload.status, orderId: payload.orderId ?? null, at: payload.at }
    },
    offerUpdated(state, { payload }) {
      const entry = (state.requests[payload.requestId] ??= {})
      entry.offer = { status: payload.status, offerId: payload.offerId, partnerId: payload.partnerId, orderId: payload.orderId ?? null, at: payload.at }
    },
    /** The user has seen it (the highlight faded): forget it. */
    changeSeen(state, { payload: requestId }) {
      delete state.requests[requestId]
    },
  },
})

export const { requestUpdated, offerUpdated, changeSeen } = liveSlice.actions
export const liveReducer = liveSlice.reducer

/**
 * What the customer should notice on their request: cancelled, accepted (closed by an accepted offer, with its
 * order), closed some other way, a new offer, or an offer withdrawn. Undefined when nothing changed.
 */
export function customerChange(entry) {
  const request = entry?.request
  if (request?.status === 'Cancelled') return { kind: 'cancelled', at: request.at }
  if (request?.status === 'Closed') return request.orderId ? { kind: 'accepted', orderId: request.orderId, at: request.at } : { kind: 'closed', at: request.at }
  const offer = entry?.offer
  if (offer?.status === 'Sent') return { kind: 'offer', at: offer.at }
  if (offer?.status === 'Withdrawn') return { kind: 'offerWithdrawn', at: offer.at }
  return undefined
}

/**
 * What a partner should notice on a request they received: it was cancelled, their offer was accepted or turned
 * down, or the customer chose someone else. Their own sends and withdrawals need no pointing out.
 */
export function partnerChange(entry) {
  const request = entry?.request
  const offer = entry?.offer
  if (request?.status === 'Cancelled') return { kind: 'cancelled', at: request.at }
  if (offer?.status === 'Accepted') return { kind: 'offerAccepted', orderId: offer.orderId, at: offer.at }
  if (request?.status === 'Closed') return { kind: 'closedOther', at: request.at }
  if (offer?.status === 'Rejected') return { kind: 'offerRejected', at: offer.at }
  return undefined
}

/** The raw live entry for a request (see customerChange / partnerChange). */
export const selectLiveEntry = (requestId) => (state) => state.live.requests[requestId]
