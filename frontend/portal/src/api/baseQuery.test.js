import { http, HttpResponse } from 'msw'
import { makeStore } from '@/app/store'
import { selectAuthStatus, selectNotice, sessionStarted } from '@/features/auth/authSlice'
import { problem, sessionFor } from '@/test/auth'
import { server } from '@/test/server'
import { baseApi } from './baseApi'

const secretApi = baseApi.injectEndpoints({
  endpoints: (build) => ({
    secret: build.query({ query: (id = 1) => `/secret/${id}` }),
    rawVerify: build.mutation({ query: (body) => ({ url: '/auth/otp/verify', method: 'POST', body }) }),
  }),
})

function secretEndpoint(validToken) {
  const seen = []
  server.use(
    http.get('*/api/v1/secret/:id', ({ request }) => {
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
    http.post('*/api/v1/auth/refresh', () => {
      counter.count += 1
      return respond()
    }),
  )
  return counter
}

function signedInStore(token = 'old-token') {
  const store = makeStore()
  store.dispatch(sessionStarted(sessionFor(undefined, token)))
  return store
}

describe('baseQueryWithReauth', () => {
  it('sends the access token', async () => {
    const seen = secretEndpoint('old-token')
    const result = await signedInStore().dispatch(secretApi.endpoints.secret.initiate())
    expect(seen).toEqual(['Bearer old-token'])
    expect(result.data).toEqual({ ok: true })
  })

  it('refreshes an expired session once, shared by requests that fail together, and retries', async () => {
    secretEndpoint('new-token')
    const refreshes = countRefreshes(() => HttpResponse.json(sessionFor(undefined, 'new-token')))
    const store = signedInStore()

    const results = await Promise.all([1, 2, 3].map((id) => store.dispatch(secretApi.endpoints.secret.initiate(id))))

    expect(results.every((r) => r.data?.ok)).toBe(true)
    expect(refreshes.count).toBe(1)
    expect(store.getState().auth.accessToken).toBe('new-token')
  })

  it('signs the user out when the session cannot be refreshed', async () => {
    secretEndpoint('new-token')
    countRefreshes(() => problem(401, 'session.revoked'))
    const store = signedInStore()

    await store.dispatch(secretApi.endpoints.secret.initiate())

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

  it('does not refresh for anonymous users or rejected sign-in requests', async () => {
    secretEndpoint('new-token')
    server.use(http.post('*/api/v1/auth/otp/verify', () => problem(401, 'otp.invalid')))
    const refreshes = countRefreshes(() => HttpResponse.json(sessionFor()))

    expect((await makeStore().dispatch(secretApi.endpoints.secret.initiate())).error.status).toBe(401)
    await signedInStore().dispatch(secretApi.endpoints.rawVerify.initiate({ phone: '1', code: '1' }))

    expect(refreshes.count).toBe(0)
  })
})
