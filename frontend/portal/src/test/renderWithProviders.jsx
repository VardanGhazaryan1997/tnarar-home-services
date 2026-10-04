import { render } from '@testing-library/react'
import { I18nextProvider } from 'react-i18next'
import { Provider } from 'react-redux'
import { createMemoryRouter, RouterProvider } from 'react-router'
import { routes } from '@/app/routes'
import { makeStore } from '@/app/store'
import i18n from '@/i18n'

/**
 * Renders the app's real routes at the given URL with a fresh store.
 * Returns the RTL result plus the store and router for assertions.
 */
export function renderRoute(url = '/', { preloadedState } = {}) {
  const store = makeStore(preloadedState)
  const router = createMemoryRouter(routes, { initialEntries: [url] })

  const result = render(
    <Provider store={store}>
      <I18nextProvider i18n={i18n}>
        <RouterProvider router={router} />
      </I18nextProvider>
    </Provider>,
  )

  return { ...result, store, router }
}
