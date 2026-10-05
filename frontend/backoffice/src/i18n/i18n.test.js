import enUS from 'antd/locale/en_US'
import hyAM from 'antd/locale/hy_AM'
import ruRU from 'antd/locale/ru_RU'
import dayjs from 'dayjs'
import { http, HttpResponse } from 'msw'
import i18n, { BUNDLED_LANGUAGES, DEFAULT_LANGUAGE, getAntdLocale, getDayjsLocale } from '@/i18n'
import { server } from '@/test/server'
import { backOfficeLanguage, normalizeLanguage, STORAGE_KEY } from './languages'
import en from './locales/en/common.json'
import hy from './locales/hy/common.json'
import ru from './locales/ru/common.json'

const keysOf = (obj, prefix = '') =>
  Object.entries(obj).flatMap(([k, v]) =>
    typeof v === 'object' ? keysOf(v, `${prefix}${k}.`) : [`${prefix}${k}`],
  )

describe('i18n', () => {
  it('uses Armenian as the default and fallback language and bundles hy, ru, en', () => {
    expect(DEFAULT_LANGUAGE).toBe('hy')
    expect(i18n.options.fallbackLng).toEqual(['hy'])
    expect(BUNDLED_LANGUAGES).toEqual(['hy', 'ru', 'en'])
  })

  it('translates a key into the selected language', async () => {
    await i18n.changeLanguage('ru')
    expect(i18n.t('nav.dashboard')).toBe(ru.nav.dashboard)
  })

  it('loads the text of a language added in the Back Office from the API', async () => {
    server.use(
      http.get('*/api/v1/i18n/fr/backoffice', () => HttpResponse.json({ nav: { dashboard: 'Tableau de bord' } })),
    )

    await i18n.changeLanguage('fr')

    expect(i18n.t('nav.dashboard')).toBe('Tableau de bord')
    expect(i18n.t('nav.partners')).toBe(hy.nav.partners) // missing keys fall back
  })

  it('falls back to Armenian when a language has no translations yet', async () => {
    server.use(http.get('*/api/v1/i18n/de/backoffice', () => new HttpResponse(null, { status: 404 })))

    await i18n.changeLanguage('de')

    expect(i18n.t('nav.dashboard')).toBe(hy.nav.dashboard)
  })

  it('remembers the chosen language on this device', async () => {
    await i18n.changeLanguage('en')
    expect(localStorage.getItem(STORAGE_KEY)).toBe('en')
  })

  it("starts in the language chosen last time, else the browser's language", () => {
    vi.spyOn(window.navigator, 'languages', 'get').mockReturnValue(['ru-RU', 'en'])
    expect(i18n.services.languageDetector.detect()[0]).toBe('ru')

    localStorage.setItem(STORAGE_KEY, 'en')
    expect(i18n.services.languageDetector.detect()[0]).toBe('en')
  })

  it.each([
    ['ru', ru],
    ['en', en],
  ])('has every Armenian key translated in %s', (_, locale) => {
    expect(keysOf(locale).sort()).toEqual(keysOf(hy).sort())
  })

  it('updates <html lang> and the date locale when the language changes', async () => {
    await i18n.changeLanguage('ru')
    expect(document.documentElement.lang).toBe('ru')
    expect(dayjs.locale()).toBe('ru')

    await i18n.changeLanguage('hy')
    expect(dayjs.locale()).toBe('hy-am')
  })
})

describe('normalizeLanguage', () => {
  it.each([
    ['en-US', 'en'],
    ['RU', 'ru'],
    ['hy_AM', 'hy'],
    [undefined, ''],
  ])('normalizes %s to %s', (input, expected) => {
    expect(normalizeLanguage(input)).toBe(expected)
  })
})

describe('backOfficeLanguage', () => {
  it.each([
    ['ru-RU', 'ru'],
    ['ar-SA', 'en'],
    ['fa', 'en'],
    ['hi-IN', 'en'],
  ])('opens the Back Office for %s in %s', (input, expected) => {
    expect(backOfficeLanguage(input)).toBe(expected)
  })
})

describe('getAntdLocale', () => {
  it.each([
    ['hy', hyAM],
    ['ru', ruRU],
    ['en', enUS],
  ])('maps %s to the Ant Design locale', (lng, locale) => {
    expect(getAntdLocale(lng)).toBe(locale)
  })

  it('falls back to the Armenian Ant Design locale', () => {
    expect(getAntdLocale('fr')).toBe(hyAM)
  })
})

describe('getDayjsLocale', () => {
  it.each([
    ['hy', 'hy-am'],
    ['ru', 'ru'],
    ['en', 'en'],
    ['fr', 'hy-am'],
  ])('maps %s to the dayjs locale %s', (lng, locale) => {
    expect(getDayjsLocale(lng)).toBe(locale)
  })
})
