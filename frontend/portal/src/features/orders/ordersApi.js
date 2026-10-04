import { baseApi } from '@/api/baseApi'

const LIST = { type: 'Orders', id: 'LIST' }

/** Every order action returns the whole order: put it in the cache, and refresh the lists (status and price change there). */
const action = (build, query) =>
  build.mutation({
    query,
    invalidatesTags: [LIST],
    async onQueryStarted(_arg, { dispatch, queryFulfilled }) {
      try {
        const { data } = await queryFulfilled
        dispatch(ordersApi.util.upsertQueryData('getOrder', data.id, data))
      } catch {
        // The page shows the error.
      }
    },
  })

/**
 * Orders the user is a party to, as customer or partner, and what they can do with them (T47). Each order
 * lists the viewer's possible next steps in `actions`, so screens only show buttons that will work.
 */
export const ordersApi = baseApi.injectEndpoints({
  endpoints: (build) => ({
    getOrders: build.query({
      query: ({ as, page = 1 } = {}) => ({ url: '/orders', params: { as: as || undefined, page, pageSize: 20 } }),
      providesTags: [LIST],
    }),
    getOrder: build.query({
      query: (id) => `/orders/${id}`,
      providesTags: (_result, _error, id) => [{ type: 'Orders', id }],
    }),
    startOrder: action(build, (id) => ({ url: `/orders/${id}/start`, method: 'POST' })),
    requestCompletion: action(build, (id) => ({ url: `/orders/${id}/request-completion`, method: 'POST' })),
    confirmCompletion: action(build, (id) => ({ url: `/orders/${id}/confirm-completion`, method: 'POST' })),
    rejectCompletion: action(build, ({ id, reason }) => ({ url: `/orders/${id}/reject-completion`, method: 'POST', body: { reason } })),
    cancelOrder: action(build, ({ id, reason }) => ({ url: `/orders/${id}/cancel`, method: 'POST', body: { reason } })),
    /** `{ id, kind: ExtraWork | Schedule, title, description, amount, newStartDate, newDurationDays, newVisitAt }`. */
    proposeChange: action(build, ({ id, ...body }) => ({ url: `/orders/${id}/change-requests`, method: 'POST', body })),
    acceptChange: action(build, ({ id, changeId }) => ({ url: `/orders/${id}/change-requests/${changeId}/accept`, method: 'POST' })),
    rejectChange: action(build, ({ id, changeId, note }) => ({ url: `/orders/${id}/change-requests/${changeId}/reject`, method: 'POST', body: { note } })),
    withdrawChange: action(build, ({ id, changeId }) => ({ url: `/orders/${id}/change-requests/${changeId}/withdraw`, method: 'POST' })),
    /** `{ id, amount, method: Cash | BankTransfer | Card | Other, paidOn, stageId, note }`: "I paid" / "I received" (T49). */
    recordPayment: action(build, ({ id, ...body }) => ({ url: `/orders/${id}/payments`, method: 'POST', body })),
    confirmPayment: action(build, ({ paymentId }) => ({ url: `/payments/${paymentId}/confirm`, method: 'POST' })),
    disputePayment: action(build, ({ paymentId, reason }) => ({ url: `/payments/${paymentId}/dispute`, method: 'POST', body: { reason } })),
    withdrawPayment: action(build, ({ paymentId }) => ({ url: `/payments/${paymentId}/withdraw`, method: 'POST' })),
    /** The customer's review of a completed order: `{ id, rating: 1–5, text }` (T52). */
    submitReview: action(build, ({ id, rating, text }) => ({ url: `/orders/${id}/review`, method: 'POST', body: { rating, text } })),
    replyToReview: action(build, ({ id, text }) => ({ url: `/orders/${id}/review/reply`, method: 'POST', body: { text } })),
  }),
})

export const {
  useGetOrdersQuery,
  useGetOrderQuery,
  useStartOrderMutation,
  useRequestCompletionMutation,
  useConfirmCompletionMutation,
  useRejectCompletionMutation,
  useCancelOrderMutation,
  useProposeChangeMutation,
  useAcceptChangeMutation,
  useRejectChangeMutation,
  useWithdrawChangeMutation,
  useRecordPaymentMutation,
  useConfirmPaymentMutation,
  useDisputePaymentMutation,
  useWithdrawPaymentMutation,
  useSubmitReviewMutation,
  useReplyToReviewMutation,
} = ordersApi
