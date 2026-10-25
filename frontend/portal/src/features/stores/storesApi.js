import { useTranslation } from 'react-i18next'
import { baseApi } from '@/api/baseApi'

/**
 * Public store prices (E2): building materials with the usual price per unit and every store's offer for them, and
 * stores with their branches and products. Every query takes the UI language (`lng`) so switching language loads
 * translated names; it isn't sent as a parameter.
 */
export const storesApi = baseApi.injectEndpoints({
  endpoints: (build) => ({
    /** `{ region }` (a region slug), optional. */
    getMaterialPrices: build.query({ query: ({ region }) => ({ url: '/materials', params: withoutEmpty({ region }) }) }),
    /** `{ slug, region, grade, inStock }`: the material with its offers, in-stock first and then cheapest per unit. */
    getMaterialOffers: build.query({
      query: ({ slug, region, grade, inStock }) => ({
        url: `/materials/${encodeURIComponent(slug)}`,
        params: withoutEmpty({ region, grade, inStock: inStock ? 'true' : undefined }),
      }),
    }),
    /** `{ region }` (a region slug), optional: products on sale today, biggest discount first. */
    getOnSale: build.query({ query: ({ region }) => ({ url: '/materials/on-sale', params: withoutEmpty({ region }) }) }),
    getStore: build.query({ query: ({ slug }) => `/stores/${encodeURIComponent(slug)}` }),
    /**
     * `{ grade, materials: [{ materialId, quantity }], region? }` → `{ cheapest, stores }`: the cheapest store offer per
     * material in whole packs, and each store's total for the materials it has. A read, so it is cached like one.
     */
    getWhereToBuy: build.query({
      query: (body) => ({ url: '/materials/where-to-buy', method: 'POST', body }),
      keepUnusedDataFor: 60,
    }),
  }),
})

function withoutEmpty(params) {
  return Object.fromEntries(Object.entries(params).filter(([, value]) => value !== undefined && value !== null && value !== ''))
}

const useLanguage = () => useTranslation().i18n.language

export const useMaterialPrices = (params) => storesApi.useGetMaterialPricesQuery({ ...params, lng: useLanguage() })
export const useMaterialOffers = (params) => storesApi.useGetMaterialOffersQuery({ ...params, lng: useLanguage() })
export const useStore = (slug) => storesApi.useGetStoreQuery({ slug, lng: useLanguage() })
export const useWhereToBuy = (body, options) => storesApi.useGetWhereToBuyQuery(body, options)
export const useOnSale = (params) => storesApi.useGetOnSaleQuery({ ...params, lng: useLanguage() })
