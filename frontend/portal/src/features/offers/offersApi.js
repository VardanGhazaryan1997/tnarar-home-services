import { baseApi } from '@/api/baseApi'

/** Offers: partners send them on requests they received; customers compare and accept one. */
export const offersApi = baseApi.injectEndpoints({
  endpoints: (build) => ({
    sendOffer: build.mutation({
      query: ({ requestId, ...body }) => ({ url: `/requests/${requestId}/offers`, method: 'POST', body }),
      invalidatesTags: ['Offers', 'MyOffers', 'Inbox'],
    }),
    /** The customer: every offer but withdrawn ones; a partner: their own. */
    getRequestOffers: build.query({
      query: (requestId) => `/requests/${requestId}/offers`,
      providesTags: ['Offers'],
    }),
    getMyOffers: build.query({
      query: ({ status, page = 1 } = {}) => ({ url: '/offers/mine', params: { status: status || undefined, page, pageSize: 20 } }),
      providesTags: ['MyOffers'],
    }),
    /** Creates the order; accepting a work offer closes the request. Returns the order. */
    acceptOffer: build.mutation({
      query: (id) => ({ url: `/offers/${id}/accept`, method: 'POST' }),
      invalidatesTags: ['Offers', 'MyRequests', 'Orders'],
    }),
    rejectOffer: build.mutation({
      query: ({ id, reason }) => ({ url: `/offers/${id}/reject`, method: 'POST', body: { reason: reason || null } }),
      invalidatesTags: ['Offers'],
    }),
    withdrawOffer: build.mutation({
      query: (id) => ({ url: `/offers/${id}/withdraw`, method: 'POST' }),
      invalidatesTags: ['Offers', 'MyOffers'],
    }),
  }),
})

export const {
  useSendOfferMutation,
  useGetRequestOffersQuery,
  useGetMyOffersQuery,
  useAcceptOfferMutation,
  useRejectOfferMutation,
  useWithdrawOfferMutation,
} = offersApi
