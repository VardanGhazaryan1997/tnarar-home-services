import { configureStore } from '@reduxjs/toolkit'
import { baseApi } from '@/api/baseApi'
import { authReducer } from '@/features/auth/authSlice'

export function makeStore(preloadedState) {
  return configureStore({
    reducer: {
      auth: authReducer,
      [baseApi.reducerPath]: baseApi.reducer,
    },
    middleware: (getDefaultMiddleware) => getDefaultMiddleware().concat(baseApi.middleware),
    preloadedState,
  })
}
