import { errorMessage, fieldErrors, problemCode } from '@/api/errors'

/**
 * Puts an API error on the form: validation errors on their fields (name errors on the
 * default-language field), "slug already used" on the slug field. Returns a message for
 * anything else, or null when it was shown on the form. `fieldNames` moves errors of API fields the form shows under
 * another name, e.g. { price: 'priceTypical' }.
 */
export function applyApiError({ form, error, t, defaultCode, fieldNames = {} }) {
  const fields = fieldErrors(t, error).map((field) => {
    if (field.name === 'name') return { ...field, name: ['name', defaultCode] }
    return fieldNames[field.name] ? { ...field, name: fieldNames[field.name] } : field
  })
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
