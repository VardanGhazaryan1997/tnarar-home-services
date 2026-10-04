import { http, HttpResponse } from 'msw'
import { server } from './server'

export const CUSTOMER = {
  id: 'user-1',
  phoneNumber: '+37491234567',
  fullName: 'Ani Petrosyan',
  email: null,
  roles: ['Customer'],
  isProfileComplete: true,
}

export const PARTNER_USER = {
  id: 'user-2',
  phoneNumber: '+37499111222',
  fullName: 'Aram Hakobyan',
  email: null,
  roles: ['Customer', 'Partner'],
  isProfileComplete: true,
}

export const NEW_USER = { ...CUSTOMER, id: 'user-3', fullName: null, isProfileComplete: false }

export const sessionFor = (user = CUSTOMER, accessToken = `access-${user.id}`) => ({
  accessToken,
  accessTokenExpiresAt: '2026-10-05T10:00:00Z',
  user,
  isNewUser: !user.isProfileComplete,
})

/** A ProblemDetails response with a stable error code. */
export const problem = (status, code, extra = {}) => HttpResponse.json({ status, code, ...extra }, { status })

/** The browser holds a refresh cookie for this user, so pages open signed in. */
export function signedInAs(user = CUSTOMER) {
  server.use(http.post('*/api/v1/auth/refresh', () => HttpResponse.json(sessionFor(user))))
}
