import { screen, within } from '@testing-library/react'
import en from '@/i18n/locales/en/common.json'
import { CUSTOMER, PARTNER_USER, signedInAs } from '@/test/auth'
import { renderRoute } from '@/test/renderWithProviders'
import { http, HttpResponse } from 'msw'
import { server } from '@/test/server'
import { desktopItems, mobileItems, navItemsFor } from './navigation'

describe('AppLayout', () => {
  it('offers visitors the home page and signing in', async () => {
    renderRoute('/en')

    const header = screen.getByRole('banner')
    expect(await within(header).findByRole('link', { name: en.nav.signIn })).toHaveAttribute('href', '/en/sign-in')
    const tabs = screen.getByRole('navigation', { name: en.layout.tabNavigation })
    expect(within(tabs).getAllByRole('link').map((l) => l.textContent)).toEqual([en.nav.tab.home, en.nav.tab.services, en.nav.signIn])
    const main = within(header).getByRole('navigation', { name: en.layout.mainNavigation })
    expect(within(main).getAllByRole('link').map((l) => l.textContent)).toEqual([en.nav.services, en.nav.estimates, en.nav.how])
    expect(screen.getByRole('link', { name: en.layout.skipToContent })).toHaveAttribute('href', '#main')
    expect(screen.getByRole('contentinfo')).toHaveTextContent('Tnarar')
  })

  it('shows signed-in users their sections and name', async () => {
    signedInAs(CUSTOMER)
    renderRoute('/en')

    const header = screen.getByRole('banner')
    expect(await within(header).findByRole('link', { name: CUSTOMER.fullName })).toHaveAttribute('href', '/en/account')
    const main = within(header).getByRole('navigation', { name: en.layout.mainNavigation })
    expect(within(main).getAllByRole('link').map((l) => l.textContent)).toEqual([en.nav.services, en.nav.estimates, en.nav.requests, en.nav.orders, en.nav.messages])
    expect(within(header).getByRole('link', { name: en.layout.home })).toHaveAttribute('href', '/en')
    const tabs = screen.getByRole('navigation', { name: en.layout.tabNavigation })
    expect(within(tabs).getAllByRole('link').map((l) => l.textContent)).toEqual([en.nav.tab.home, en.nav.tab.requests, en.nav.tab.orders, en.nav.tab.messages, en.nav.tab.account])
  })

  it('adds the inbox for partners and keeps five tabs for signed-in users', () => {
    expect(desktopItems(PARTNER_USER).map((i) => i.key)).toEqual(['services', 'estimates', 'requests', 'inbox', 'orders', 'messages'])
    expect(mobileItems(PARTNER_USER).map((i) => i.key)).toEqual(['home', 'requests', 'orders', 'messages', 'account'])
    expect(navItemsFor({ ...PARTNER_USER, fullName: null }).some((i) => i.key === 'inbox')).toBe(true)
    expect(navItemsFor(null).map((i) => i.key)).toEqual(['home', 'services', 'estimates', 'how'])
  })

  it('links the public pages and the information pages marked for the footer', async () => {
    server.use(
      http.get('*/api/v1/pages', () =>
        HttpResponse.json([
          { slug: 'terms', title: 'Terms of use', showInFooter: true },
          { slug: 'hidden', title: 'Not in footer', showInFooter: false },
        ]),
      ),
    )
    renderRoute('/en')

    const footer = screen.getByRole('contentinfo')
    expect(await within(footer).findByRole('link', { name: 'Terms of use' })).toHaveAttribute('href', '/en/pages/terms')
    expect(within(footer).queryByRole('link', { name: 'Not in footer' })).not.toBeInTheDocument()
    expect(within(footer).getByRole('link', { name: en.layout.links.partners })).toHaveAttribute('href', '/en/how-it-works?for=partners')
  })
})
