import { baseApi } from '@/api/baseApi'
import { withoutEmpty } from '@/shared/useUrlFilters'

/** Payment records between customers and partners (payments.view to read, payments.manage to decide disputes). */
export const paymentsApi = baseApi.enhanceEndpoints({ addTagTypes: ['AdminPayments', 'AdminOrder'] }).injectEndpoints({
  endpoints: (build) => ({
    getPayments: build.query({
      query: ({ status, orderId, page = 1 } = {}) => ({ url: '/admin/payments', params: withoutEmpty({ status, orderId, page, pageSize: 20 }) }),
      providesTags: ['AdminPayments'],
    }),
    resolvePayment: build.mutation({
      query: ({ id, counts, note }) => ({ url: `/admin/payments/${id}/resolve`, method: 'POST', body: { counts, note } }),
      invalidatesTags: (result) => ['AdminPayments', ...(result ? [{ type: 'AdminOrder', id: result.orderId }] : [])],
    }),
  }),
})

export const { useGetPaymentsQuery, useResolvePaymentMutation } = paymentsApi
