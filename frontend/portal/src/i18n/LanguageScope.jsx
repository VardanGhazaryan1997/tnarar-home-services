import { Suspense, useLayoutEffect } from 'react'
import { useTranslation } from 'react-i18next'
import { Outlet, useParams } from 'react-router'
import { useAvailableLanguages } from './hooks'
import LanguageRedirect from './LanguageRedirect'
import { isLanguageCode } from './languages'

/**
 * Route element for "/:lng". A known language becomes the UI language; anything else
 * is treated as a page path without a language and redirected ("/about" → "/hy/about").
 */
export default function LanguageScope() {
  const { lng } = useParams()
  const { i18n } = useTranslation()
  const { codes, isLoading } = useAvailableLanguages()
  const known = codes.includes(lng)

  useLayoutEffect(() => {
    if (known && i18n.language !== lng) i18n.changeLanguage(lng)
  }, [known, lng, i18n])

  // A code we don't bundle may still be an active language: wait for the list.
  if (!known && isLoading && isLanguageCode(lng)) return null
  if (!known) return <LanguageRedirect />

  // Non-bundled languages load their text first.
  return (
    <Suspense fallback={null}>
      <Outlet />
    </Suspense>
  )
}
