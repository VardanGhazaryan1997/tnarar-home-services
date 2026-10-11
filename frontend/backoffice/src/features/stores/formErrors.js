import { errorMessage, fieldErrors, problemCode } from '@/api/errors'

/**
 * Puts an API error on the form: validation errors on their fields (`fieldNames` moves errors of API fields the form
 * shows under another name), "slug or code already used" on that field. Returns a message for anything else, or null
 * when it was shown on the form.
 */
export function applyStoreError({ form, error, t, fieldNames = {} }) {
  const fields = fieldErrors(t, error).map((field) => ({ ...field, name: fieldNames[field.name] ?? field.name }))
  if (fields.length > 0) {
    form.setFields(fields)
    return null
  }

  const code = problemCode(error)
  if (code?.endsWith('.slug_taken')) {
    form.setFields([{ name: 'slug', errors: [errorMessage(t, error)] }])
    return null
  }
  if (code === 'store_product.code_taken') {
    form.setFields([{ name: 'code', errors: [errorMessage(t, error)] }])
    return null
  }

  return errorMessage(t, error)
}
