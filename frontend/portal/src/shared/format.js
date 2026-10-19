import { intlLocale } from '@/i18n/languages'

/** "12 Oct 2026" in the UI language. Accepts ISO strings or Dates; empty for null. */
export function formatDate(value, lng, options = { day: 'numeric', month: 'short', year: 'numeric' }) {
  if (!value) return ''
  const date = typeof value === 'string' && /^\d{4}-\d{2}-\d{2}$/.test(value) ? new Date(`${value}T00:00:00`) : new Date(value)
  return new Intl.DateTimeFormat(intlLocale(lng), options).format(date)
}

/** "12 Oct 2026, 14:30" in the UI language. */
export const formatDateTime = (value, lng) =>
  formatDate(value, lng, { day: 'numeric', month: 'short', year: 'numeric', hour: '2-digit', minute: '2-digit' })

/** "150 000 ֏" — amounts are whole drams. */
export function formatMoney(amount, lng) {
  return `${new Intl.NumberFormat(intlLocale(lng), { maximumFractionDigits: 0 }).format(amount)} ֏`
}

/** A price per unit, which can be small: up to one decimal under 100 ֏ ("12.5 ֏"), whole drams from 100 ("1 250 ֏"). */
export function formatUnitPrice(amount, lng) {
  return `${new Intl.NumberFormat(intlLocale(lng), { maximumFractionDigits: amount < 100 ? 1 : 0 }).format(amount)} ֏`
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

/**
 * How long until a moment, in the UI language: "in 2 days", "in 5 hours", "in 20 minutes" (rounded down; days
 * once a day or more is left). Null once it has passed.
 */
export function formatTimeLeft(until, lng, now = new Date()) {
  const ms = new Date(until).getTime() - now.getTime()
  if (ms <= 0) return null
  const minutes = Math.floor(ms / 60_000)
  const hours = Math.floor(minutes / 60)
  const days = Math.floor(hours / 24)
  const format = new Intl.RelativeTimeFormat(intlLocale(lng), { numeric: 'always' })
  if (days >= 1) return format.format(days, 'day')
  if (hours >= 1) return format.format(hours, 'hour')
  return format.format(Math.max(1, minutes), 'minute')
}
