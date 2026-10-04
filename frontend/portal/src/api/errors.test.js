import i18n from '@/i18n'
import { errorMessage, fieldErrors, problemCode } from './errors'

const t = i18n.getFixedT('en')

describe('errors', () => {
  it('reads the problem code, or network for fetch errors', () => {
    expect(problemCode({ data: { code: 'otp.invalid' } })).toBe('otp.invalid')
    expect(problemCode({ status: 'FETCH_ERROR' })).toBe('network')
    expect(problemCode({ status: 500 })).toBeNull()
  })

  it('translates known codes and falls back to a generic message', () => {
    expect(errorMessage(t, { data: { code: 'otp.invalid' } })).toBe(i18n.getResource('en', 'common', 'errors.otp.invalid'))
    expect(errorMessage(t, { data: { code: 'never.heard_of' } })).toBe(i18n.getResource('en', 'common', 'errors.generic'))
    expect(errorMessage(t, { status: 500 })).toBe(i18n.getResource('en', 'common', 'errors.generic'))
  })

  it('maps validation errors to one message per field', () => {
    expect(fieldErrors(t, { status: 400, data: { errors: { fullName: ['name.required'] } } })).toEqual({
      fullName: i18n.getResource('en', 'common', 'errors.name.required'),
    })
    expect(fieldErrors(t, { status: 422, data: { code: 'x' } })).toEqual({})
  })
})
