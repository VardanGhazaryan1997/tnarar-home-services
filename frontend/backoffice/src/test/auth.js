import { http, HttpResponse } from 'msw'
import { server } from './server'

export const SUPER_ADMIN = {
  id: '019a0000-0000-7000-8000-000000000001',
  email: 'admin@homeservices.local',
  fullName: 'Ani Admin',
  isSuperAdmin: true,
  permissions: [],
}

export const staffMember = (permissions = [], overrides = {}) => ({
  id: '019a0000-0000-7000-8000-000000000002',
  email: 'operator@homeservices.local',
  fullName: 'Aram Operator',
  isSuperAdmin: false,
  permissions,
  ...overrides,
})

let tokenCounter = 0

export const sessionFor = (staff = SUPER_ADMIN) => ({
  accessToken: `access-${++tokenCounter}`,
  accessTokenExpiresAt: '2026-10-05T09:15:00Z',
  staff,
})

export const problem = (status, code) => HttpResponse.json({ status, code, title: code }, { status })

/** The browser has a valid refresh cookie for `staff` (the default for every test is a Super Admin). */
export function signedInAs(staff) {
  server.use(http.post('*/api/v1/admin/auth/refresh', () => HttpResponse.json(sessionFor(staff))))
}

/** The browser has no (valid) refresh cookie. */
export function signedOutBrowser() {
  server.use(http.post('*/api/v1/admin/auth/refresh', () => problem(401, 'session.missing')))
}
