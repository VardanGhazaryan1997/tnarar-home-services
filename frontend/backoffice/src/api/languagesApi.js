import { baseApi } from './baseApi'

/** Active languages, managed in the Back Office: [{ code, name, nativeName, isDefault }]. */
export const languagesApi = baseApi.injectEndpoints({
  endpoints: (build) => ({
    getLanguages: build.query({
      query: () => '/languages',
      providesTags: ['Languages'],
      keepUnusedDataFor: 60 * 60,
    }),
  }),
})

export const { useGetLanguagesQuery } = languagesApi
