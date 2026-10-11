import { baseApi } from '@/api/baseApi'
import { withoutEmpty } from '@/shared/useUrlFilters'

const BASE = '/admin/estimates'

/** Customers' saved estimates and how many become requests (requests.view). */
export const estimatesApi = baseApi.injectEndpoints({
  endpoints: (build) => ({
    getEstimates: build.query({
      query: ({ search, withRequest, shared, page = 1 } = {}) => ({
        url: BASE,
        params: withoutEmpty({ search: search?.trim(), withRequest, shared, page, pageSize: 20 }),
      }),
    }),
    getEstimateStats: build.query({ query: () => `${BASE}/stats` }),
    getEstimate: build.query({ query: (id) => `${BASE}/${id}` }),
  }),
})

export const { useGetEstimatesQuery, useGetEstimateStatsQuery, useGetEstimateQuery } = estimatesApi
