import { createApi } from '@reduxjs/toolkit/query/react'
import { baseQueryWithReauth } from './baseQuery'
import { API_BASE_URL } from './config'

export { API_BASE_URL }

/**
 * The single RTK Query API for the Back Office. Admin endpoints use /admin/... paths.
 * Features add their endpoints with baseApi.injectEndpoints(...) in their own folder.
 */
export const baseApi = createApi({
  reducerPath: 'api',
  baseQuery: baseQueryWithReauth,
  tagTypes: ['Languages'],
  endpoints: () => ({}),
})
