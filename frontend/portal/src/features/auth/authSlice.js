import { createSlice } from '@reduxjs/toolkit'

/**
 * The Portal session. `status` is "unknown" until the refresh cookie has been checked at start-up,
 * then "authenticated" or "anonymous". The access token lives only in memory; the refresh token is
 * an httpOnly cookie the browser sends to /api/v1/auth.
 */
const initialState = {
  status: 'unknown',
  checking: false,
  accessToken: null,
  accessTokenExpiresAt: null,
  user: null,
  // Why the user was signed out (an error code), shown once on the sign-in page.
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
      state.user = payload.user
      state.notice = null
    },
    profileUpdated(state, { payload }) {
      state.user = payload
    },
    signedOut(_state, { payload }) {
      return { ...initialState, status: 'anonymous', notice: payload ?? null }
    },
    noticeShown(state) {
      state.notice = null
    },
  },
  extraReducers: (builder) => {
    // Dispatched by bootstrapSession (authApi.js); a string avoids an import cycle.
    builder.addCase('auth/bootstrapSession/pending', (state) => {
      state.checking = true
    })
  },
})

export const { sessionStarted, profileUpdated, signedOut, noticeShown } = authSlice.actions
export const authReducer = authSlice.reducer

export const selectAuthStatus = (state) => state.auth.status
export const selectUser = (state) => state.auth.user
export const selectNotice = (state) => state.auth.notice
export const selectIsPartner = (state) => Boolean(state.auth.user?.roles?.includes('Partner'))
