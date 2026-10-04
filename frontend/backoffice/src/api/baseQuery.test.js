import { http, HttpResponse } from 'msw'
import { makeStore } from '@/app/store'
import { selectAccessToken, selectAuthStatus, selectNotice, sessionStarted } from '@/features/auth/authSlice'
import { problem, sessionFor } from '@/test/auth'
import { server } from '@/test/server'
import { baseApi } from './baseApi'

const secretApi = baseApi.injectEndpoints({
  endpoints: (build) => ({
    secret: build.query({ query: (id = 1) => `/admin/secret/${id}` }),
  }),
})

/** /admin/secret answers only to `validToken`. */
function secretEndpoint(validToken) {
  const seen = []
  server.use(
    http.get('*/api/v1/admin/secret/:id', ({ request }) => {
      const auth = request.headers.get('Authorization')
      seen.push(auth)
      return auth === `Bearer ${validToken}` ? HttpResponse.json({ ok: true }) : problem(401, 'unauthorized')
    }),
  )
  return seen
}

function countRefreshes(respond) {
  const counter = { count: 0 }
  server.use(
    http.post('*/api/v1/admin/auth/refresh', async () => {
      counter.count += 1
      return respond()
    }),
  )
  return counter
}

function signedInStore(token = 'old-token') {
  const store = makeStore()
  store.dispatch(sessionStarted({ ...sessionFor(), accessToken: token }))
  return store
}

describe('baseQueryWithReauth', () => {
  it('sends the access token', async () => {
    const seen = secretEndpoint('old-token')

    const result = await signedInStore().dispatch(secretApi.endpoints.secret.initiate())

    expect(seen).toEqual(['Bearer old-token'])
    expect(result.data).toEqual({ ok: true })
  })

  it('refreshes an expired session once and retries the request', async () => {
    const seen = secretEndpoint('new-token')
    const refreshes = countRefreshes(() => HttpResponse.json({ ...sessionFor(), accessToken: 'new-token' }))
    const store = signedInStore()

    const result = await store.dispatch(secretApi.endpoints.secret.initiate())

    expect(result.data).toEqual({ ok: true })
    expect(seen).toEqual(['Bearer old-token', 'Bearer new-token'])
    expect(refreshes.count).toBe(1)
    expect(selectAccessToken(store.getState())).toBe('new-token')
  })

  it('shares one refresh between requests that fail together', async () => {
    secretEndpoint('new-token')
    const refreshes = countRefreshes(() => HttpResponse.json({ ...sessionFor(), accessToken: 'new-token' }))
    const store = signedInStore()

    const results = await Promise.all([1, 2, 3].map((id) => store.dispatch(secretApi.endpoints.secret.initiate(id))))

    expect(results.every((r) => r.data?.ok)).toBe(true)
    expect(refreshes.count).toBe(1)
  })

  it('signs the staff member out when the session cannot be refreshed', async () => {
    secretEndpoint('new-token')
    countRefreshes(() => problem(401, 'session.revoked'))
    const store = signedInStore()

    const result = await store.dispatch(secretApi.endpoints.secret.initiate())

    expect(result.error.status).toBe(401)
    expect(selectAuthStatus(store.getState())).toBe('anonymous')
    expect(selectNotice(store.getState())).toBe('session.revoked')
  })

  it('reports an expired session when the refresh fails without a code', async () => {
    secretEndpoint('new-token')
    countRefreshes(() => new HttpResponse(null, { status: 500 }))
    const store = signedInStore()

    await store.dispatch(secretApi.endpoints.secret.initiate())

    expect(selectNotice(store.getState())).toBe('session.expired')
  })

  it('does not try to refresh when nobody is signed in', async () => {
    secretEndpoint('new-token')
    const refreshes = countRefreshes(() => HttpResponse.json(sessionFor()))

    const result = await makeStore().dispatch(secretApi.endpoints.secret.initiate())

    expect(result.error.status).toBe(401)
    expect(refreshes.count).toBe(0)
  })

  it('does not refresh when a sign-in request itself is rejected', async () => {
    server.use(http.post('*/api/v1/admin/auth/login', () => problem(401, 'staff.invalid_credentials')))
    const refreshes = countRefreshes(() => HttpResponse.json(sessionFor()))
    const loginApi = baseApi.injectEndpoints({
      endpoints: (build) => ({
        rawLogin: build.mutation({ query: (body) => ({ url: '/admin/auth/login', method: 'POST', body }) }),
      }),
    })

    await signedInStore().dispatch(loginApi.endpoints.rawLogin.initiate({ email: 'a@b.c', password: 'x' }))

    expect(refreshes.count).toBe(0)
  })
})
