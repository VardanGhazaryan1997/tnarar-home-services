import { configureStore } from '@reduxjs/toolkit'
import { baseApi } from '@/api/baseApi'
import { authReducer } from '@/features/auth/authSlice'

export function makeStore(preloadedState) {
  return configureStore({
    reducer: {
      auth: authReducer,
      [baseApi.reducerPath]: baseApi.reducer,
    },
    // The development-only state checks warn when they take over 32 ms, which large API caches (and slow test
    // machines) reach without anything being wrong; they still run, just quietly up to 200 ms.
    middleware: (getDefaultMiddleware) =>
      getDefaultMiddleware({ immutableCheck: { warnAfter: 200 }, serializableCheck: { warnAfter: 200 } }).concat(baseApi.middleware),
    preloadedState,
  })
}
