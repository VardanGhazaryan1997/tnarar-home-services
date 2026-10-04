import { baseApi } from '@/api/baseApi'
import { withoutEmpty } from '@/shared/useUrlFilters'

/** Commission rates and partners' weekly statements (commissions.view to read, commissions.manage to change). */
export const commissionsApi = baseApi
  .enhanceEndpoints({ addTagTypes: ['CommissionRates', 'CommissionStatements', 'CommissionStatement'] })
  .injectEndpoints({
    endpoints: (build) => ({
      getCommissionRates: build.query({
        query: () => '/admin/commission-rates',
        providesTags: ['CommissionRates'],
      }),
      setDefaultRate: build.mutation({
        query: (percent) => ({ url: '/admin/commission-rates/default', method: 'PUT', body: { percent } }),
        invalidatesTags: ['CommissionRates'],
      }),
      setCategoryRate: build.mutation({
        query: ({ categoryId, percent }) => ({ url: `/admin/commission-rates/categories/${categoryId}`, method: 'PUT', body: { percent } }),
        invalidatesTags: ['CommissionRates'],
      }),
      clearCategoryRate: build.mutation({
        query: (categoryId) => ({ url: `/admin/commission-rates/categories/${categoryId}`, method: 'DELETE' }),
        invalidatesTags: ['CommissionRates'],
      }),
      getStatements: build.query({
        query: ({ filter, partnerId, page = 1 } = {}) => ({
          url: '/admin/commission-statements',
          params: withoutEmpty({ filter, partnerId, page, pageSize: 20 }),
        }),
        providesTags: ['CommissionStatements'],
      }),
      getStatement: build.query({
        query: (id) => `/admin/commission-statements/${id}`,
        providesTags: (result, error, id) => [{ type: 'CommissionStatement', id }],
      }),
      recordSettlement: build.mutation({
        query: ({ statementId, ...body }) => ({ url: `/admin/commission-statements/${statementId}/settlements`, method: 'POST', body }),
        invalidatesTags: (result, error, { statementId }) => ['CommissionStatements', { type: 'CommissionStatement', id: statementId }],
      }),
    }),
  })

export const {
  useGetCommissionRatesQuery,
  useSetDefaultRateMutation,
  useSetCategoryRateMutation,
  useClearCategoryRateMutation,
  useGetStatementsQuery,
  useGetStatementQuery,
  useRecordSettlementMutation,
} = commissionsApi
