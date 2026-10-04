import { Navigate, useLocation } from 'react-router'
import { useDetectedLanguage } from './hooks'
import { localizedPath } from './languages'

/** Sends a URL without a language (e.g. "/") to the same page in the visitor's language. */
export default function LanguageRedirect() {
  const detected = useDetectedLanguage()
  const location = useLocation()

  if (!detected) return null

  return <Navigate replace to={`${localizedPath(detected, location.pathname)}${location.search}${location.hash}`} />
}
