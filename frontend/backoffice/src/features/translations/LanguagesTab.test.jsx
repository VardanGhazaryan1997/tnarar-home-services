import { screen, waitFor, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { http, HttpResponse } from 'msw'
import i18n from '@/i18n'
import hy from '@/i18n/locales/hy/common.json'
import { problem, signedInAs, staffMember } from '@/test/auth'
import { renderRoute } from '@/test/renderWithProviders'
import { server } from '@/test/server'
import { ADMIN_LANGUAGES } from '@/test/translations'

const t = (key, options) => i18n.t(key, options)

function capture(method, path, respond = () => HttpResponse.json(ADMIN_LANGUAGES[0])) {
  const requests = []
  server.use(
    http[method](`*/api/v1/admin/${path}`, async ({ request }) => {
      requests.push({ url: new URL(request.url).pathname, body: await request.json().catch(() => null) })
      return respond()
    }),
  )
  return requests
}

async function openTab() {
  const result = renderRoute('/translations/languages')
  await screen.findByText('Français')
  return result
}

const dialogTitled = async (title) => (await screen.findAllByText(title)).map((element) => element.closest('[role="dialog"]')).find(Boolean)

describe('Languages tab', () => {
  it('lists every language with its code, default flag and visibility', async () => {
    await openTab()

    expect(screen.getByRole('heading', { level: 1, name: hy.nav.translations })).toBeInTheDocument()
    expect(screen.getByText('fr')).toBeInTheDocument()
    expect(screen.getByText(hy.translations.languages.default)).toBeInTheDocument()
    expect(screen.getByText(hy.translations.languages.inactive)).toBeInTheDocument()
    expect(screen.getAllByText(hy.translations.languages.active)).toHaveLength(3)
    // The default language can't be hidden.
    expect(screen.queryByLabelText(`${hy.translations.languages.hide}: Հայերեն`)).not.toBeInTheDocument()
  })

  it('adds a language, which starts hidden', async () => {
    const requests = capture('post', 'languages', () => HttpResponse.json({ ...ADMIN_LANGUAGES[3], code: 'de' }, { status: 201 }))
    const user = userEvent.setup()
    await openTab()

    await user.click(screen.getByRole('button', { name: new RegExp(hy.translations.languages.add) }))
    const dialog = await dialogTitled(hy.translations.languages.add)
    await user.click(within(dialog).getByRole('button', { name: hy.catalog.form.save }))
    expect(await within(dialog).findByText(hy.translations.languages.codeRequired)).toBeInTheDocument()

    await user.type(within(dialog).getByLabelText(hy.translations.languages.code), 'German')
    await user.click(within(dialog).getByRole('button', { name: hy.catalog.form.save }))
    expect(await within(dialog).findByText(hy.errors.code.invalid)).toBeInTheDocument()

    await user.clear(within(dialog).getByLabelText(hy.translations.languages.code))
    await user.type(within(dialog).getByLabelText(hy.translations.languages.code), 'de')
    await user.type(within(dialog).getByLabelText(hy.translations.languages.name), ' German ')
    await user.type(within(dialog).getByLabelText(hy.translations.languages.nativeName), 'Deutsch')
    await user.click(within(dialog).getByRole('button', { name: hy.catalog.form.save }))

    await waitFor(() => expect(requests).toEqual([{ url: '/api/v1/admin/languages', body: { code: 'de', name: 'German', nativeName: 'Deutsch', sortOrder: 0 } }]))
    expect(await screen.findByText(hy.translations.languages.added)).toBeInTheDocument()
  })

  it('shows "already exists" on the code and other errors as a message', async () => {
    let response = () => problem(409, 'language.code_taken')
    capture('post', 'languages', () => response())
    const user = userEvent.setup()
    await openTab()

    await user.click(screen.getByRole('button', { name: new RegExp(hy.translations.languages.add) }))
    const dialog = await dialogTitled(hy.translations.languages.add)
    await user.type(within(dialog).getByLabelText(hy.translations.languages.code), 'fr')
    await user.type(within(dialog).getByLabelText(hy.translations.languages.name), 'French')
    await user.type(within(dialog).getByLabelText(hy.translations.languages.nativeName), 'Français')
    await user.click(within(dialog).getByRole('button', { name: hy.catalog.form.save }))
    expect(await within(dialog).findByText(hy.errors.language.code_taken)).toBeInTheDocument()

    response = () => HttpResponse.json({ status: 400, code: 'validation_failed', errors: { nativeName: ['native_name.too_long'] } }, { status: 400 })
    await user.click(within(dialog).getByRole('button', { name: hy.catalog.form.save }))
    expect(await within(dialog).findByText(hy.errors.native_name.too_long)).toBeInTheDocument()

    response = () => problem(500, 'unexpected')
    await user.click(within(dialog).getByRole('button', { name: hy.catalog.form.save }))
    expect(await screen.findByText(hy.errors.generic)).toBeInTheDocument()
  })

  it('edits a language without changing its code', async () => {
    const requests = capture('put', 'languages/:code')
    const user = userEvent.setup()
    await openTab()

    await user.click(screen.getByLabelText(`${hy.catalog.actions.edit}: Русский`))
    const dialog = await dialogTitled(t('translations.languages.edit', { name: 'Русский' }))
    expect(within(dialog).queryByLabelText(hy.translations.languages.code)).not.toBeInTheDocument()
    const name = within(dialog).getByLabelText(hy.translations.languages.nativeName)
    await user.clear(name)
    await user.type(name, 'Русский язык')
    await user.click(within(dialog).getByRole('button', { name: hy.catalog.form.save }))

    await waitFor(() => expect(requests).toEqual([{ url: '/api/v1/admin/languages/ru', body: { name: 'Russian', nativeName: 'Русский язык', sortOrder: 2 } }]))
    expect(await screen.findByText(hy.catalog.saved)).toBeInTheDocument()
  })

  it('shows a hidden language and hides a shown one after confirmation', async () => {
    const requests = capture('post', 'languages/:code/:action')
    const user = userEvent.setup()
    await openTab()

    await user.click(screen.getByLabelText(`${hy.translations.languages.show}: Français`))
    await waitFor(() => expect(requests.map((r) => r.url)).toEqual(['/api/v1/admin/languages/fr/activate']))
    expect(await screen.findByText(hy.translations.languages.shown)).toBeInTheDocument()

    await user.click(screen.getByLabelText(`${hy.translations.languages.hide}: English`))
    const confirm = (await screen.findAllByRole('tooltip')).find((tooltip) => within(tooltip).queryByText(t('translations.languages.hideConfirm', { name: 'English' })))
    await user.click(within(confirm).getByRole('button', { name: hy.translations.languages.hide }))
    await waitFor(() => expect(requests.map((r) => r.url)).toEqual(['/api/v1/admin/languages/fr/activate', '/api/v1/admin/languages/en/deactivate']))
  })

  it('explains a refused change', async () => {
    capture('post', 'languages/:code/:action', () => problem(422, 'language.default_cannot_be_deactivated'))
    const user = userEvent.setup()
    await openTab()

    await user.click(screen.getByLabelText(`${hy.translations.languages.show}: Français`))
    expect(await screen.findByText(hy.errors.language.default_cannot_be_deactivated)).toBeInTheDocument()
  })

  it('shows load errors', async () => {
    server.use(http.get('*/api/v1/admin/languages', () => problem(500, 'unexpected')))
    renderRoute('/translations/languages')
    expect(await screen.findByText(hy.errors.generic)).toBeInTheDocument()
  })

  it('is closed to staff without translations.manage', async () => {
    signedInAs(staffMember(['content.manage']))
    renderRoute('/translations')
    expect(await screen.findByText(hy.forbidden.title)).toBeInTheDocument()
  })
})
