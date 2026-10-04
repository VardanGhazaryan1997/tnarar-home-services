import { baseApi } from '@/api/baseApi'
import { profileUpdated, selectUser } from '@/features/auth/authSlice'

const PROFILE_URL = '/me/partner-profile'

/** Every change returns the whole profile: put it in the cache instead of fetching again. */
const replaceProfile = async (_arg, { dispatch, queryFulfilled }) => {
  try {
    const { data } = await queryFulfilled
    dispatch(partnerProfileApi.util.upsertQueryData('getMyPartnerProfile', undefined, data))
  } catch {
    // The page shows the error.
  }
}

/** The signed-in user's partner profile (T23): create and edit it, add work examples and documents, submit it. */
export const partnerProfileApi = baseApi.injectEndpoints({
  endpoints: (build) => ({
    /** 404 "partner.not_found" until the user saves it the first time. */
    getMyPartnerProfile: build.query({ query: () => PROFILE_URL, providesTags: ['PartnerProfile'] }),
    /** `{ type, displayName, about, yearsOfExperience, avatarFileId, categoryIds, areas: [{ cityId, districtId }] }`. */
    savePartnerProfile: build.mutation({
      query: (body) => ({ url: PROFILE_URL, method: 'PUT', body }),
      async onQueryStarted(_arg, { dispatch, getState, queryFulfilled }) {
        try {
          const { data } = await queryFulfilled
          dispatch(partnerProfileApi.util.upsertQueryData('getMyPartnerProfile', undefined, data))
          // Saving a profile makes the user a partner; the API adds the role, so the session follows.
          const user = selectUser(getState())
          if (user && !user.roles.includes('Partner')) dispatch(profileUpdated({ ...user, roles: [...user.roles, 'Partner'] }))
        } catch {
          // The page shows the error.
        }
      },
    }),
    /** `{ kind: WorkExample | Document, fileId, caption }`. */
    addPartnerMedia: build.mutation({
      query: (body) => ({ url: `${PROFILE_URL}/media`, method: 'POST', body }),
      onQueryStarted: replaceProfile,
    }),
    removePartnerMedia: build.mutation({
      query: (mediaId) => ({ url: `${PROFILE_URL}/media/${mediaId}`, method: 'DELETE' }),
      onQueryStarted: replaceProfile,
    }),
    submitPartnerProfile: build.mutation({
      query: () => ({ url: `${PROFILE_URL}/submit`, method: 'POST' }),
      onQueryStarted: replaceProfile,
    }),
  }),
})

export const {
  useGetMyPartnerProfileQuery,
  useSavePartnerProfileMutation,
  useAddPartnerMediaMutation,
  useRemovePartnerMediaMutation,
  useSubmitPartnerProfileMutation,
} = partnerProfileApi
