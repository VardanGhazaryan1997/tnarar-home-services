import { createAsyncThunk } from '@reduxjs/toolkit'
import { baseApi } from '@/api/baseApi'
import { AUTH_PATH } from '@/api/baseQuery'
import { profileUpdated, sessionStarted, signedOut } from './authSlice'

export const authApi = baseApi.injectEndpoints({
  endpoints: (build) => ({
    /** Texts a sign-in code. Returns { expiresInSeconds, resendAfterSeconds }. */
    sendCode: build.mutation({
      query: (phone) => ({ url: `${AUTH_PATH}otp/send`, method: 'POST', body: { phone } }),
    }),
    /** Checks the code and starts the session (creating the account on first sign-in). */
    verifyCode: build.mutation({
      query: (body) => ({ url: `${AUTH_PATH}otp/verify`, method: 'POST', body }),
      onQueryStarted: async (_arg, { dispatch, queryFulfilled }) => {
        try {
          const { data } = await queryFulfilled
          dispatch(sessionStarted(data))
        } catch {
          // The page shows the error.
        }
      },
    }),
    refreshSession: build.mutation({
      query: () => ({ url: `${AUTH_PATH}refresh`, method: 'POST' }),
    }),
    signOut: build.mutation({
      query: () => ({ url: `${AUTH_PATH}logout`, method: 'POST' }),
      onQueryStarted: async (_arg, { dispatch, queryFulfilled }) => {
        try {
          await queryFulfilled
        } catch {
          // Signed out locally even if the request failed.
        } finally {
          dispatch(signedOut())
          dispatch(baseApi.util.resetApiState())
        }
      },
    }),
    /** Name (required) and email (optional). */
    updateProfile: build.mutation({
      query: (body) => ({ url: '/me', method: 'PUT', body }),
      onQueryStarted: async (_arg, { dispatch, queryFulfilled }) => {
        try {
          const { data } = await queryFulfilled
          dispatch(profileUpdated(data))
        } catch {
          // The form shows the error.
        }
      },
    }),
  }),
})

export const { useSendCodeMutation, useVerifyCodeMutation, useSignOutMutation, useUpdateProfileMutation } = authApi

/** At start-up: restores the session from the refresh cookie, once (also under StrictMode). */
export const bootstrapSession = createAsyncThunk(
  'auth/bootstrapSession',
  async (_arg, { dispatch }) => {
    const result = await dispatch(authApi.endpoints.refreshSession.initiate(undefined, { track: false }))
    dispatch(result.data ? sessionStarted(result.data) : signedOut())
  },
  { condition: (_arg, { getState }) => getState().auth.status === 'unknown' && !getState().auth.checking },
)
