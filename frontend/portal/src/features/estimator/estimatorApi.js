import { baseApi } from '@/api/baseApi'

/** The renovation estimator (B3/B4): quick estimates, room templates, measuring and pricing rooms. Open to everyone. */
export const estimatorApi = baseApi.injectEndpoints({
  endpoints: (build) => ({
    /** `{ roomType, area, height?, oldBuilding? }` → `{ rooms: [{ lines }], totalMin, totalTypical, totalMax, unpricedLines }`. */
    quickEstimate: build.mutation({
      query: (body) => ({ url: '/estimates/quick', method: 'POST', body }),
    }),
  }),
})

export const { useQuickEstimateMutation } = estimatorApi

/** Room types in the order the estimator offers them. */
export const ROOM_TYPES = ['LivingRoom', 'Bedroom', 'KidsRoom', 'Kitchen', 'Bathroom', 'Toilet', 'Hallway', 'Balcony', 'Office', 'Garage', 'Other']

/** The largest floor area the estimator takes (m²). */
export const MAX_AREA = 2000

/** "12,5" or "12.5" → 12.5; blank or not a number → NaN. */
export function parseArea(text) {
  const cleaned = String(text ?? '').trim().replace(',', '.')
  return cleaned === '' || !/^\d+(\.\d+)?$/.test(cleaned) ? Number.NaN : Number(cleaned)
}
