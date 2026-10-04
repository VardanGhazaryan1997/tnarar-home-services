import { useGetLanguagesQuery } from '@/api/languagesApi'
import { BUNDLED_LANGUAGES, DEFAULT_LANGUAGE, NATIVE_NAMES } from '@/i18n/languages'

const BUNDLED = BUNDLED_LANGUAGES.map((code) => ({ code, nativeName: NATIVE_NAMES[code], isDefault: code === DEFAULT_LANGUAGE }))

/** The languages names are entered in (active languages from the API), default language first. */
export function useCatalogLanguages() {
  const { data } = useGetLanguagesQuery()
  const languages = data?.length ? data : BUNDLED
  const defaultCode = languages.find((l) => l.isDefault)?.code ?? DEFAULT_LANGUAGE
  const ordered = [...languages].sort((a, b) => Number(b.code === defaultCode) - Number(a.code === defaultCode))
  return { languages: ordered, defaultCode }
}
