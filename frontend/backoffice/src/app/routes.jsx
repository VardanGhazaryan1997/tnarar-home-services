import { Navigate, Outlet } from 'react-router'
import GuestOnly from '@/features/auth/GuestOnly'
import RequireAuth from '@/features/auth/RequireAuth'
import RequirePermission from '@/features/auth/RequirePermission'
import SessionGate from '@/features/auth/SessionGate'
import AdminLayout from '@/layouts/AdminLayout/AdminLayout'
import NotFoundPage from '@/pages/NotFoundPage/NotFoundPage'
import { NAV_ITEMS } from './navigation'

const permissionOf = (key) => NAV_ITEMS.find((item) => item.key === key).permission

// Pages load on first visit, so each one brings only the Ant Design components it uses.
const page = (load) => async () => ({ Component: (await load()).default })

export const routes = [
  {
    element: <SessionGate />,
    // Shown while the first (lazy) page loads; SessionGate shows its own spinner right after.
    hydrateFallbackElement: null,
    children: [
      {
        element: <GuestOnly />,
        children: [
          { path: 'login', lazy: page(() => import('@/features/auth/LoginPage')) },
          { path: 'login/2fa', lazy: page(() => import('@/features/auth/TwoFactorPage')) },
        ],
      },
      // Opened from an invitation link, signed in or not.
      { path: 'accept-invite', lazy: page(() => import('@/features/auth/AcceptInvitePage')) },
      {
        element: <RequireAuth />,
        children: [
          {
            path: '/',
            element: <AdminLayout />,
            children: [
              { index: true, lazy: page(() => import('@/pages/DashboardPage/DashboardPage')) },
              {
                path: 'requests',
                element: (
                  <RequirePermission permission={permissionOf('requests')}>
                    <Outlet />
                  </RequirePermission>
                ),
                children: [
                  { index: true, lazy: page(() => import('@/features/requests/RequestsPage')) },
                  { path: ':id', lazy: page(() => import('@/features/requests/RequestDetailPage')) },
                ],
              },
              {
                path: 'orders',
                element: (
                  <RequirePermission permission={permissionOf('orders')}>
                    <Outlet />
                  </RequirePermission>
                ),
                children: [
                  { index: true, lazy: page(() => import('@/features/orders/OrdersPage')) },
                  { path: ':id', lazy: page(() => import('@/features/orders/OrderDetailPage')) },
                ],
              },
              {
                path: 'payments',
                element: (
                  <RequirePermission permission={permissionOf('payments')}>
                    <Outlet />
                  </RequirePermission>
                ),
                children: [
                  { index: true, lazy: page(() => import('@/features/payments/PaymentsPage')) },
                ],
              },
              {
                path: 'commissions',
                element: (
                  <RequirePermission permission={permissionOf('commissions')}>
                    <Outlet />
                  </RequirePermission>
                ),
                children: [
                  { index: true, element: <Navigate replace to="statements" /> },
                  {
                    lazy: page(() => import('@/features/commissions/CommissionsSection')),
                    children: [
                      { path: 'statements', lazy: page(() => import('@/features/commissions/StatementsTab')) },
                      { path: 'rates', lazy: page(() => import('@/features/commissions/RatesTab')) },
                    ],
                  },
                  { path: 'statements/:id', lazy: page(() => import('@/features/commissions/StatementDetailPage')) },
                ],
              },
              {
                path: 'reviews',
                element: (
                  <RequirePermission permission={permissionOf('reviews')}>
                    <Outlet />
                  </RequirePermission>
                ),
                children: [
                  { index: true, lazy: page(() => import('@/features/reviews/ReviewsPage')) },
                ],
              },
              {
                path: 'partners',
                element: (
                  <RequirePermission permission={permissionOf('partners')}>
                    <Outlet />
                  </RequirePermission>
                ),
                children: [
                  { index: true, lazy: page(() => import('@/features/partners/PartnersPage')) },
                  { path: ':id', lazy: page(() => import('@/features/partners/PartnerDetailPage')) },
                ],
              },
              {
                path: 'users',
                element: (
                  <RequirePermission permission={permissionOf('users')}>
                    <Outlet />
                  </RequirePermission>
                ),
                children: [
                  { index: true, lazy: page(() => import('@/features/users/UsersPage')) },
                  { path: ':id', lazy: page(() => import('@/features/users/UserDetailPage')) },
                ],
              },
              {
                path: 'catalog',
                element: (
                  <RequirePermission permission={permissionOf('catalog')}>
                    <Outlet />
                  </RequirePermission>
                ),
                children: [
                  {
                    lazy: page(() => import('@/features/catalog/CatalogPage')),
                    children: [
                      { index: true, element: <Navigate replace to="categories" /> },
                      { path: 'categories', lazy: page(() => import('@/features/catalog/CategoriesTab')) },
                      { path: 'work-items', lazy: page(() => import('@/features/catalog/WorkItemsTab')) },
                      { path: 'cities', lazy: page(() => import('@/features/catalog/CitiesTab')) },
                    ],
                  },
                ],
              },
              {
                path: 'content',
                element: (
                  <RequirePermission permission={permissionOf('content')}>
                    <Outlet />
                  </RequirePermission>
                ),
                children: [
                  { index: true, element: <Navigate replace to="pages" /> },
                  {
                    lazy: page(() => import('@/features/content/ContentSection')),
                    children: [
                      { path: 'pages', lazy: page(() => import('@/features/content/PagesTab')) },
                      { path: 'faqs', lazy: page(() => import('@/features/content/FaqsTab')) },
                    ],
                  },
                  { path: 'pages/new', lazy: page(() => import('@/features/content/PageEditorPage')) },
                  { path: 'pages/:id', lazy: page(() => import('@/features/content/PageEditorPage')) },
                ],
              },
              {
                path: 'translations',
                element: (
                  <RequirePermission permission={permissionOf('translations')}>
                    <Outlet />
                  </RequirePermission>
                ),
                children: [
                  { index: true, element: <Navigate replace to="texts" /> },
                  {
                    lazy: page(() => import('@/features/translations/TranslationsSection')),
                    children: [
                      { path: 'texts', lazy: page(() => import('@/features/translations/TextsTab')) },
                      { path: 'languages', lazy: page(() => import('@/features/translations/LanguagesTab')) },
                    ],
                  },
                ],
              },
              {
                path: 'staff',
                element: (
                  <RequirePermission permission={permissionOf('staff')}>
                    <Outlet />
                  </RequirePermission>
                ),
                children: [
                  { index: true, element: <Navigate replace to="members" /> },
                  {
                    lazy: page(() => import('@/features/staff/StaffSection')),
                    children: [
                      { path: 'members', lazy: page(() => import('@/features/staff/StaffListTab')) },
                      { path: 'roles', lazy: page(() => import('@/features/staff/RolesTab')) },
                    ],
                  },
                  { path: 'members/:id', lazy: page(() => import('@/features/staff/StaffMemberPage')) },
                ],
              },
              {
                path: 'audit',
                element: (
                  <RequirePermission permission={permissionOf('audit')}>
                    <Outlet />
                  </RequirePermission>
                ),
                children: [{ index: true, lazy: page(() => import('@/features/audit/AuditLogPage')) }],
              },
              { path: '*', element: <NotFoundPage /> },
            ],
          },
        ],
      },
    ],
  },
]
