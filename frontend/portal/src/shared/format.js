/** "12 Oct 2026" in the UI language. Accepts ISO strings or Dates; empty for null. */
export function formatDate(value, lng, options = { day: 'numeric', month: 'short', year: 'numeric' }) {
  if (!value) return ''
  const date = typeof value === 'string' && /^\d{4}-\d{2}-\d{2}$/.test(value) ? new Date(`${value}T00:00:00`) : new Date(value)
  return new Intl.DateTimeFormat(lng, options).format(date)
}

/** "12 Oct 2026, 14:30" in the UI language. */
export const formatDateTime = (value, lng) =>
  formatDate(value, lng, { day: 'numeric', month: 'short', year: 'numeric', hour: '2-digit', minute: '2-digit' })

/** "150 000 ֏" — amounts are whole drams. */
export function formatMoney(amount, lng) {
  return `${new Intl.NumberFormat(lng, { maximumFractionDigits: 0 }).format(amount)} ֏`
}

/** "10 000 – 30 000 ֏", "from 10 000 ֏", "up to 30 000 ֏", or null when there's no budget. */
export function formatBudget(min, max, lng, t) {
  if (min != null && max != null) return `${formatMoney(min, lng)} – ${formatMoney(max, lng)}`
  if (min != null) return t('format.budgetFrom', { amount: formatMoney(min, lng) })
  if (max != null) return t('format.budgetUpTo', { amount: formatMoney(max, lng) })
  return null
}

/** Today as yyyy-mm-dd in the user's time zone (for date inputs). */
export function todayIso(now = new Date()) {
  const local = new Date(now.getTime() - now.getTimezoneOffset() * 60_000)
  return local.toISOString().slice(0, 10)
}
