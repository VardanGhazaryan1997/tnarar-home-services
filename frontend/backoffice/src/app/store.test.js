import { baseApi } from '@/api/baseApi'
import { makeStore } from './store'

describe('makeStore', () => {
  it('registers the API slice', () => {
    expect(makeStore().getState()).toHaveProperty(baseApi.reducerPath)
  })

  it('accepts preloaded state', () => {
    const store = makeStore({ [baseApi.reducerPath]: undefined })
    expect(store.getState()).toHaveProperty(baseApi.reducerPath)
  })
})
