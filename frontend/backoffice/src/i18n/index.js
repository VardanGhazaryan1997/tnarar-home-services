import enUS from 'antd/locale/en_US'
import hyAM from 'antd/locale/hy_AM'
import ruRU from 'antd/locale/ru_RU'
import dayjs from 'dayjs'
import 'dayjs/locale/en'
import 'dayjs/locale/hy-am'
import 'dayjs/locale/ru'
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

// Ant Design ships its own texts (pagination, empty states, pickers); dates are formatted by dayjs.
const ANTD_LOCALES = { hy: hyAM, ru: ruRU, en: enUS }
const DAYJS_LOCALES = { hy: 'hy-am', ru: 'ru', en: 'en' }

export const getAntdLocale = (lng) => ANTD_LOCALES[lng] ?? ANTD_LOCALES[DEFAULT_LANGUAGE]
export const getDayjsLocale = (lng) => DAYJS_LOCALES[lng] ?? DAYJS_LOCALES[DEFAULT_LANGUAGE]

const applyLanguage = (lng) => {
  document.documentElement.lang = lng
  dayjs.locale(getDayjsLocale(lng))
}

i18n.on('languageChanged', applyLanguage)

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
    interpolation: { escapeValue: false },
    initAsync: false,
    detection: {
      // The staff member's last choice, else the browser's language.
      order: ['localStorage', 'navigator'],
      lookupLocalStorage: STORAGE_KEY,
      caches: ['localStorage'],
      convertDetectedLanguage: normalizeLanguage,
    },
    backend: {
      // Texts for languages added in the Back Office (GET /api/v1/i18n/{lng}/backoffice). Missing texts
      // come back in the default language; if the request fails the UI falls back to Armenian.
      loadPath: (lngs) => `${resolveBaseUrl(API_BASE_URL)}/i18n/${lngs[0]}/backoffice`,
    },
  })

export default i18n
