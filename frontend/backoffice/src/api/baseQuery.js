import { fetchBaseQuery } from '@reduxjs/toolkit/query/react'
import { sessionStarted, signedOut } from '@/features/auth/authSlice'
import i18n from '@/i18n'
import { API_BASE_URL, resolveBaseUrl } from './config'

export const AUTH_PATH = '/admin/auth/'

const rawBaseQuery = fetchBaseQuery({
  baseUrl: resolveBaseUrl(API_BASE_URL),
  // The refresh token is an httpOnly cookie; send it even when the API is on another origin.
  credentials: 'include',
  prepareHeaders: (headers, { getState }) => {
    headers.set('Accept-Language', i18n.language)
    const token = getState().auth?.accessToken
    if (token) headers.set('Authorization', `Bearer ${token}`)
    return headers
  },
})

// One refresh at a time: requests that fail together wait for the same refresh,
// because each refresh rotates the cookie and the old one stops working.
let pendingRefresh = null

function refreshOnce(api, extraOptions) {
  pendingRefresh ??= Promise.resolve(
    rawBaseQuery({ url: `${AUTH_PATH}refresh`, method: 'POST' }, api, extraOptions),
  ).finally(() => {
    pendingRefresh = null
  })
  return pendingRefresh
}

const urlOf = (args) => (typeof args === 'string' ? args : args.url)

/**
 * Sends the access token with every request. When it has expired (401), refreshes the
 * session once and retries; if that fails the staff member is signed out.
 */
export async function baseQueryWithReauth(args, api, extraOptions) {
  let result = await rawBaseQuery(args, api, extraOptions)

  const expired = result.error?.status === 401 && !urlOf(args).startsWith(AUTH_PATH)
  if (!expired || api.getState().auth?.status !== 'authenticated') return result

  const refreshed = await refreshOnce(api, extraOptions)
  if (refreshed.data) {
    api.dispatch(sessionStarted(refreshed.data))
    result = await rawBaseQuery(args, api, extraOptions)
  } else {
    api.dispatch(signedOut(refreshed.error?.data?.code ?? 'session.expired'))
  }

  return result
}
