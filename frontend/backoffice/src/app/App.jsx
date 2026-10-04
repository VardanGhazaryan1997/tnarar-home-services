import { useState } from 'react'
import { Provider } from 'react-redux'
import { createBrowserRouter, RouterProvider } from 'react-router'
import AppProviders from './AppProviders'
import { routes } from './routes'
import { makeStore } from './store'

export default function App() {
  const [store] = useState(() => makeStore())
  const [router] = useState(() => createBrowserRouter(routes))

  return (
    <Provider store={store}>
      <AppProviders>
        <RouterProvider router={router} />
      </AppProviders>
    </Provider>
  )
}
