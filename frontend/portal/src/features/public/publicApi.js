import { useTranslation } from 'react-i18next'
import { baseApi } from '@/api/baseApi'

export const SEARCH_PAGE_SIZE = 12
export const REVIEWS_PAGE_SIZE = 5

/**
 * What visitors see without signing in: approved partners, information pages and FAQs. Every query takes
 * the UI language (`lng`) so switching language loads translated names; it isn't sent as a parameter.
 */
export const publicApi = baseApi.injectEndpoints({
  endpoints: (build) => ({
    /** `{ category, city, district, type, search, page, pageSize }`, all optional; slugs, not ids. */
    searchPartners: build.query({
      query: (args) => ({ url: '/partners', params: withoutEmpty({ ...args, lng: undefined }) }),
    }),
    getPartner: build.query({ query: ({ slug }) => `/partners/${encodeURIComponent(slug)}` }),
    /** A partner's visible reviews, newest first (T52). */
    getPartnerReviews: build.query({
      query: ({ slug, page = 1 }) => ({ url: `/partners/${encodeURIComponent(slug)}/reviews`, params: { page, pageSize: REVIEWS_PAGE_SIZE } }),
    }),
    getPages: build.query({ query: () => '/pages', keepUnusedDataFor: 60 * 30 }),
    getPage: build.query({ query: ({ slug }) => `/pages/${encodeURIComponent(slug)}` }),
    /** `audience`: General | Customers | Partners, or nothing for all. */
    getFaqs: build.query({ query: ({ audience }) => ({ url: '/faqs', params: withoutEmpty({ audience }) }), keepUnusedDataFor: 60 * 30 }),
  }),
})

function withoutEmpty(params) {
  return Object.fromEntries(Object.entries(params).filter(([, value]) => value !== undefined && value !== null && value !== ''))
}

const useLanguage = () => useTranslation().i18n.language

export const useSearchPartners = (params, options) => publicApi.useSearchPartnersQuery({ ...params, lng: useLanguage() }, options)
export const usePartner = (slug) => publicApi.useGetPartnerQuery({ slug, lng: useLanguage() })
export const usePartnerReviews = (slug, page) => publicApi.useGetPartnerReviewsQuery({ slug, page })
export const usePages = () => publicApi.useGetPagesQuery({ lng: useLanguage() })
export const usePage = (slug) => publicApi.useGetPageQuery({ slug, lng: useLanguage() })
export const useFaqs = (audience) => publicApi.useGetFaqsQuery({ audience, lng: useLanguage() })
