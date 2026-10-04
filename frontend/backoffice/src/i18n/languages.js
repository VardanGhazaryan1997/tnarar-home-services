/** Armenian is the default and the fallback for missing translations. */
export const DEFAULT_LANGUAGE = 'hy'

/** Languages whose UI text ships with the app; others load their text from the API. */
export const BUNDLED_LANGUAGES = ['hy', 'ru', 'en']

/** Names of the bundled languages in their own language, for when the API is unreachable. */
export const NATIVE_NAMES = { hy: 'Հայերեն', ru: 'Русский', en: 'English' }

/** Where the staff member's choice is remembered (separate from the Portal's). */
export const STORAGE_KEY = 'hs_admin_lang'

/** "en-US", "EN", "ru_RU" → "en", "en", "ru". */
export function normalizeLanguage(code) {
  return String(code ?? '').trim().toLowerCase().split(/[-_]/)[0]
}
