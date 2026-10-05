import {
  directionOf,
  intlLocale,
  isLanguageCode,
  localizedPath,
  normalizeLanguage,
  pickLanguage,
  switchLanguagePath,
} from './languages'

describe('languages', () => {
  it.each([
    ['en-US', 'en'],
    ['RU', 'ru'],
    ['hy_AM', 'hy'],
    [' fr ', 'fr'],
    [undefined, ''],
  ])('normalizes %s to %s', (input, expected) => {
    expect(normalizeLanguage(input)).toBe(expected)
  })

  it.each([
    ['hy', true],
    ['fil', true],
    ['about', false],
    ['EN', false],
    ['', false],
    [undefined, false],
  ])('recognizes %s as a language code: %s', (value, expected) => {
    expect(isLanguageCode(value)).toBe(expected)
  })

  it.each([
    ['ar', 'rtl'],
    ['fa-IR', 'rtl'],
    ['hi', 'ltr'],
    ['hy', 'ltr'],
  ])('writes %s as %s', (lng, expected) => {
    expect(directionOf(lng)).toBe(expected)
  })

  it('formats Persian and Arabic dates in the Gregorian calendar with Western digits', () => {
    expect(intlLocale('fa')).toBe('fa-u-ca-gregory-nu-latn')
    expect(intlLocale('ar')).toBe('ar-u-ca-gregory-nu-latn')
    expect(intlLocale('hi')).toBe('hi')
    expect(new Intl.DateTimeFormat(intlLocale('fa'), { year: 'numeric' }).format(new Date('2026-10-06'))).toContain('2026')
  })

  it('picks the first available candidate in order of preference', () => {
    expect(pickLanguage(['de-DE', 'ru-RU', 'en'], ['hy', 'ru', 'en'])).toBe('ru')
  })

  it('picks nothing when no candidate is available', () => {
    expect(pickLanguage(['de', 'fr'], ['hy', 'ru', 'en'])).toBeNull()
  })

  it.each([
    ['/', '/ru'],
    ['/about', '/ru/about'],
    ['about', '/ru/about'],
  ])('localizes %s as %s', (path, expected) => {
    expect(localizedPath('ru', path)).toBe(expected)
  })

  it('localizes the home page by default', () => {
    expect(localizedPath('en')).toBe('/en')
  })

  it.each([
    ['/ru/partners/42', '/en/partners/42'],
    ['/ru', '/en'],
    ['/ru/', '/en'],
  ])('switches %s to %s', (pathname, expected) => {
    expect(switchLanguagePath(pathname, 'en')).toBe(expected)
  })
})
