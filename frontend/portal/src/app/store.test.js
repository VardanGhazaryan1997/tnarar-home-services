import { baseApi } from '@/api/baseApi'
import { makeStore } from './store'

describe('makeStore', () => {
  it('registers the API slice', () => {
    const store = makeStore()
    expect(store.getState()).toHaveProperty(baseApi.reducerPath)
  })

  it('creates an independent store each time', () => {
    expect(makeStore()).not.toBe(makeStore())
  })
})
