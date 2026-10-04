import { cleanNames, localizedName, slugify } from './names'

describe('localizedName', () => {
  const name = { hy: 'Սանտեխնիկա', en: 'Plumbing' }

  it('uses the requested language', () => {
    expect(localizedName(name, 'en', 'hy')).toBe('Plumbing')
  })

  it('falls back to the default language, then any translation', () => {
    expect(localizedName(name, 'ru', 'hy')).toBe('Սանտեխնիկա')
    expect(localizedName({ fr: 'Plomberie' }, 'ru', 'hy')).toBe('Plomberie')
  })

  it('is empty without a name', () => {
    expect(localizedName(undefined, 'en', 'hy')).toBe('')
    expect(localizedName({}, 'en', 'hy')).toBe('')
  })
})

describe('slugify', () => {
  it.each([
    ['Exterior cladding', 'exterior-cladding'],
    ['  Roof & Gutter repair! ', 'roof-gutter-repair'],
    ['Ремонт', ''],
    [undefined, ''],
  ])('turns %s into %s', (text, slug) => {
    expect(slugify(text)).toBe(slug)
  })

  it('keeps slugs within 64 characters', () => {
    expect(slugify('a'.repeat(80))).toHaveLength(64)
  })
})

describe('cleanNames', () => {
  it('trims translations and drops blank ones', () => {
    expect(cleanNames({ hy: ' Ա ', ru: '  ', en: undefined, fr: 'B' })).toEqual({ hy: 'Ա', fr: 'B' })
  })

  it('accepts no names', () => {
    expect(cleanNames()).toEqual({})
  })
})
