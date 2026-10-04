import { formatDate } from '@/i18n/format'

/** "5 Oct – 11 Oct 2026": the statement's week. */
export const weekText = (statement, language) => `${formatDate(statement.periodStart, language)} – ${formatDate(statement.periodEnd, language)}`

export const SETTLEMENT_METHODS = ['BankTransfer', 'Cash', 'Card', 'Other']

export const MAX_PERCENT = 50

/** Form rules for every rate: 0–50 (the input keeps it to two decimals). */
export const percentRules = (t) => [{ required: true, type: 'number', min: 0, max: MAX_PERCENT, message: t('commissions.rates.range', { max: MAX_PERCENT }) }]

/** "12.5%" in the staff member's language. */
export const formatPercent = (value, language) => `${new Intl.NumberFormat(language).format(value)}%`
