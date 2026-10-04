import { createApi } from '@reduxjs/toolkit/query/react'
import { API_BASE_URL } from './config'
import { baseQueryWithReauth } from './baseQuery'

export { API_BASE_URL }

/**
 * The single RTK Query API for the Portal. Features add their endpoints with
 * baseApi.injectEndpoints(...) in their own folder.
 */
export const baseApi = createApi({
  reducerPath: 'api',
  baseQuery: baseQueryWithReauth,
  tagTypes: ['Languages', 'Catalog', 'Me', 'MyRequests', 'Inbox', 'Offers', 'MyOffers', 'Orders', 'Conversations', 'Messages', 'PartnerProfile', 'Notifications', 'Commissions'],
  endpoints: () => ({}),
})
