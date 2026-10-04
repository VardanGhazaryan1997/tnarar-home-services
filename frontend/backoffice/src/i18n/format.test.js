import { formatDate } from '@/i18n/format'

describe('formatDate', () => {
  it('formats in the staff member\'s language, with the time when asked', () => {
    expect(formatDate('2026-10-05T09:30:00Z', 'en')).toMatch(/2026/)
    expect(formatDate('2026-10-05T09:30:00Z', 'en', { withTime: true }).length).toBeGreaterThan(formatDate('2026-10-05T09:30:00Z', 'en').length)
  })

  it('accepts languages it has no mapping for, and shows nothing for no date', () => {
    expect(formatDate('2026-10-05T09:30:00Z', 'fr')).toMatch(/2026/)
    expect(formatDate(null, 'en')).toBe('')
  })
})
