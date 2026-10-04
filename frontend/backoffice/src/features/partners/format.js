const LOCALES = { hy: 'hy-AM', ru: 'ru-RU', en: 'en-GB' }

/** A date (and time) in the staff member's language, e.g. "5 Oct 2026, 13:00". Empty for no date. */
export function formatDate(value, language, { withTime = false } = {}) {
  if (!value) return ''
  return new Intl.DateTimeFormat(LOCALES[language] ?? language, {
    dateStyle: 'medium',
    ...(withTime ? { timeStyle: 'short' } : {}),
  }).format(new Date(value))
}
