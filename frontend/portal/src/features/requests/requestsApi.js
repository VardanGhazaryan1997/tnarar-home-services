import { baseApi } from '@/api/baseApi'

/** Service requests: the customer's own, and the partner's inbox. */
export const requestsApi = baseApi.injectEndpoints({
  endpoints: (build) => ({
    createRequest: build.mutation({
      query: (body) => ({ url: '/requests', method: 'POST', body }),
      invalidatesTags: ['MyRequests'],
    }),
    getMyRequests: build.query({
      query: ({ status, page = 1 } = {}) => ({ url: '/requests/mine', params: { status: status || undefined, page, pageSize: 20 } }),
      providesTags: ['MyRequests'],
    }),
    getMyRequest: build.query({
      query: (id) => `/requests/${id}`,
      providesTags: (_result, _error, id) => [{ type: 'MyRequests', id }],
    }),
    cancelMyRequest: build.mutation({
      query: ({ id, reason }) => ({ url: `/requests/${id}/cancel`, method: 'POST', body: { reason: reason || null } }),
      invalidatesTags: ['MyRequests', 'Offers'],
    }),
    getInbox: build.query({
      query: ({ status, page = 1 } = {}) => ({ url: '/requests/inbox', params: { status: status || undefined, page, pageSize: 20 } }),
      providesTags: ['Inbox'],
    }),
    /** Opening a received request marks it viewed. */
    getInboxRequest: build.query({
      query: (id) => `/requests/inbox/${id}`,
      providesTags: (_result, _error, id) => [{ type: 'Inbox', id }],
    }),
    declineInboxRequest: build.mutation({
      query: ({ id, reason }) => ({ url: `/requests/inbox/${id}/decline`, method: 'POST', body: { reason: reason || null } }),
      invalidatesTags: ['Inbox'],
    }),
  }),
})

export const {
  useCreateRequestMutation,
  useGetMyRequestsQuery,
  useGetMyRequestQuery,
  useCancelMyRequestMutation,
  useGetInboxQuery,
  useGetInboxRequestQuery,
  useDeclineInboxRequestMutation,
} = requestsApi
