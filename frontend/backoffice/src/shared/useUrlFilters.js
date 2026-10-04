import { useSearchParams } from 'react-router'

/**
 * List filters kept in the address (?status=&search=&page=), so a link shows the same list. `get(key, fallback)`
 * reads one; `update({ key: value })` changes some and goes back to page 1 (undefined or '' removes a key).
 */
export function useUrlFilters() {
  const [params, setParams] = useSearchParams()
  const get = (key, fallback) => params.get(key) ?? fallback
  const update = (changes) => {
    const next = new URLSearchParams(params)
    for (const [key, value] of Object.entries({ page: undefined, ...changes })) {
      if (value === undefined || value === '') next.delete(key)
      else next.set(key, String(value))
    }
    setParams(next)
  }
  return { get, update, page: Number(params.get('page') ?? 1) }
}

/** Query parameters without the empty ones. */
export const withoutEmpty = (params) => Object.fromEntries(Object.entries(params).filter(([, value]) => value !== undefined && value !== null && value !== ''))
