import { render } from '@testing-library/react'
import { Provider } from 'react-redux'
import { createMemoryRouter, RouterProvider } from 'react-router'
import AppProviders from '@/app/AppProviders'
import { routes } from '@/app/routes'
import { makeStore } from '@/app/store'

/** Renders the real Back Office routes at `url` with a fresh store. */
export function renderRoute(url = '/', { preloadedState } = {}) {
  const store = makeStore(preloadedState)
  const router = createMemoryRouter(routes, { initialEntries: [url] })

  const result = render(
    <Provider store={store}>
      <AppProviders>
        <RouterProvider router={router} />
      </AppProviders>
    </Provider>,
  )

  return { ...result, store, router }
}
