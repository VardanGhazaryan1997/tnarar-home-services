import { formatBudget, formatDate, formatDateTime, formatMoney, todayIso } from './format'

const t = (key, values) => `${key}:${values.amount}`

describe('format', () => {
  it('formats dates, date-only values and nothing', () => {
    expect(formatDate('2026-10-07', 'en')).toBe('Oct 7, 2026')
    expect(formatDate('2026-10-07T09:00:00Z', 'en', { year: 'numeric' })).toBe('2026')
    expect(formatDate(null, 'en')).toBe('')
    expect(formatDateTime('2026-10-07T09:30:00', 'en')).toMatch(/Oct 7, 2026.*09:30/)
  })

  it('formats money and budget ranges', () => {
    expect(formatMoney(150000, 'en')).toBe('150,000 ֏')
    expect(formatBudget(10000, 30000, 'en', t)).toBe('10,000 ֏ – 30,000 ֏')
    expect(formatBudget(10000, null, 'en', t)).toBe('format.budgetFrom:10,000 ֏')
    expect(formatBudget(null, 30000, 'en', t)).toBe('format.budgetUpTo:30,000 ֏')
    expect(formatBudget(null, null, 'en', t)).toBeNull()
  })

  it('gives today as a local date', () => {
    expect(todayIso(new Date(2026, 9, 5, 23, 30))).toBe('2026-10-05')
  })
})
