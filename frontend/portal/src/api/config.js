export const API_BASE_URL = import.meta.env.VITE_API_URL ?? '/api/v1'

// fetch needs an absolute URL outside the browser (tests, SSR), so resolve
// a relative base URL against the current origin.
export const resolveBaseUrl = (url) => new URL(url, window.location.origin).href
