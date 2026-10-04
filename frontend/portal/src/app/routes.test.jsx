import { screen, waitFor } from '@testing-library/react'
import { http, HttpResponse } from 'msw'
import en from '@/i18n/locales/en/common.json'
import hy from '@/i18n/locales/hy/common.json'
import ru from '@/i18n/locales/ru/common.json'
import { rememberLanguage, setBrowserLanguages } from '@/test/browser'
import { renderRoute } from '@/test/renderWithProviders'
import { ACTIVE_LANGUAGES, server } from '@/test/server'

const pathOf = (router) => router.state.location.pathname + router.state.location.search

function withFrench() {
  server.use(
    http.get('*/api/v1/languages', () =>
      HttpResponse.json([...ACTIVE_LANGUAGES, { code: 'fr', name: 'French', nativeName: 'Français', isDefault: false }]),
    ),
    http.get('*/api/v1/i18n/fr/portal', () => HttpResponse.json({ home: { title: 'Bienvenue' } })),
  )
}

describe('routes', () => {
  describe('/', () => {
    it('opens the language the visitor chose last time', async () => {
      rememberLanguage('ru')
      const { router } = renderRoute('/')

      expect(await screen.findByRole('heading', { level: 1, name: ru.home.title })).toBeInTheDocument()
      expect(pathOf(router)).toBe('/ru')
    })

    it("otherwise opens the browser's preferred supported language", async () => {
      setBrowserLanguages('de-DE', 'en-US')
      const { router } = renderRoute('/')

      expect(await screen.findByRole('heading', { level: 1, name: en.home.title })).toBeInTheDocument()
      expect(pathOf(router)).toBe('/en')
    })

    it('otherwise opens the default language', async () => {
      setBrowserLanguages('de-DE')
      const { router } = renderRoute('/')

      expect(await screen.findByRole('heading', { level: 1, name: hy.home.title })).toBeInTheDocument()
      expect(pathOf(router)).toBe('/hy')
    })

    it('opens a language added in the Back Office once the language list confirms it', async () => {
      withFrench()
      rememberLanguage('fr')
      const { router } = renderRoute('/')

      expect(await screen.findByRole('heading', { level: 1, name: 'Bienvenue' })).toBeInTheDocument()
      expect(pathOf(router)).toBe('/fr')
    })

    it('keeps the query string when redirecting', async () => {
      rememberLanguage('hy')
      const { router } = renderRoute('/?city=yerevan')

      await waitFor(() => expect(pathOf(router)).toBe('/hy?city=yerevan'))
    })
  })

  describe('/:lng', () => {
    it('shows the page in the language from the URL', async () => {
      renderRoute('/ru')

      expect(await screen.findByRole('heading', { level: 1, name: ru.home.title })).toBeInTheDocument()
      expect(document.documentElement.lang).toBe('ru')
    })

    it('renders pages inside the app layout', async () => {
      renderRoute('/hy')
      expect(await screen.findByRole('main')).toBeInTheDocument()
    })

    it('shows a language added in the Back Office with its text from the API', async () => {
      withFrench()
      renderRoute('/fr')

      expect(await screen.findByRole('heading', { level: 1, name: 'Bienvenue' })).toBeInTheDocument()
    })

    it('shows a not-found page for unknown pages, with a link home in the same language', async () => {
      renderRoute('/en/no-such-page')

      expect(await screen.findByRole('heading', { name: en.notFound.title })).toBeInTheDocument()
      expect(screen.getByRole('link', { name: en.notFound.backHome })).toHaveAttribute('href', '/en')
    })
  })

  describe('URLs without a language', () => {
    it('are redirected to the same page in the visitor language', async () => {
      rememberLanguage('ru')
      const { router } = renderRoute('/no-such-page')

      expect(await screen.findByRole('heading', { name: ru.notFound.title })).toBeInTheDocument()
      expect(pathOf(router)).toBe('/ru/no-such-page')
    })

    it('treat an inactive language code as a page path', async () => {
      rememberLanguage('hy')
      const { router } = renderRoute('/de')

      await waitFor(() => expect(pathOf(router)).toBe('/hy/de'))
      expect(await screen.findByRole('heading', { name: hy.notFound.title })).toBeInTheDocument()
    })

    it('still work when the language list cannot be loaded', async () => {
      server.use(http.get('*/api/v1/languages', () => new HttpResponse(null, { status: 500 })))
      rememberLanguage('hy')
      const { router } = renderRoute('/de')

      await waitFor(() => expect(pathOf(router)).toBe('/hy/de'))
    })
  })
})
