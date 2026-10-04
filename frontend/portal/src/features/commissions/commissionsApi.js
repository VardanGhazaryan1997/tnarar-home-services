import { baseApi } from '@/api/baseApi'

/** The signed-in partner's commissions: totals, per-order charges and weekly statements (read-only). */
export const commissionsApi = baseApi.injectEndpoints({
  endpoints: (build) => ({
    getCommissionSummary: build.query({
      query: () => '/me/commissions/summary',
      providesTags: ['Commissions'],
    }),
    getMyCommissions: build.query({
      query: ({ unbilled = false, page = 1 } = {}) => ({ url: '/me/commissions', params: { unbilled: unbilled || undefined, page, pageSize: 20 } }),
      providesTags: ['Commissions'],
    }),
    getMyStatements: build.query({
      query: ({ page = 1 } = {}) => ({ url: '/me/commission-statements', params: { page, pageSize: 20 } }),
      providesTags: ['Commissions'],
    }),
    getMyStatement: build.query({
      query: (id) => `/me/commission-statements/${id}`,
      providesTags: ['Commissions'],
    }),
  }),
})

export const { useGetCommissionSummaryQuery, useGetMyCommissionsQuery, useGetMyStatementsQuery, useGetMyStatementQuery } = commissionsApi
