/** The search page address for a category slug (or every service) with optional filters. */
export function searchPath(path, { category, ...filters } = {}) {
  const query = new URLSearchParams(Object.entries(filters).filter(([, value]) => value)).toString()
  const base = category ? path(`/services/${category}`) : path('/search')
  return query ? `${base}?${query}` : base
}
