import { screen, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { http, HttpResponse } from 'msw'
import en from '@/i18n/locales/en/common.json'
import ru from '@/i18n/locales/ru/common.json'
import { STORAGE_KEY } from '@/i18n/languages'
import { renderRoute } from '@/test/renderWithProviders'
import { server } from '@/test/server'

describe('LanguageSwitcher', () => {
  it('lists the active languages by their own names, with the current one selected', async () => {
    renderRoute('/ru')

    const select = await screen.findByRole('combobox', { name: ru.languageSwitcher.label })
    expect(await within(select).findAllByRole('option')).toHaveLength(3)
    expect(within(select).getByRole('option', { name: 'Հայերեն' })).toBeInTheDocument()
    expect(select).toHaveValue('ru')
  })

  it('opens the same page in the chosen language and remembers the choice', async () => {
    const user = userEvent.setup()
    const { router } = renderRoute('/ru/no-such-page?from=switcher')

    await user.selectOptions(await screen.findByRole('combobox', { name: ru.languageSwitcher.label }), 'en')

    expect(await screen.findByRole('heading', { name: en.notFound.title })).toBeInTheDocument()
    expect(router.state.location.pathname).toBe('/en/no-such-page')
    expect(router.state.location.search).toBe('?from=switcher')
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
    renderRoute('/hy')

    const select = await screen.findByRole('combobox', { name: 'Լեզու' })
    expect(await within(select).findByRole('option', { name: 'Français' })).toBeInTheDocument()
  })

  it('offers the bundled languages when the language list cannot be loaded', async () => {
    server.use(http.get('*/api/v1/languages', () => new HttpResponse(null, { status: 500 })))
    renderRoute('/en')

    const select = await screen.findByRole('combobox', { name: en.languageSwitcher.label })
    expect(within(select).getAllByRole('option').map((o) => o.textContent)).toEqual([
      'Հայերեն',
      'Русский',
      'English',
      'العربية',
      'فارسی',
      'हिन्दी',
    ])
  })
})
