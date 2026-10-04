import { createSlice } from '@reduxjs/toolkit'

/**
 * Staff session. `status` is "unknown" until the refresh cookie has been checked at start-up,
 * then "authenticated" or "anonymous". The access token lives only in memory; the refresh
 * token is an httpOnly cookie the browser sends to /api/v1/admin/auth.
 */
const initialState = {
  status: 'unknown',
  checking: false,
  accessToken: null,
  accessTokenExpiresAt: null,
  staff: null,
  // Between the password step and the authenticator step.
  challenge: null,
  // Why the staff member was sent to the sign-in page (an error code), shown there once.
  notice: null,
}

const authSlice = createSlice({
  name: 'auth',
  initialState,
  reducers: {
    sessionStarted(state, { payload }) {
      state.status = 'authenticated'
      state.checking = false
      state.accessToken = payload.accessToken
      state.accessTokenExpiresAt = payload.accessTokenExpiresAt
      state.staff = payload.staff
      state.challenge = null
      state.notice = null
    },
    signedOut(_state, { payload }) {
      return { ...initialState, status: 'anonymous', notice: payload ?? null }
    },
    challengeReceived(state, { payload }) {
      state.challenge = payload
      state.notice = null
    },
    challengeCleared(state, { payload }) {
      state.challenge = null
      state.notice = payload ?? null
    },
  },
  extraReducers: (builder) => {
    // Dispatched by bootstrapSession (authApi.js); a string avoids an import cycle.
    builder.addCase('auth/bootstrapSession/pending', (state) => {
      state.checking = true
    })
  },
})

export const { sessionStarted, signedOut, challengeReceived, challengeCleared } = authSlice.actions
export const authReducer = authSlice.reducer

export const selectAuth = (state) => state.auth
export const selectAuthStatus = (state) => state.auth.status
export const selectStaff = (state) => state.auth.staff
export const selectChallenge = (state) => state.auth.challenge
export const selectNotice = (state) => state.auth.notice
export const selectAccessToken = (state) => state.auth.accessToken
