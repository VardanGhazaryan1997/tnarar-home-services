import { createSlice } from '@reduxjs/toolkit'

/**
 * What just changed on a request while the user had the Portal open (T1): the newest live event per request, so
 * pages can point it out (T5). `change` is what the user should notice:
 * - cancelled: the request was cancelled;
 * - accepted: the request closed because an offer was accepted (`orderId` is its order);
 * - closed: the request closed some other way;
 * - offer: a new offer arrived; offerAccepted / offerRejected / offerWithdrawn: an offer was decided.
 */
const liveSlice = createSlice({
  name: 'live',
  initialState: { requests: {} },
  reducers: {
    requestUpdated(state, { payload }) {
      const change = payload.status === 'Cancelled' ? 'cancelled' : payload.status === 'Closed' ? (payload.orderId ? 'accepted' : 'closed') : null
      if (change) state.requests[payload.requestId] = { change, status: payload.status, orderId: payload.orderId ?? null, at: payload.at }
    },
    offerUpdated(state, { payload }) {
      const change = OFFER_CHANGES[payload.status]
      // A request-level change (cancelled, accepted) says more than the offer that came with it.
      const current = state.requests[payload.requestId]
      if (!change || (current && REQUEST_CHANGES.includes(current.change) && current.at >= payload.at)) return
      state.requests[payload.requestId] = { change, offerId: payload.offerId, partnerId: payload.partnerId, orderId: payload.orderId ?? null, at: payload.at }
    },
    /** The user has seen it (the highlight faded): forget it. */
    changeSeen(state, { payload: requestId }) {
      delete state.requests[requestId]
    },
  },
})

const OFFER_CHANGES = { Sent: 'offer', Accepted: 'offerAccepted', Rejected: 'offerRejected', Withdrawn: 'offerWithdrawn' }
const REQUEST_CHANGES = ['cancelled', 'accepted', 'closed']

export const { requestUpdated, offerUpdated, changeSeen } = liveSlice.actions
export const liveReducer = liveSlice.reducer

/** The newest unseen change on a request, or undefined. */
export const selectRequestChange = (requestId) => (state) => state.live.requests[requestId]
