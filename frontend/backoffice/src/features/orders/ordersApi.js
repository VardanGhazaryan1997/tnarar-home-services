import { baseApi } from '@/api/baseApi'
import { withoutEmpty } from '@/shared/useUrlFilters'

const BASE = '/admin/orders'

/** Orders (orders.view to read, orders.manage to cancel and resolve). Payments and reviews refresh the order too. */
export const ordersApi = baseApi.enhanceEndpoints({ addTagTypes: ['AdminOrders', 'AdminOrder'] }).injectEndpoints({
  endpoints: (build) => ({
    getOrders: build.query({
      query: ({ status, needsAttention, search, page = 1 } = {}) => ({
        url: BASE,
        params: withoutEmpty({ status, needsAttention, search: search?.trim(), page, pageSize: 20 }),
      }),
      providesTags: ['AdminOrders'],
    }),
    getOrder: build.query({
      query: (id) => `${BASE}/${id}`,
      providesTags: (result, error, id) => [{ type: 'AdminOrder', id }],
    }),
    cancelOrder: build.mutation({
      query: ({ id, reason }) => ({ url: `${BASE}/${id}/cancel`, method: 'POST', body: { reason } }),
      invalidatesTags: (result, error, { id }) => ['AdminOrders', { type: 'AdminOrder', id }],
    }),
    resolveOrder: build.mutation({
      query: (id) => ({ url: `${BASE}/${id}/resolve`, method: 'POST' }),
      invalidatesTags: (result, error, id) => ['AdminOrders', { type: 'AdminOrder', id }],
    }),
  }),
})

export const { useGetOrdersQuery, useGetOrderQuery, useCancelOrderMutation, useResolveOrderMutation } = ordersApi
