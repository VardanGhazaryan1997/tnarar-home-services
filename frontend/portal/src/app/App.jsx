import { useState } from 'react'
import { I18nextProvider } from 'react-i18next'
import { Provider } from 'react-redux'
import { createBrowserRouter, RouterProvider } from 'react-router'
import i18n from '@/i18n'
import { routes } from './routes'
import { makeStore } from './store'

export default function App() {
  const [store] = useState(() => makeStore())
  const [router] = useState(() => createBrowserRouter(routes))

  return (
    <Provider store={store}>
      <I18nextProvider i18n={i18n}>
        <RouterProvider router={router} />
      </I18nextProvider>
    </Provider>
  )
}
