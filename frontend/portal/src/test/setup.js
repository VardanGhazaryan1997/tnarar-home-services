import '@testing-library/jest-dom/vitest'
import { cleanup, configure } from '@testing-library/react'
import i18n from '@/i18n'
import { server } from './server'
import { hub } from './signalr'

// findBy*/waitFor wait up to 1 s by default; lazy routes and auth bootstrap can take longer on a busy machine.
configure({ asyncUtilTimeout: 10_000 })

// jsdom doesn't scroll; the partner wizard scrolls to the top when the step changes.
window.scrollTo = () => {}

// No real chat hub in tests: see test/signalr.js.
vi.mock('@microsoft/signalr', () => import('./signalr'))

beforeAll(() => server.listen({ onUnhandledRequest: 'error' }))

afterEach(async () => {
  cleanup()
  server.resetHandlers()
  hub.connections = []
  hub.failStart = false
  vi.restoreAllMocks()
  await i18n.changeLanguage('hy')
  // changeLanguage remembers the choice; start every test without one.
  localStorage.clear()
})

afterAll(() => server.close())
