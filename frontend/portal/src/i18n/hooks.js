import { useParams } from 'react-router'
import { useTranslation } from 'react-i18next'
import { useGetLanguagesQuery } from '@/api/languagesApi'
import { BUNDLED_LANGUAGES, DEFAULT_LANGUAGE, localizedPath, NATIVE_NAMES, normalizeLanguage } from './languages'

const BUNDLED_LIST = BUNDLED_LANGUAGES.map((code) => ({
  code,
  name: NATIVE_NAMES[code],
  nativeName: NATIVE_NAMES[code],
  isDefault: code === DEFAULT_LANGUAGE,
}))

/**
 * Languages visitors can choose: the active languages from the API, or the bundled
 * ones while it loads or if it fails. `codes` also accepts every bundled language.
 */
export function useAvailableLanguages() {
  const { data, isLoading } = useGetLanguagesQuery()
  const fromApi = data?.length ? data : null

  return {
    languages: fromApi ?? BUNDLED_LIST,
    codes: [...new Set([...BUNDLED_LANGUAGES, ...(fromApi ?? []).map((l) => l.code)])],
    defaultLanguage: fromApi?.find((l) => l.isDefault)?.code ?? DEFAULT_LANGUAGE,
    isLoading,
  }
}

/**
 * The visitor's language: their last choice, else the browser's languages, else the default.
 * Returns null while it can't tell yet (a non-bundled choice waits for the language list).
 */
export function useDetectedLanguage() {
  const { i18n } = useTranslation()
  const { codes, defaultLanguage, isLoading } = useAvailableLanguages()
  const candidates = i18n.services.languageDetector.detect(['localStorage', 'navigator']).map(normalizeLanguage)

  for (const code of candidates) {
    if (BUNDLED_LANGUAGES.includes(code)) return code
    if (isLoading) return null
    if (codes.includes(code)) return code
  }

  return isLoading ? null : defaultLanguage
}

/** Builds links that keep the current language: path("/about") → "/ru/about". Use under "/:lng". */
export function useLocalizedPath() {
  const { lng } = useParams()
  return (path = '/') => localizedPath(lng, path)
}
