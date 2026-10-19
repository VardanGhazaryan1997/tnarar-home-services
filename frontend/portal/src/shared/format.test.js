import { formatBudget, formatDate, formatDateTime, formatMoney, formatTimeLeft, todayIso } from './format'

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

describe('formatUnitPrice', () => {
  it('keeps one decimal under 100 ֏ and whole drams from there', async () => {
    const { formatUnitPrice } = await import('@/shared/format')
    expect(formatUnitPrice(12.5, 'en')).toBe('12.5 ֏')
    expect(formatUnitPrice(12, 'en')).toBe('12 ֏')
    expect(formatUnitPrice(1250.4, 'en')).toBe('1,250 ֏')
  })
  it('says how long is left in the UI language, rounded down, and nothing once it has passed', () => {
    const now = new Date('2026-10-05T12:00:00Z')
    expect(formatTimeLeft('2026-10-08T13:00:00Z', 'en', now)).toBe('in 3 days')
    expect(formatTimeLeft('2026-10-05T17:30:00Z', 'en', now)).toBe('in 5 hours')
    expect(formatTimeLeft('2026-10-05T12:20:30Z', 'en', now)).toBe('in 20 minutes')
    expect(formatTimeLeft('2026-10-05T12:00:10Z', 'en', now)).toBe('in 1 minute')
    expect(formatTimeLeft('2026-10-08T13:00:00Z', 'ru', now)).toBe('через 3 дня')
    expect(formatTimeLeft('2026-10-05T12:00:00Z', 'en', now)).toBeNull()
  })
})
