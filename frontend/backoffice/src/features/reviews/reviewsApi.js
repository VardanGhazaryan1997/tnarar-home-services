import { baseApi } from '@/api/baseApi'
import { withoutEmpty } from '@/shared/useUrlFilters'

const BASE = '/admin/reviews'
const changed = (result) => ['AdminReviews', ...(result ? [{ type: 'AdminOrder', id: result.orderId }] : [])]

/** Customers' reviews of partners (reviews.moderate): read, hide from the public profile, restore. */
export const reviewsApi = baseApi.enhanceEndpoints({ addTagTypes: ['AdminReviews', 'AdminOrder'] }).injectEndpoints({
  endpoints: (build) => ({
    getReviews: build.query({
      query: ({ hidden, maxRating, partnerId, page = 1 } = {}) => ({ url: BASE, params: withoutEmpty({ hidden, maxRating, partnerId, page, pageSize: 20 }) }),
      providesTags: ['AdminReviews'],
    }),
    hideReview: build.mutation({
      query: ({ id, reason }) => ({ url: `${BASE}/${id}/hide`, method: 'POST', body: { reason } }),
      invalidatesTags: changed,
    }),
    restoreReview: build.mutation({
      query: (id) => ({ url: `${BASE}/${id}/restore`, method: 'POST' }),
      invalidatesTags: changed,
    }),
  }),
})

export const { useGetReviewsQuery, useHideReviewMutation, useRestoreReviewMutation } = reviewsApi
