/** The stable error code of a failed request: the API's ProblemDetails `code`, or "network". */
export function problemCode(error) {
  if (error?.data?.code) return error.data.code
  if (error?.status === 'FETCH_ERROR') return 'network'
  return null
}

const translate = (t, code) => t(`errors.${code}`, { defaultValue: t('errors.generic') })

/** A message for the user, translated from the error code. */
export function errorMessage(t, error) {
  const code = problemCode(error)
  return code ? translate(t, code) : t('errors.generic')
}

/** Validation errors (400) as { field: message }, one message per field. */
export function fieldErrors(t, error) {
  if (error?.status !== 400 || !error.data?.errors) return {}
  return Object.fromEntries(Object.entries(error.data.errors).map(([name, codes]) => [name, translate(t, codes[0])]))
}
