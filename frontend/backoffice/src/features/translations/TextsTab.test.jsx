import { screen, waitFor, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { http, HttpResponse } from 'msw'
import i18n from '@/i18n'
import hy from '@/i18n/locales/hy/common.json'
import { problem } from '@/test/auth'
import { renderRoute } from '@/test/renderWithProviders'
import { server } from '@/test/server'
import { TEXTS, textRow, textsPage } from '@/test/translations'

const t = (key, options) => i18n.t(key, options)

function captureList(respond = () => HttpResponse.json(textsPage(TEXTS))) {
  const requests = []
  server.use(
    http.get('*/api/v1/admin/translations/:ns', ({ request, params }) => {
      requests.push({ ns: params.ns, ...Object.fromEntries(new URL(request.url).searchParams) })
      return respond()
    }),
  )
  return requests
}

function capture(method, path, respond) {
  const requests = []
  server.use(
    http[method](`*/api/v1/admin/translations/${path}`, async ({ request }) => {
      const url = new URL(request.url)
      requests.push({ url: decodeURIComponent(url.pathname), search: url.search, body: await request.json().catch(() => null) })
      return respond()
    }),
  )
  return requests
}

async function openTab(url = '/translations') {
  const result = renderRoute(url)
  await screen.findByText('Գլխավոր')
  return result
}

const dialogTitled = async (title) => (await screen.findAllByText(title)).map((element) => element.closest('[role="dialog"]')).find(Boolean)

describe('Texts tab', () => {
  it('opens on the website texts with progress per language and a column per language', async () => {
    const requests = captureList()
    const { router } = await openTab()

    expect(router.state.location.pathname).toBe('/translations/texts')
    expect(requests[0]).toEqual({ ns: 'portal', page: '1', pageSize: '50' })
    expect(screen.getByText('home.title')).toBeInTheDocument()
    expect(screen.getByText(`Русский · ${t('translations.texts.translated', { translated: 3, total: 4 })}`)).toBeInTheDocument()
    expect(screen.getByText(`Français · ${t('translations.texts.translated', { translated: 0, total: 4 })}`)).toBeInTheDocument()
    // Missing texts: English for home.search, French for both keys.
    expect(screen.getAllByText(hy.translations.texts.missing)).toHaveLength(3)
    const headers = screen.getAllByRole('columnheader').map((header) => header.textContent)
    expect(headers.slice(0, 5)).toEqual([hy.translations.texts.key, 'Հայերեն', 'Русский', 'English', 'Français'])
  })

  it('switches app and filters by text and missing language, keeping the filters in the address', async () => {
    const requests = captureList()
    const user = userEvent.setup()
    const { router, container } = await openTab()

    await user.click(within(container.querySelector('.ant-segmented')).getByText(hy.translations.namespaces.backoffice))
    await waitFor(() => expect(requests.at(-1)).toEqual({ ns: 'backoffice', page: '1', pageSize: '50' }))
    expect(await screen.findByText(hy.translations.texts.noKeys)).toBeInTheDocument()

    await user.type(screen.getByRole('searchbox', { name: hy.translations.texts.search }), 'home{Enter}')
    await waitFor(() => expect(requests.at(-1)).toEqual({ ns: 'backoffice', search: 'home', page: '1', pageSize: '50' }))

    await user.click(screen.getByRole('combobox', { name: hy.translations.texts.missingIn }))
    await user.click(await screen.findByTitle(t('translations.texts.missingInLanguage', { language: 'English' })))
    await waitFor(() => expect(requests.at(-1)).toEqual({ ns: 'backoffice', search: 'home', missingIn: 'en', page: '1', pageSize: '50' }))
    expect(router.state.location.search).toBe('?ns=backoffice&search=home&missingIn=en')
  })

  it('pages through many keys', async () => {
    const requests = captureList(() => HttpResponse.json(textsPage(TEXTS, { totalCount: 120 })))
    const user = userEvent.setup()
    const { router } = await openTab()

    await user.click(screen.getByTitle('2'))
    await waitFor(() => expect(requests.at(-1)).toEqual({ ns: 'portal', page: '2', pageSize: '50' }))
    await user.click(screen.getByTitle('1'))
    await waitFor(() => expect(router.state.location.search).toBe(''))
  })

  it('adds a key with its texts', async () => {
    const requests = capture('put', ':ns/keys/:key', () => HttpResponse.json(textRow('home.subtitle', { hy: 'Ենթավերնագիր' })))
    const user = userEvent.setup()
    await openTab()

    await user.click(screen.getByRole('button', { name: new RegExp(hy.translations.texts.add) }))
    const dialog = await dialogTitled(hy.translations.texts.add)
    await user.click(within(dialog).getByRole('button', { name: hy.catalog.form.save }))
    expect(await within(dialog).findByText(hy.translations.texts.keyRequired)).toBeInTheDocument()
    expect(within(dialog).getByText(hy.content.form.requiredInDefault)).toBeInTheDocument()

    await user.type(within(dialog).getByLabelText(hy.translations.texts.key), 'home subtitle')
    await user.click(within(dialog).getByRole('button', { name: hy.catalog.form.save }))
    expect(await within(dialog).findByText(hy.errors.key.invalid)).toBeInTheDocument()

    await user.clear(within(dialog).getByLabelText(hy.translations.texts.key))
    await user.type(within(dialog).getByLabelText(hy.translations.texts.key), 'home.subtitle')
    await user.type(within(dialog).getByLabelText(t('translations.texts.defaultText', { language: 'Հայերեն' })), 'Ենթավերնագիր')
    await user.type(within(dialog).getByLabelText('English'), '   ')
    await user.click(within(dialog).getByRole('button', { name: hy.catalog.form.save }))

    await waitFor(() =>
      expect(requests).toEqual([
        { url: '/api/v1/admin/translations/portal/keys/home.subtitle', search: '', body: { values: { hy: 'Ենթավերնագիր', ru: '', en: '', fr: '' } } },
      ]),
    )
    expect(await screen.findByText(hy.catalog.created)).toBeInTheDocument()
  })

  it('edits texts and shows server errors', async () => {
    let response = () => HttpResponse.json({ status: 400, code: 'validation_failed', errors: { values: ['values.too_long'] } }, { status: 400 })
    const requests = capture('put', ':ns/keys/:key', () => response())
    const user = userEvent.setup()
    await openTab()

    await user.click(screen.getByLabelText(`${hy.catalog.actions.edit}: home.search`))
    const dialog = await dialogTitled(hy.translations.texts.edit)
    expect(within(dialog).getByText('home.search')).toBeInTheDocument()
    await user.type(within(dialog).getByLabelText('English'), 'Search')
    await user.click(within(dialog).getByRole('button', { name: hy.catalog.form.save }))
    expect(await within(dialog).findByText(hy.errors.values.too_long)).toBeInTheDocument()

    response = () => problem(409, 'translation.key_conflict')
    await user.click(within(dialog).getByRole('button', { name: hy.catalog.form.save }))
    expect(await screen.findByText(hy.errors.translation.key_conflict)).toBeInTheDocument()

    response = () => HttpResponse.json(textRow('home.search', { hy: 'Որոնել', ru: 'Поиск', en: 'Search' }))
    await user.click(within(dialog).getByRole('button', { name: hy.catalog.form.save }))
    expect(await screen.findByText(hy.catalog.saved)).toBeInTheDocument()
    expect(requests.at(-1).body).toEqual({ values: { hy: 'Որոնել', ru: 'Поиск', en: 'Search', fr: '' } })
  })

  it.each([
    ['deletes a key in every language', () => new HttpResponse(null, { status: 204 }), hy.catalog.deleted],
    ['explains a failed delete', () => problem(404, 'translation.not_found'), hy.errors.translation.not_found],
  ])('%s', async (_, respond, expected) => {
    const requests = capture('delete', ':ns/keys/:key', respond)
    const user = userEvent.setup()
    await openTab()

    await user.click(screen.getByLabelText(`${hy.catalog.actions.delete}: home.search`))
    const confirm = await screen.findByRole('tooltip')
    expect(within(confirm).getByText(t('translations.texts.deleteConfirm', { key: 'home.search' }))).toBeInTheDocument()
    await user.click(within(confirm).getByRole('button', { name: hy.catalog.actions.delete }))

    expect(await screen.findByText(expected)).toBeInTheDocument()
    expect(requests.map((r) => r.url)).toEqual(['/api/v1/admin/translations/portal/keys/home.search'])
  })

  it('exports a language as a JSON file', async () => {
    const requests = capture('get', ':ns/export/:lng', () => new HttpResponse('{"home":{"title":"Home"}}', { headers: { 'Content-Type': 'application/json' } }))
    const createObjectURL = vi.fn(() => 'blob:texts')
    const revokeObjectURL = vi.fn()
    vi.stubGlobal('URL', Object.assign(URL, { createObjectURL, revokeObjectURL }))
    const click = vi.spyOn(HTMLAnchorElement.prototype, 'click').mockImplementation(() => {})
    const user = userEvent.setup()
    await openTab()

    await user.click(screen.getByRole('button', { name: new RegExp(hy.translations.export.button) }))
    const dialog = await dialogTitled(hy.translations.export.title)
    await user.click(within(dialog).getByRole('combobox', { name: hy.translations.transfer.language }))
    await user.click(await screen.findByTitle('English (en)'))
    await user.click(within(dialog).getByRole('checkbox', { name: hy.translations.export.withFallback }))
    await user.click(within(dialog).getByRole('button', { name: hy.translations.export.confirm }))

    await waitFor(() => expect(click).toHaveBeenCalled())
    expect(requests).toEqual([{ url: '/api/v1/admin/translations/portal/export/en', search: '?withFallback=true', body: null }])
    expect(createObjectURL).toHaveBeenCalled()
    expect(revokeObjectURL).toHaveBeenCalledWith('blob:texts')
    vi.unstubAllGlobals()
  })

  it('explains a failed export', async () => {
    capture('get', ':ns/export/:lng', () => problem(422, 'language.invalid'))
    const user = userEvent.setup()
    await openTab()

    await user.click(screen.getByRole('button', { name: new RegExp(hy.translations.export.button) }))
    const dialog = await dialogTitled(hy.translations.export.title)
    await user.click(within(dialog).getByRole('button', { name: hy.translations.export.confirm }))
    expect(await screen.findByText(hy.errors.language.invalid)).toBeInTheDocument()
  })

  it('imports a JSON file and shows what changed', async () => {
    const requests = capture('post', ':ns/import/:lng', () =>
      HttpResponse.json({ added: 2, updated: 1, unchanged: 5, removed: 0, skipped: ['old.key'], invalid: ['bad'] }),
    )
    const user = userEvent.setup()
    await openTab()

    await user.click(screen.getByRole('button', { name: new RegExp(hy.translations.import.button) }))
    const dialog = await dialogTitled(hy.translations.import.title)
    await user.click(within(dialog).getByRole('button', { name: hy.translations.import.confirm }))
    expect(await within(dialog).findByText(hy.translations.import.fileRequired)).toBeInTheDocument()

    const file = new File(['{"home":{"title":"Home"}}'], 'portal.en.json', { type: 'application/json' })
    await user.upload(dialog.querySelector('input[type="file"]'), file)
    await user.click(within(dialog).getByRole('checkbox', { name: hy.translations.import.replace }))
    await user.click(within(dialog).getByRole('button', { name: hy.translations.import.confirm }))

    expect(await within(dialog).findByText(hy.translations.import.added)).toBeInTheDocument()
    expect(requests).toEqual([{ url: '/api/v1/admin/translations/portal/import/hy', search: '?replace=true', body: { home: { title: 'Home' } } }])
    expect(within(dialog).getByText(t('translations.import.skipped', { n: 1 }))).toBeInTheDocument()
    expect(within(dialog).getByText('old.key')).toBeInTheDocument()
    expect(within(dialog).getByText(t('translations.import.invalid', { n: 1 }))).toBeInTheDocument()
    await user.click(within(dialog).getByRole('button', { name: hy.staff.inviteLink.done }))
  })

  it('refuses files that are not JSON and shows import errors', async () => {
    capture('post', ':ns/import/:lng', () => HttpResponse.json({ status: 400, code: 'validation_failed', errors: { content: ['content.invalid'] } }, { status: 400 }))
    const user = userEvent.setup()
    await openTab()

    await user.click(screen.getByRole('button', { name: new RegExp(hy.translations.import.button) }))
    const dialog = await dialogTitled(hy.translations.import.title)
    await user.upload(dialog.querySelector('input[type="file"]'), new File(['not json'], 'bad.json', { type: 'application/json' }))
    await user.click(within(dialog).getByRole('button', { name: hy.translations.import.confirm }))
    expect(await within(dialog).findByText(hy.translations.import.notJson)).toBeInTheDocument()

    await user.upload(dialog.querySelector('input[type="file"]'), new File(['[1]'], 'list.json', { type: 'application/json' }))
    await user.click(within(dialog).getByRole('button', { name: hy.translations.import.confirm }))
    expect(await within(dialog).findByText(hy.errors.generic)).toBeInTheDocument()
    await user.click(within(dialog).getByRole('button', { name: hy.catalog.form.cancel }))
  })

  it('shows load errors and an empty list', async () => {
    captureList(() => problem(500, 'unexpected'))
    renderRoute('/translations/texts')

    expect(await screen.findByText(hy.errors.generic)).toBeInTheDocument()
    expect(screen.getByText(hy.translations.texts.empty)).toBeInTheDocument()
  })
})
