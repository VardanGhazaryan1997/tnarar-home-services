import { http, HttpResponse } from 'msw'
import { setupServer } from 'msw/node'
import { CATEGORIES, CITIES, page } from './fixtures'

export const ACTIVE_LANGUAGES = [
  { code: 'hy', name: 'Armenian', nativeName: 'Հայերեն', isDefault: true },
  { code: 'ru', name: 'Russian', nativeName: 'Русский', isDefault: false },
  { code: 'en', name: 'English', nativeName: 'English', isDefault: false },
]

// Default handlers shared by all tests. Individual tests add their own with server.use(...).
// By default nobody is signed in (no refresh cookie); use signedInAs() from test/auth.js.
export const handlers = [
  http.get('*/api/v1/languages', () => HttpResponse.json(ACTIVE_LANGUAGES)),
  http.get('*/api/v1/categories', () => HttpResponse.json(CATEGORIES)),
  http.get('*/api/v1/cities', () => HttpResponse.json(CITIES)),
  http.get('*/api/v1/partners', () => HttpResponse.json(page([]))),
  http.get('*/api/v1/pages', () => HttpResponse.json([])),
  http.get('*/api/v1/faqs', () => HttpResponse.json([])),
  http.get('*/api/v1/conversations/unread', () => HttpResponse.json({ conversations: 0, messages: 0 })),
  http.get('*/api/v1/notifications/unread-count', () => HttpResponse.json({ count: 0 })),
  http.get('*/api/v1/partners/:slug/reviews', () => HttpResponse.json(page([]))),
  http.get('*/api/v1/me/commissions/summary', () =>
    HttpResponse.json({ unbilled: 0, outstanding: 0, overdue: 0, nextDueOn: null, pausedSince: null, pauseAfterOverdueDays: 14 }),
  ),
  // Commission lists a page may still be loading when a test ends (after its own handlers are reset).
  http.get('*/api/v1/me/commissions', () => HttpResponse.json(page([]))),
  http.get('*/api/v1/me/commission-statements', () => HttpResponse.json(page([]))),
  http.get('*/api/v1/me/commission-statements/:id', () => HttpResponse.json({ status: 404, code: 'commission.statement_not_found' }, { status: 404 })),
  http.post('*/api/v1/auth/refresh', () => HttpResponse.json({ status: 401, code: 'session.missing' }, { status: 401 })),
]

export const server = setupServer(...handlers)
