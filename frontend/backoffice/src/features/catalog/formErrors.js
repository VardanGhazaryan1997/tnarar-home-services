import { errorMessage, fieldErrors, problemCode } from '@/api/errors'

/**
 * Puts an API error on the form: validation errors on their fields (name errors on the
 * default-language field), "slug already used" on the slug field. Returns a message for
 * anything else, or null when it was shown on the form.
 */
export function applyApiError({ form, error, t, defaultCode }) {
  const fields = fieldErrors(t, error).map((field) =>
    field.name === 'name' ? { ...field, name: ['name', defaultCode] } : field,
  )
  if (fields.length > 0) {
    form.setFields(fields)
    return null
  }

  const code = problemCode(error)
  if (code?.endsWith('.slug_taken')) {
    form.setFields([{ name: 'slug', errors: [errorMessage(t, error)] }])
    return null
  }

  return errorMessage(t, error)
}
