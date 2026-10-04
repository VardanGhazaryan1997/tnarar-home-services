import { baseApi } from '@/api/baseApi'

const BASE = '/admin/users'

/** Portal users (users.view to read, users.block to block and unblock). See backend/README "Users". */
export const usersApi = baseApi.enhanceEndpoints({ addTagTypes: ['Users', 'User', 'Partners', 'Partner'] }).injectEndpoints({
  endpoints: (build) => ({
    getUsers: build.query({
      query: ({ search, role, status, page = 1, pageSize = 50 } = {}) => ({
        url: BASE,
        params: Object.fromEntries(
          Object.entries({ search: search?.trim(), role, status, page, pageSize }).filter(([, value]) => value !== undefined && value !== ''),
        ),
      }),
      providesTags: ['Users'],
    }),
    getUser: build.query({
      query: (id) => `${BASE}/${id}`,
      providesTags: (result, error, id) => [{ type: 'User', id }],
    }),
    // Blocking also hides the user's partner profile, so partner pages refresh too.
    blockUser: build.mutation({
      query: ({ id, reason }) => ({ url: `${BASE}/${id}/block`, method: 'POST', body: { reason } }),
      invalidatesTags: (result, error, { id }) => ['Users', { type: 'User', id }, 'Partners', 'Partner'],
    }),
    unblockUser: build.mutation({
      query: (id) => ({ url: `${BASE}/${id}/unblock`, method: 'POST' }),
      invalidatesTags: (result, error, id) => ['Users', { type: 'User', id }, 'Partners', 'Partner'],
    }),
  }),
})

export const { useGetUsersQuery, useGetUserQuery, useBlockUserMutation, useUnblockUserMutation } = usersApi
