import { baseApi } from '@/api/baseApi'

const BASE = '/admin/partners'

// Decisions and the endpoint each one calls; "needsComment" ones require a comment for the partner.
export const DECISIONS = {
  approve: { path: 'approve', needsComment: false },
  requestChanges: { path: 'request-changes', needsComment: true },
  reject: { path: 'reject', needsComment: true },
  suspend: { path: 'suspend', needsComment: true },
  reinstate: { path: 'reinstate', needsComment: false },
}

/** Decisions staff can take on a profile in each status. */
export const DECISIONS_BY_STATUS = {
  UnderReview: ['approve', 'requestChanges', 'reject'],
  Approved: ['suspend'],
  Suspended: ['reinstate'],
}

/** Partner review (partners.view to read, partners.approve to decide). See backend/README "Reviewing partners". */
export const partnersApi = baseApi.enhanceEndpoints({ addTagTypes: ['Partners', 'Partner'] }).injectEndpoints({
  endpoints: (build) => ({
    getPartners: build.query({
      query: ({ status, type, search, page = 1, pageSize = 20 } = {}) => ({
        url: BASE,
        params: Object.fromEntries(
          Object.entries({ status, type, search: search?.trim(), page, pageSize }).filter(([, value]) => value !== undefined && value !== ''),
        ),
      }),
      providesTags: ['Partners'],
    }),
    getPartner: build.query({
      query: (id) => `${BASE}/${id}`,
      providesTags: (result, error, id) => [{ type: 'Partner', id }],
    }),
    decideOnPartner: build.mutation({
      query: ({ id, decision, comment }) => ({
        url: `${BASE}/${id}/${DECISIONS[decision].path}`,
        method: 'POST',
        body: DECISIONS[decision].needsComment ? { comment } : undefined,
      }),
      invalidatesTags: (result, error, { id }) => ['Partners', { type: 'Partner', id }],
    }),

    // Public catalog in the UI language (no permission needed), for category and place names.
    getCategoryNames: build.query({
      query: (language) => ({ url: '/categories', headers: { 'Accept-Language': language } }),
    }),
    getPlaceNames: build.query({
      query: (language) => ({ url: '/cities', headers: { 'Accept-Language': language } }),
    }),
  }),
})

export const {
  useGetPartnersQuery,
  useGetPartnerQuery,
  useDecideOnPartnerMutation,
  useGetCategoryNamesQuery,
  useGetPlaceNamesQuery,
} = partnersApi
