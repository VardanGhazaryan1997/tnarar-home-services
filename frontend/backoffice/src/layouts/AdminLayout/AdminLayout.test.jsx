import { act, screen, waitFor, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import hyAM from 'antd/locale/hy_AM'
import en from '@/i18n/locales/en/common.json'
import hy from '@/i18n/locales/hy/common.json'
import i18n from '@/i18n'
import { setViewportWidth } from '@/test/matchMedia'
import { renderRoute } from '@/test/renderWithProviders'

describe('AdminLayout on desktop', () => {
  it('shows the navigation menu in a side bar', async () => {
    renderRoute('/')
    const nav = await screen.findByRole('navigation', { name: hy.nav.label })
    for (const label of [hy.nav.dashboard, hy.nav.partners, hy.nav.users, hy.nav.catalog, hy.nav.content, hy.nav.translations, hy.nav.staff, hy.nav.audit]) {
      expect(within(nav).getByRole('menuitem', { name: new RegExp(label) })).toBeInTheDocument()
    }
    expect(screen.queryByRole('button', { name: hy.nav.open })).not.toBeInTheDocument()
  })

  it('shows the app name in the header', async () => {
    renderRoute('/')
    expect(await screen.findByRole('banner')).toHaveTextContent(hy.app.name)
  })

  it('navigates when a menu item is clicked and highlights it', async () => {
    const user = userEvent.setup()
    const { router } = renderRoute('/')

    await user.click(await screen.findByRole('menuitem', { name: new RegExp(hy.nav.partners) }))

    await waitFor(() => expect(router.state.location.pathname).toBe('/partners'))
    expect(await screen.findByRole('heading', { name: hy.nav.partners })).toBeInTheDocument()
    expect(screen.getByRole('menuitem', { name: new RegExp(hy.nav.partners) })).toHaveClass('ant-menu-item-selected')
  })

  it('renders menu labels in the selected language', async () => {
    await i18n.changeLanguage('en')
    renderRoute('/')
    expect(await screen.findByRole('menuitem', { name: new RegExp(en.nav.staff) })).toBeInTheDocument()
  })
})

describe('AdminLayout on a phone', () => {
  beforeEach(() => act(() => setViewportWidth(375)))

  it('hides the side bar and shows a menu button instead', async () => {
    renderRoute('/')
    expect(await screen.findByRole('button', { name: hy.nav.open })).toBeInTheDocument()
    expect(screen.queryByRole('navigation', { name: hy.nav.label })).not.toBeInTheDocument()
  })

  it('opens the menu in a drawer and closes it after navigating', async () => {
    const user = userEvent.setup()
    const { router } = renderRoute('/')

    await user.click(await screen.findByRole('button', { name: hy.nav.open }))
    const drawerNav = await screen.findByRole('navigation', { name: hy.nav.label })
    await user.click(within(drawerNav).getByRole('menuitem', { name: new RegExp(hy.nav.catalog) }))

    await waitFor(() => expect(router.state.location.pathname).toBe('/catalog/categories'))
    await vi.waitFor(() => expect(screen.queryByRole('navigation', { name: hy.nav.label })).not.toBeInTheDocument())
  })

  it('closes the drawer with its close button', async () => {
    const user = userEvent.setup()
    renderRoute('/')

    await user.click(await screen.findByRole('button', { name: hy.nav.open }))
    await screen.findByRole('navigation', { name: hy.nav.label })
    await user.click(screen.getByRole('button', { name: hyAM.global.close }))

    await vi.waitFor(() => expect(screen.queryByRole('navigation', { name: hy.nav.label })).not.toBeInTheDocument())
  })
})
