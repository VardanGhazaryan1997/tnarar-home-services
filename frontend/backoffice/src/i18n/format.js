const LOCALES = { hy: 'hy-AM', ru: 'ru-RU', en: 'en-GB' }

/** A date (and time) in the staff member's language, e.g. "5 Oct 2026, 13:00". Empty for no date. */
export function formatDate(value, language, { withTime = false } = {}) {
  if (!value) return ''
  return new Intl.DateTimeFormat(LOCALES[language] ?? language, {
    dateStyle: 'medium',
    ...(withTime ? { timeStyle: 'short' } : {}),
  }).format(new Date(value))
}

/** An amount in drams, e.g. "120,000 ֏". */
export function formatMoney(amount, language) {
  return `${new Intl.NumberFormat(LOCALES[language] ?? language).format(amount)} ֏`
}
