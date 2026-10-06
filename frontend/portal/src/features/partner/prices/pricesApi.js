import { baseApi } from '@/api/baseApi'

const URL = '/me/partner-profile/prices'

/** The partner's price list (A4): every work item in the services they offer, with the market range and their price. */
export const pricesApi = baseApi.injectEndpoints({
  endpoints: (build) => ({
    /** `{ items: [{ workItemId, name, unit, categoryName, mainCategoryName, marketMin…, priceFrom, priceTo, includesMaterials }] }`. */
    getMyPrices: build.query({ query: () => URL }),
    /** Replaces the list: `[{ workItemId, priceFrom, priceTo, includesMaterials }]`. Returns the new list. */
    saveMyPrices: build.mutation({
      query: (prices) => ({ url: URL, method: 'PUT', body: { prices } }),
      async onQueryStarted(_prices, { dispatch, queryFulfilled }) {
        try {
          const { data } = await queryFulfilled
          dispatch(pricesApi.util.upsertQueryData('getMyPrices', undefined, data))
        } catch {
          // The page shows the error.
        }
      },
    }),
  }),
})

export const { useGetMyPricesQuery, useSaveMyPricesMutation } = pricesApi
