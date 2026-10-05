import { intlLocale } from '@/i18n/languages'
import { formatDate } from '@/shared/format'

/** "5 Oct 2026 – 11 Oct 2026": a statement's week. */
export const weekText = (statement, lng) => `${formatDate(statement.periodStart, lng)} – ${formatDate(statement.periodEnd, lng)}`

/** "12.5%" in the UI language. */
export const formatPercent = (value, lng) => `${new Intl.NumberFormat(intlLocale(lng), { maximumFractionDigits: 2 }).format(value)}%`

/** Paid, overdue or still to pay: the key under commissions.status and the tag tone. */
export function statementState(statement) {
  if (statement.status === 'Paid') return { key: 'Paid', tone: 'success' }
  if (statement.overdue) return { key: 'Overdue', tone: 'danger' }
  return { key: 'Open', tone: 'info' }
}
