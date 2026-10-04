import { http, HttpResponse } from 'msw'
import { setupServer } from 'msw/node'
import { sessionFor } from './auth'
import { CATEGORIES, CITIES, REGIONS } from './catalog'
import { AUDIT_ENTRIES, auditPage } from './audit'
import { FAQS, PAGES } from './content'
import { ADMIN_LANGUAGES, NAMESPACES, TEXTS, textsPage } from './translations'
import { userDetail, userRow, usersPage } from './users'
import { PERMISSIONS, ROLES, staffDetail, staffPage, staffRow } from './staff'
import { page, partnerDetail, partnerRow, publicCategories, publicCities, publicRegions } from './partners'
import { commissionRates, orderDetail, orderRow, paymentRow, requestDetail, requestRow, reviewRow, statementDetail, statementRow } from './operations'

export const ACTIVE_LANGUAGES = [
  { code: 'hy', name: 'Armenian', nativeName: 'Հայերեն', isDefault: true },
  { code: 'ru', name: 'Russian', nativeName: 'Русский', isDefault: false },
  { code: 'en', name: 'English', nativeName: 'English', isDefault: false },
]

// Default handlers shared by all tests. Individual tests add their own with server.use(...).
// By default the browser holds a Super Admin's refresh cookie, so pages open signed in.
export const handlers = [
  http.get('*/api/v1/languages', () => HttpResponse.json(ACTIVE_LANGUAGES)),
  http.post('*/api/v1/admin/auth/refresh', () => HttpResponse.json(sessionFor())),
  http.get('*/api/v1/admin/catalog/categories', () => HttpResponse.json(CATEGORIES)),
  http.get('*/api/v1/admin/catalog/cities', () => HttpResponse.json(CITIES)),
  http.get('*/api/v1/admin/catalog/regions', () => HttpResponse.json(REGIONS)),
  http.get('*/api/v1/categories', ({ request }) => HttpResponse.json(publicCategories(request.headers.get('Accept-Language') ?? 'hy'))),
  http.get('*/api/v1/cities', ({ request }) => HttpResponse.json(publicCities(request.headers.get('Accept-Language') ?? 'hy'))),
  http.get('*/api/v1/regions', ({ request }) => HttpResponse.json(publicRegions(request.headers.get('Accept-Language') ?? 'hy'))),
  http.get('*/api/v1/admin/partners', () => HttpResponse.json(page([partnerRow()]))),
  http.get('*/api/v1/admin/partners/:id', () => HttpResponse.json(partnerDetail())),
  http.get('*/api/v1/admin/audit-log', () => HttpResponse.json(auditPage(AUDIT_ENTRIES))),
  http.get('*/api/v1/admin/pages', () => HttpResponse.json(PAGES)),
  http.get('*/api/v1/admin/pages/:id', ({ params }) => HttpResponse.json(PAGES.find((p) => p.id === params.id) ?? PAGES[0])),
  http.get('*/api/v1/admin/faqs', () => HttpResponse.json(FAQS)),
  http.get('*/api/v1/admin/languages', () => HttpResponse.json(ADMIN_LANGUAGES)),
  http.get('*/api/v1/admin/translations', () => HttpResponse.json(NAMESPACES)),
  http.get('*/api/v1/admin/translations/:ns', () => HttpResponse.json(textsPage(TEXTS))),
  http.get('*/api/v1/admin/users', () => HttpResponse.json(usersPage([userRow()]))),
  http.get('*/api/v1/admin/users/:id', () => HttpResponse.json(userDetail())),
  http.get('*/api/v1/admin/staff', () => HttpResponse.json(staffPage([staffRow()]))),
  http.get('*/api/v1/admin/staff/:id', () => HttpResponse.json(staffDetail())),
  http.get('*/api/v1/admin/roles', () => HttpResponse.json(ROLES)),
  http.get('*/api/v1/admin/permissions', () => HttpResponse.json(PERMISSIONS)),
  http.get('*/api/v1/admin/requests', () => HttpResponse.json(page([requestRow()]))),
  http.get('*/api/v1/admin/requests/:id', () => HttpResponse.json(requestDetail())),
  http.get('*/api/v1/admin/orders', () => HttpResponse.json(page([orderRow()]))),
  http.get('*/api/v1/admin/orders/:id', () => HttpResponse.json(orderDetail())),
  http.get('*/api/v1/admin/payments', () => HttpResponse.json(page([paymentRow()]))),
  http.get('*/api/v1/admin/reviews', () => HttpResponse.json(page([reviewRow()]))),
  http.get('*/api/v1/admin/commission-rates', () => HttpResponse.json(commissionRates())),
  http.get('*/api/v1/admin/commission-statements', () => HttpResponse.json(page([statementRow()]))),
  http.get('*/api/v1/admin/commission-statements/:id', () => HttpResponse.json(statementDetail())),
]

export const server = setupServer(...handlers)
