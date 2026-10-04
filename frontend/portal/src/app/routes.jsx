import RequireAuth from '@/features/auth/RequireAuth'
import RequirePartner from '@/features/auth/RequirePartner'
import LanguageRedirect from '@/i18n/LanguageRedirect'
import LanguageScope from '@/i18n/LanguageScope'
import AppLayout from '@/layouts/AppLayout/AppLayout'
import HomePage from '@/pages/HomePage/HomePage'
import NotFoundPage from '@/pages/NotFoundPage/NotFoundPage'

/** Feature pages load on first visit, so the first page a visitor opens stays small. */
const page = (load) => async () => ({ Component: (await load()).default })

// Every page lives under its language: /hy, /ru/..., /en/...
export const routes = [
  { path: '/', element: <LanguageRedirect /> },
  {
    path: ':lng',
    element: <LanguageScope />,
    hydrateFallbackElement: null,
    children: [
      {
        element: <AppLayout />,
        children: [
          { index: true, element: <HomePage /> },
          { path: 'sign-in', lazy: page(() => import('@/features/auth/SignInPage')) },
          { path: 'services', lazy: page(() => import('@/features/public/ServicesPage')) },
          { path: 'services/:category', lazy: page(() => import('@/features/public/SearchPage')) },
          { path: 'search', lazy: page(() => import('@/features/public/SearchPage')) },
          { path: 'partners/:slug', lazy: page(() => import('@/features/public/PartnerPage')) },
          { path: 'how-it-works', lazy: page(() => import('@/features/public/HowItWorksPage')) },
          { path: 'pages/:slug', lazy: page(() => import('@/features/public/InfoPage')) },
          {
            element: <RequireAuth />,
            children: [
              { path: 'account', lazy: page(() => import('@/features/account/AccountPage')) },
              { path: 'partner', lazy: page(() => import('@/features/partner/PartnerProfilePage')) },
              { path: 'requests', lazy: page(() => import('@/features/requests/MyRequestsPage')) },
              { path: 'requests/new', lazy: page(() => import('@/features/requests/NewRequestPage')) },
              { path: 'requests/:id', lazy: page(() => import('@/features/requests/MyRequestPage')) },
              { path: 'orders', lazy: page(() => import('@/features/orders/OrdersPage')) },
              { path: 'orders/:id', lazy: page(() => import('@/features/orders/OrderPage')) },
              { path: 'messages', lazy: page(() => import('@/features/chat/MessagesPage')) },
              { path: 'messages/:id', lazy: page(() => import('@/features/chat/MessagesPage')) },
              { path: 'notifications', lazy: page(() => import('@/features/notifications/NotificationsPage')) },
              {
                element: <RequirePartner />,
                children: [
                  { path: 'inbox', lazy: page(() => import('@/features/requests/InboxPage')) },
                  { path: 'inbox/:id', lazy: page(() => import('@/features/requests/InboxRequestPage')) },
                  { path: 'inbox/:id/offer', lazy: page(() => import('@/features/offers/OfferBuilderPage')) },
                  { path: 'offers', lazy: page(() => import('@/features/offers/MyOffersPage')) },
                  { path: 'commissions', lazy: page(() => import('@/features/commissions/CommissionsPage')) },
                  { path: 'commissions/:id', lazy: page(() => import('@/features/commissions/StatementPage')) },
                ],
              },
            ],
          },
          { path: '*', element: <NotFoundPage /> },
        ],
      },
    ],
  },
]
