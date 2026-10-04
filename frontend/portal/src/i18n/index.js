import i18n from 'i18next'
import LanguageDetector from 'i18next-browser-languagedetector'
import HttpBackend from 'i18next-http-backend'
import { initReactI18next } from 'react-i18next'
import { API_BASE_URL, resolveBaseUrl } from '@/api/config'
import { BUNDLED_LANGUAGES, DEFAULT_LANGUAGE, normalizeLanguage, STORAGE_KEY } from './languages'
import en from './locales/en/common.json'
import hy from './locales/hy/common.json'
import ru from './locales/ru/common.json'

export { BUNDLED_LANGUAGES, DEFAULT_LANGUAGE }

i18n.on('languageChanged', (lng) => {
  document.documentElement.lang = lng
})

i18n
  .use(LanguageDetector)
  .use(HttpBackend)
  .use(initReactI18next)
  .init({
    // Bundled languages render instantly; other languages are fetched by the HTTP backend.
    resources: {
      hy: { common: hy },
      ru: { common: ru },
      en: { common: en },
    },
    partialBundledLanguages: true,
    fallbackLng: DEFAULT_LANGUAGE,
    load: 'languageOnly',
    ns: ['common'],
    defaultNS: 'common',
    interpolation: { escapeValue: false }, // React already escapes output
    initAsync: false,
    detection: {
      // The URL (/:lng/...) decides the language once the router mounts (see LanguageScope).
      // Detection picks the first render's language and where "/" redirects to.
      order: ['localStorage', 'navigator'],
      lookupLocalStorage: STORAGE_KEY,
      caches: ['localStorage'],
      convertDetectedLanguage: normalizeLanguage,
    },
    backend: {
      // Texts for languages added in the Back Office (GET /api/v1/i18n/{lng}/portal). Missing texts
      // come back in the default language; if the request fails the UI falls back to Armenian.
      loadPath: (lngs) => `${resolveBaseUrl(API_BASE_URL)}/i18n/${lngs[0]}/portal`,
    },
  })

export default i18n
