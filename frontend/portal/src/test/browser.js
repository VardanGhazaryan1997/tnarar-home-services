import { STORAGE_KEY } from '@/i18n/languages'

/** Pretends the browser prefers these languages (navigator.languages). */
export function setBrowserLanguages(...languages) {
  vi.spyOn(window.navigator, 'languages', 'get').mockReturnValue(languages)
  vi.spyOn(window.navigator, 'language', 'get').mockReturnValue(languages[0])
}

/** Pretends the visitor chose this language on an earlier visit. */
export function rememberLanguage(code) {
  localStorage.setItem(STORAGE_KEY, code)
}
