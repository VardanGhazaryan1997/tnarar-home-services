import { createAsyncThunk } from '@reduxjs/toolkit'
import { baseApi } from '@/api/baseApi'
import { AUTH_PATH } from '@/api/baseQuery'
import { challengeReceived, sessionStarted, signedOut } from './authSlice'

const startSession = async (_arg, { dispatch, queryFulfilled }) => {
  try {
    const { data } = await queryFulfilled
    dispatch(sessionStarted(data))
  } catch {
    // The page shows the error.
  }
}

export const authApi = baseApi.injectEndpoints({
  endpoints: (build) => ({
    /** Step 1: email + password. Returns a challenge for the authenticator step. */
    signIn: build.mutation({
      query: (body) => ({ url: `${AUTH_PATH}login`, method: 'POST', body }),
      onQueryStarted: async (_arg, { dispatch, queryFulfilled }) => {
        try {
          const { data } = await queryFulfilled
          dispatch(challengeReceived(data))
        } catch {
          // The page shows the error.
        }
      },
    }),
    /** Step 2 on first sign-in: confirms the new authenticator app. */
    completeTwoFactorSetup: build.mutation({
      query: (body) => ({ url: `${AUTH_PATH}2fa/setup`, method: 'POST', body }),
      onQueryStarted: startSession,
    }),
    /** Step 2 on later sign-ins. */
    verifyTwoFactor: build.mutation({
      query: (body) => ({ url: `${AUTH_PATH}2fa/verify`, method: 'POST', body }),
      onQueryStarted: startSession,
    }),
    /** An invited staff member chooses their password (no session; they sign in afterwards). */
    acceptInvite: build.mutation({
      query: (body) => ({ url: `${AUTH_PATH}accept-invite`, method: 'POST', body }),
    }),
    refreshSession: build.mutation({
      query: () => ({ url: `${AUTH_PATH}refresh`, method: 'POST' }),
    }),
    signOut: build.mutation({
      query: () => ({ url: `${AUTH_PATH}logout`, method: 'POST' }),
      onQueryStarted: async (_arg, { dispatch, queryFulfilled }) => {
        try {
          await queryFulfilled
        } finally {
          // Signed out locally even if the request failed.
          dispatch(signedOut())
          dispatch(baseApi.util.resetApiState())
        }
      },
    }),
  }),
})

export const {
  useSignInMutation,
  useCompleteTwoFactorSetupMutation,
  useVerifyTwoFactorMutation,
  useAcceptInviteMutation,
  useSignOutMutation,
} = authApi

/** At start-up: restores the session from the refresh cookie, once (also under StrictMode). */
export const bootstrapSession = createAsyncThunk(
  'auth/bootstrapSession',
  async (_arg, { dispatch }) => {
    const result = await dispatch(authApi.endpoints.refreshSession.initiate(undefined, { track: false }))
    dispatch(result.data ? sessionStarted(result.data) : signedOut())
  },
  { condition: (_arg, { getState }) => getState().auth.status === 'unknown' && !getState().auth.checking },
)
