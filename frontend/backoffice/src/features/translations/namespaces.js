/** The apps whose texts can be translated; the API may report more once they have texts. */
export const KNOWN_NAMESPACES = ['portal', 'backoffice']

/** Key path: letters, digits, "_" and "-", parts joined by dots, e.g. "partners.detail.back". */
export const KEY_PATTERN = /^[A-Za-z0-9_-]+(\.[A-Za-z0-9_-]+)*$/

export const LANGUAGE_CODE_PATTERN = /^[a-z]{2,3}(-[a-z0-9]{2,8})?$/
