/** The name in `lng`, else in the default language, else any translation. */
export function localizedName(name, lng, defaultLng) {
  if (!name) return ''
  return name[lng] || name[defaultLng] || Object.values(name).find(Boolean) || ''
}

/** "Exterior cladding" → "exterior-cladding" (latin letters and digits only). */
export function slugify(text) {
  return String(text ?? '')
    .toLowerCase()
    .replace(/[^a-z0-9]+/g, '-')
    .replace(/^-+|-+$/g, '')
    .slice(0, 64)
}

/** Drops blank translations and trims the rest: { hy: " Ա ", ru: "" } → { hy: "Ա" }. */
export function cleanNames(name = {}) {
  return Object.fromEntries(
    Object.entries(name)
      .map(([code, text]) => [code, (text ?? '').trim()])
      .filter(([, text]) => text.length > 0),
  )
}
