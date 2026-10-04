import i18n from '@/i18n'
import en from '@/i18n/locales/en/common.json'
import { errorMessage, fieldErrors, problemCode } from './errors'

const t = i18n.getFixedT('en')

describe('API errors', () => {
  it('reads the ProblemDetails code', () => {
    expect(problemCode({ status: 401, data: { code: 'staff.invalid_credentials' } })).toBe('staff.invalid_credentials')
  })

  it('reports network failures as "network"', () => {
    expect(problemCode({ status: 'FETCH_ERROR', error: 'TypeError' })).toBe('network')
  })

  it('has no code for other failures', () => {
    expect(problemCode({ status: 500, data: 'oops' })).toBeNull()
    expect(problemCode(undefined)).toBeNull()
  })

  it('translates known codes and falls back to a generic message', () => {
    expect(errorMessage(t, { status: 401, data: { code: 'staff.invalid_credentials' } })).toBe(
      en.errors.staff.invalid_credentials,
    )
    expect(errorMessage(t, { status: 418, data: { code: 'teapot.brewing' } })).toBe(en.errors.generic)
    expect(errorMessage(t, { status: 500 })).toBe(en.errors.generic)
  })

  it('turns validation errors into form field errors', () => {
    const error = {
      status: 400,
      data: { code: 'validation_failed', errors: { email: ['email.required'], password: ['password.required'] } },
    }

    expect(fieldErrors(t, error)).toEqual([
      { name: 'email', errors: [en.errors.email.required] },
      { name: 'password', errors: [en.errors.password.required] },
    ])
  })

  it('has no field errors for other failures', () => {
    expect(fieldErrors(t, { status: 401, data: { code: 'staff.invalid_credentials' } })).toEqual([])
  })
})
