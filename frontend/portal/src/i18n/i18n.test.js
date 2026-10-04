import { http, HttpResponse } from 'msw'
import i18n, { BUNDLED_LANGUAGES, DEFAULT_LANGUAGE } from '@/i18n'
import { server } from '@/test/server'
import { STORAGE_KEY } from './languages'
import en from './locales/en/common.json'
import hy from './locales/hy/common.json'
import ru from './locales/ru/common.json'

const keysOf = (obj, prefix = '') =>
  Object.entries(obj).flatMap(([k, v]) =>
    typeof v === 'object' ? keysOf(v, `${prefix}${k}.`) : [`${prefix}${k}`],
  )

describe('i18n', () => {
  it('uses Armenian as the default and fallback language', () => {
    expect(DEFAULT_LANGUAGE).toBe('hy')
    expect(i18n.options.fallbackLng).toEqual(['hy'])
  })

  it('bundles Armenian, Russian and English', () => {
    expect(BUNDLED_LANGUAGES).toEqual(['hy', 'ru', 'en'])
  })

  it('translates a key into the selected language', async () => {
    await i18n.changeLanguage('ru')
    expect(i18n.t('common:home.title')).toBe(ru.home.title)

    await i18n.changeLanguage('en')
    expect(i18n.t('common:home.title')).toBe(en.home.title)
  })

  it('loads the text of a language added in the Back Office from the API', async () => {
    server.use(
      http.get('*/api/v1/i18n/fr/portal', () =>
        HttpResponse.json({ home: { title: 'Trouvez des artisans de confiance' } }),
      ),
    )

    await i18n.changeLanguage('fr')

    expect(i18n.t('common:home.title')).toBe('Trouvez des artisans de confiance')
    expect(i18n.t('common:notFound.title')).toBe(hy.notFound.title) // missing keys fall back
  })

  it('falls back to Armenian when a language has no translations yet', async () => {
    server.use(http.get('*/api/v1/i18n/de/portal', () => new HttpResponse(null, { status: 404 })))

    await i18n.changeLanguage('de')

    expect(i18n.t('common:home.title')).toBe(hy.home.title)
  })

  it('remembers the chosen language for the next visit', async () => {
    await i18n.changeLanguage('ru')
    expect(localStorage.getItem(STORAGE_KEY)).toBe('ru')
  })

  it.each([
    ['ru', ru],
    ['en', en],
  ])('has every Armenian key translated in %s', (_, locale) => {
    expect(keysOf(locale).sort()).toEqual(keysOf(hy).sort())
  })
})

describe('document language', () => {
  it('updates <html lang> when the language changes', async () => {
    await i18n.changeLanguage('ru')
    expect(document.documentElement.lang).toBe('ru')
  })
})
