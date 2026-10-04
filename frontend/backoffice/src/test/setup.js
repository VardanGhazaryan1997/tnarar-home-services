import '@testing-library/jest-dom/vitest'
import { act, cleanup, configure } from '@testing-library/react'
import i18n from '@/i18n'
import { installMatchMedia, setViewportWidth } from './matchMedia'
import { server } from './server'

installMatchMedia()

// findBy* waits up to 15 s (default 1 s): the first Ant Design render in a test file (a lazy page
// import plus styles) can take many seconds while other workers keep the CPU busy.
configure({ asyncUtilTimeout: 15000 })

if (!window.ResizeObserver) {
  window.ResizeObserver = class {
    observe() {}
    unobserve() {}
    disconnect() {}
  }
}

beforeAll(() => server.listen({ onUnhandledRequest: 'error' }))

// Tests start in Armenian, whatever language the test browser reports.
beforeEach(async () => {
  await i18n.changeLanguage('hy')
  localStorage.clear()
})

afterEach(async () => {
  cleanup()
  server.resetHandlers()
  act(() => setViewportWidth(1280))
  vi.restoreAllMocks()
})

afterAll(() => server.close())
