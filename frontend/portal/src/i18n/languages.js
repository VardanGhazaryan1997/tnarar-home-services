/** Armenian is the default and the fallback for missing translations. */
export const DEFAULT_LANGUAGE = 'hy'

/**
 * Languages whose UI text ships with the app (src/i18n/locales). Languages added later
 * in the Back Office load their text from the API instead.
 */
export const BUNDLED_LANGUAGES = ['hy', 'ru', 'en']

/** Names of the bundled languages in their own language, for when the API is unreachable. */
export const NATIVE_NAMES = { hy: 'Հայերեն', ru: 'Русский', en: 'English' }

/** Where the visitor's last choice is remembered. */
export const STORAGE_KEY = 'hs_lang'

const LANGUAGE_CODE = /^[a-z]{2,3}$/

/** "en-US", "EN", "ru_RU" → "en", "en", "ru". */
export function normalizeLanguage(code) {
  return String(code ?? '').trim().toLowerCase().split(/[-_]/)[0]
}

/** True for strings shaped like a language code ("hy", "fil"), whether or not we support it. */
export const isLanguageCode = (value) => LANGUAGE_CODE.test(value ?? '')

/** The first candidate (in order of preference) that is available, or null. */
export function pickLanguage(candidates, available) {
  for (const candidate of candidates) {
    const code = normalizeLanguage(candidate)
    if (available.includes(code)) return code
  }
  return null
}

/** "/about" in Russian → "/ru/about"; "/" → "/ru". */
export function localizedPath(lng, path = '/') {
  const normalized = path.startsWith('/') ? path : `/${path}`
  return normalized === '/' ? `/${lng}` : `/${lng}${normalized}`
}

/** The same page in another language: "/ru/partners/42" → "/en/partners/42". */
export function switchLanguagePath(pathname, lng) {
  const [, , ...rest] = pathname.split('/')
  return localizedPath(lng, `/${rest.join('/')}`)
}
