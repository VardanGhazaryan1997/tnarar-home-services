/**
 * The main sections. Desktop shows them in the header (the logo leads home); phones in the bottom tab bar
 * (5 slots, so partners reach their inbox from the Requests section).
 * `auth`: signed-in users only; `anonymous`: visitors only; `mobileOnly` / `desktopOnly`: one of the two bars;
 * `anonymousTab`: in the tab bar only for visitors (signed-in users have no free slot).
 */
export const NAV_ITEMS = [
  { key: 'home', path: '/', icon: 'home', end: true, mobileOnly: true },
  { key: 'services', path: '/services', icon: 'search', anonymousTab: true },
  { key: 'estimates', path: '/estimates', icon: 'money', desktopOnly: true },
  { key: 'how', path: '/how-it-works', icon: 'info', anonymous: true, desktopOnly: true },
  { key: 'requests', path: '/requests', icon: 'requests', auth: true },
  { key: 'inbox', path: '/inbox', icon: 'inbox', auth: true, partner: true, desktopOnly: true },
  { key: 'orders', path: '/orders', icon: 'orders', auth: true },
  { key: 'messages', path: '/messages', icon: 'messages', auth: true, badge: 'unread' },
  { key: 'account', path: '/account', icon: 'account', auth: true, mobileOnly: true },
]

/** The items a visitor sees: anonymous visitors get the public pages (plus Sign in). */
export function navItemsFor(user) {
  const isPartner = Boolean(user?.roles?.includes('Partner'))
  return NAV_ITEMS.filter((item) => (!item.auth || user) && (!item.anonymous || !user) && (!item.partner || isPartner))
}

/** Header links. */
export const desktopItems = (user) => navItemsFor(user).filter((item) => !item.mobileOnly)

/** Tab bar links. */
export const mobileItems = (user) => navItemsFor(user).filter((item) => !item.desktopOnly && (!item.anonymousTab || !user))
