import { errorMessage, fieldErrors, problemCode } from '@/api/errors'

/**
 * Puts an API error on a content form: errors on per-language fields (`localized`, e.g. ["title", "body"])
 * go on the default-language input, "address already used" on the slug. Returns a message for anything else,
 * or null when it was shown on the form.
 */
export function applyContentError({ form, error, t, defaultCode, localized }) {
  const fields = fieldErrors(t, error).map((field) => (localized.includes(field.name) ? { ...field, name: [field.name, defaultCode] } : field))
  if (problemCode(error) === 'page.slug_taken') fields.push({ name: 'slug', errors: [errorMessage(t, error)] })
  if (fields.length > 0) {
    form.setFields(fields)
    return null
  }
  return errorMessage(t, error)
}
