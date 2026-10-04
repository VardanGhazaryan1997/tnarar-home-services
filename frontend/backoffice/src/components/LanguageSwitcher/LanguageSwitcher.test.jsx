import { screen, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { http, HttpResponse } from 'msw'
import en from '@/i18n/locales/en/common.json'
import hy from '@/i18n/locales/hy/common.json'
import { STORAGE_KEY } from '@/i18n/languages'
import { renderRoute } from '@/test/renderWithProviders'
import { server } from '@/test/server'

async function openSwitcher(user, label = hy.languageSwitcher.label) {
  const select = await screen.findByRole('combobox', { name: label })
  await user.click(select)
  return screen.findByRole('listbox')
}

describe('LanguageSwitcher', () => {
  it('shows the current language in the header', async () => {
    renderRoute('/')

    const banner = await screen.findByRole('banner')
    expect(within(banner).getByRole('combobox', { name: hy.languageSwitcher.label })).toBeInTheDocument()
    expect(await within(banner).findByText('Հայերեն')).toBeInTheDocument()
  })

  it('switches the whole Back Office to the chosen language and remembers it', async () => {
    const user = userEvent.setup()
    renderRoute('/')
    await openSwitcher(user)

    await user.click(await screen.findByRole('option', { name: 'English' }))

    expect(await screen.findByRole('menuitem', { name: new RegExp(en.nav.dashboard) })).toBeInTheDocument()
    expect(document.documentElement.lang).toBe('en')
    expect(localStorage.getItem(STORAGE_KEY)).toBe('en')
  })

  it('includes languages added in the Back Office', async () => {
    server.use(
      http.get('*/api/v1/languages', () =>
        HttpResponse.json([
          { code: 'hy', name: 'Armenian', nativeName: 'Հայերեն', isDefault: true },
          { code: 'fr', name: 'French', nativeName: 'Français', isDefault: false },
        ]),
      ),
    )
    const user = userEvent.setup()
    renderRoute('/')

    await openSwitcher(user)

    expect(await screen.findByRole('option', { name: 'Français' })).toBeInTheDocument()
  })

  it('offers the bundled languages when the language list cannot be loaded', async () => {
    server.use(http.get('*/api/v1/languages', () => new HttpResponse(null, { status: 500 })))
    const user = userEvent.setup()
    renderRoute('/')

    await openSwitcher(user)

    for (const name of ['Հայերեն', 'Русский', 'English']) {
      expect(await screen.findByRole('option', { name })).toBeInTheDocument()
    }
  })
})
