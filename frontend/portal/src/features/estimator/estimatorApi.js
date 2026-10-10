import { useTranslation } from 'react-i18next'
import { baseApi } from '@/api/baseApi'

/**
 * The renovation estimator: quick estimates, room templates, measuring and pricing rooms (open to everyone), the
 * signed-in user's saved estimates and shared estimates. Work names come in the UI language, so the language is part of
 * the cache key of the queries.
 */
export const estimatorApi = baseApi.enhanceEndpoints({ addTagTypes: ['Estimates'] }).injectEndpoints({
  endpoints: (build) => ({
    /** `{ roomType, area, height?, oldBuilding? }` → `{ rooms: [{ lines }], totalMin, totalTypical, totalMax, unpricedLines }`. */
    quickEstimate: build.mutation({
      query: (body) => ({ url: '/estimates/quick', method: 'POST', body }),
    }),
    /** `{ rooms, oldBuilding }` → the same shape as a quick estimate, a room per room sent. */
    measureEstimate: build.mutation({
      query: (body) => ({ url: '/estimates/measure', method: 'POST', body }),
    }),
    getRoomTemplates: build.query({ query: () => '/estimates/templates', keepUnusedDataFor: 60 * 30 }),
    getWorkItems: build.query({ query: () => '/work-items', keepUnusedDataFor: 60 * 30 }),

    getMyEstimates: build.query({ query: () => '/me/estimates', providesTags: ['Estimates'] }),
    getMyEstimate: build.query({ query: ({ id }) => `/me/estimates/${id}`, providesTags: ['Estimates'] }),
    createEstimate: build.mutation({
      query: (body) => ({ url: '/me/estimates', method: 'POST', body }),
      invalidatesTags: ['Estimates'],
    }),
    updateEstimate: build.mutation({
      query: ({ id, ...body }) => ({ url: `/me/estimates/${id}`, method: 'PUT', body }),
      invalidatesTags: ['Estimates'],
    }),
    deleteEstimate: build.mutation({
      query: (id) => ({ url: `/me/estimates/${id}`, method: 'DELETE' }),
      invalidatesTags: ['Estimates'],
    }),
    shareEstimate: build.mutation({
      query: ({ id, share }) => ({ url: `/me/estimates/${id}/share`, method: share ? 'POST' : 'DELETE' }),
      invalidatesTags: ['Estimates'],
    }),
    getSharedEstimate: build.query({ query: ({ token }) => `/estimates/shared/${encodeURIComponent(token)}` }),
  }),
})

export const {
  useQuickEstimateMutation,
  useMeasureEstimateMutation,
  useGetMyEstimatesQuery,
  useCreateEstimateMutation,
  useUpdateEstimateMutation,
  useDeleteEstimateMutation,
  useShareEstimateMutation,
} = estimatorApi

/** Room templates in the UI language. */
export function useRoomTemplates() {
  const { i18n } = useTranslation()
  return estimatorApi.useGetRoomTemplatesQuery(i18n.language)
}

/** Every active work item in the UI language (`skip` loads nothing). */
export function useWorkItems({ skip = false } = {}) {
  const { i18n } = useTranslation()
  return estimatorApi.useGetWorkItemsQuery(i18n.language, { skip })
}

/** One of the user's estimates (names in the UI language); nothing is loaded without an id. */
export function useMyEstimate(id) {
  const { i18n } = useTranslation()
  return estimatorApi.useGetMyEstimateQuery({ id, lng: i18n.language }, { skip: !id })
}

/** A shared estimate by its link token. */
export function useSharedEstimate(token) {
  const { i18n } = useTranslation()
  return estimatorApi.useGetSharedEstimateQuery({ token, lng: i18n.language })
}

/** Room types in the order the estimator offers them. */
export const ROOM_TYPES = ['LivingRoom', 'Bedroom', 'KidsRoom', 'Kitchen', 'Bathroom', 'Toilet', 'Hallway', 'Balcony', 'Office', 'Garage', 'Other']

/** The largest floor area the estimator takes (m²). */
export const MAX_AREA = 2000

/** "12,5" or "12.5" → 12.5; blank or not a number → NaN. */
export function parseArea(text) {
  const cleaned = String(text ?? '').trim().replace(',', '.')
  return cleaned === '' || !/^\d+(\.\d+)?$/.test(cleaned) ? Number.NaN : Number(cleaned)
}
