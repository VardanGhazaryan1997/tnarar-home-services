import { http, HttpResponse } from 'msw'
import { makeStore } from '@/app/store'
import i18n from '@/i18n'
import { server } from '@/test/server'
import { API_BASE_URL, baseApi } from './baseApi'

const pingApi = baseApi.injectEndpoints({
  endpoints: (build) => ({ ping: build.query({ query: () => '/ping' }) }),
})

function captureRequests() {
  const requests = []
  server.use(
    http.get('*/ping', ({ request }) => {
      requests.push(request)
      return HttpResponse.json({ ok: true })
    }),
  )
  return requests
}

describe('baseApi', () => {
  it('calls the API under /api/v1 by default', async () => {
    const requests = captureRequests()
    const store = makeStore()

    const result = await store.dispatch(pingApi.endpoints.ping.initiate())

    expect(API_BASE_URL).toBe('/api/v1')
    expect(new URL(requests[0].url).pathname).toBe('/api/v1/ping')
    expect(result.data).toEqual({ ok: true })
  })

  it('sends the current UI language in Accept-Language', async () => {
    const requests = captureRequests()
    await i18n.changeLanguage('ru')

    await makeStore().dispatch(pingApi.endpoints.ping.initiate())

    expect(requests[0].headers.get('Accept-Language')).toBe('ru')
  })
})
