import { baseApi } from '@/api/baseApi'
import { withoutEmpty } from '@/shared/useUrlFilters'

const BASE = '/admin/requests'

/** Service requests (requests.view to read, requests.manage to send to partners and cancel). */
export const requestsApi = baseApi.enhanceEndpoints({ addTagTypes: ['AdminRequests', 'AdminRequest'] }).injectEndpoints({
  endpoints: (build) => ({
    getRequests: build.query({
      query: ({ status, kind, needsAttention, search, page = 1 } = {}) => ({
        url: BASE,
        params: withoutEmpty({ status, kind, needsAttention, search: search?.trim(), page, pageSize: 20 }),
      }),
      providesTags: ['AdminRequests'],
    }),
    getRequest: build.query({
      query: (id) => `${BASE}/${id}`,
      providesTags: (result, error, id) => [{ type: 'AdminRequest', id }],
    }),
    assignRequest: build.mutation({
      query: ({ id, partnerIds }) => ({ url: `${BASE}/${id}/recipients`, method: 'POST', body: { partnerIds } }),
      invalidatesTags: (result, error, { id }) => ['AdminRequests', { type: 'AdminRequest', id }],
    }),
    cancelRequest: build.mutation({
      query: ({ id, reason }) => ({ url: `${BASE}/${id}/cancel`, method: 'POST', body: { reason } }),
      invalidatesTags: (result, error, { id }) => ['AdminRequests', { type: 'AdminRequest', id }],
    }),
  }),
})

export const { useGetRequestsQuery, useGetRequestQuery, useAssignRequestMutation, useCancelRequestMutation } = requestsApi
