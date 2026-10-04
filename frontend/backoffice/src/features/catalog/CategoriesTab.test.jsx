import { screen, waitFor, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { http, HttpResponse } from 'msw'
import i18n from '@/i18n'
import hy from '@/i18n/locales/hy/common.json'
import { problem } from '@/test/auth'
import { renderRoute } from '@/test/renderWithProviders'
import { server } from '@/test/server'

const nameLabel = (language) => i18n.t('catalog.form.nameIn', { language })

function capture(method, path, respond = (body) => HttpResponse.json({ id: 'new-id', ...body })) {
  const requests = []
  server.use(
    http[method](`*/api/v1/admin/catalog/${path}`, async ({ request }) => {
      const body = request.method === 'POST' || request.method === 'PUT' ? await request.json().catch(() => null) : null
      requests.push({ url: new URL(request.url).pathname, body })
      return respond(body)
    }),
  )
  return requests
}

async function openTab() {
  renderRoute('/catalog/categories')
  return screen.findByRole('table')
}

/** The open dialog, after checking its title. */
async function dialog(title) {
  const element = await screen.findByRole('dialog')
  expect(within(element).getByText(title)).toBeInTheDocument()
  return element
}

// Buttons with an icon include the icon's label in their accessible name.
const buttonNamed = (text) => new RegExp(text.replace(/[.*+?^${}()|[\]\\]/g, '\\$&'))

describe('Categories tab', () => {
  it('opens from /catalog and shows the category tree with status', async () => {
    const { router } = renderRoute('/catalog')

    const table = await screen.findByRole('table')
    expect(router.state.location.pathname).toBe('/catalog/categories')
    expect(await within(table).findByText('Վերանորոգում')).toBeInTheDocument()
    expect(within(table).getByText('Սալիկապատում')).toBeInTheDocument()
    expect(within(table).getByText('Սանտեխնիկա')).toBeInTheDocument()
    expect(within(table).getAllByText(hy.catalog.status.hidden)).toHaveLength(1)
  })

  it('shows names in the selected UI language', async () => {
    await i18n.changeLanguage('en')
    const table = await openTab()

    expect(await within(table).findByText('Renovation')).toBeInTheDocument()
  })

  it('adds a category, suggesting the slug from the English name', async () => {
    const requests = capture('post', 'categories')
    const user = userEvent.setup()
    await openTab()

    await user.click(screen.getByRole('button', { name: buttonNamed(hy.catalog.categories.add) }))
    const form = await dialog(hy.catalog.categories.add)
    await user.type(within(form).getByLabelText(nameLabel('Հայերեն')), 'Տանիքի նորոգում')
    await user.type(within(form).getByLabelText(nameLabel('English')), 'Roof repair')
    expect(within(form).getByLabelText(hy.catalog.form.slug)).toHaveValue('roof-repair')
    await user.click(within(form).getByRole('button', { name: hy.catalog.form.save }))

    await waitFor(() => expect(requests).toHaveLength(1))
    expect(requests[0].body).toEqual({
      slug: 'roof-repair',
      name: { hy: 'Տանիքի նորոգում', en: 'Roof repair' },
      icon: null,
      parentId: null,
      sortOrder: 0,
    })
    expect(await screen.findByText(hy.catalog.created)).toBeInTheDocument()
  })

  it('requires the default-language name and a valid slug', async () => {
    const user = userEvent.setup()
    await openTab()

    await user.click(screen.getByRole('button', { name: buttonNamed(hy.catalog.categories.add) }))
    const form = await dialog(hy.catalog.categories.add)
    await user.type(within(form).getByLabelText(hy.catalog.form.slug), 'Bad Slug')
    await user.click(within(form).getByRole('button', { name: hy.catalog.form.save }))

    expect(await within(form).findByText(hy.catalog.form.nameRequired)).toBeInTheDocument()
    expect(within(form).getByText(hy.errors.slug.invalid)).toBeInTheDocument()
  })

  it('shows a taken slug on the slug field', async () => {
    capture('post', 'categories', () => problem(409, 'category.slug_taken'))
    const user = userEvent.setup()
    await openTab()

    await user.click(screen.getByRole('button', { name: buttonNamed(hy.catalog.categories.add) }))
    const form = await dialog(hy.catalog.categories.add)
    await user.type(within(form).getByLabelText(nameLabel('Հայերեն')), 'Սանտեխնիկա')
    await user.type(within(form).getByLabelText(hy.catalog.form.slug), 'plumbing')
    await user.click(within(form).getByRole('button', { name: hy.catalog.form.save }))

    expect(await within(form).findByText(hy.errors.category.slug_taken)).toBeInTheDocument()
  })

  it('shows server validation errors on the fields', async () => {
    capture('post', 'categories', () =>
      HttpResponse.json(
        { status: 400, code: 'validation_failed', errors: { name: ['name.default_language_required'] } },
        { status: 400 },
      ),
    )
    const user = userEvent.setup()
    await openTab()

    await user.click(screen.getByRole('button', { name: buttonNamed(hy.catalog.categories.add) }))
    const form = await dialog(hy.catalog.categories.add)
    await user.type(within(form).getByLabelText(nameLabel('Հայերեն')), 'Ա')
    await user.type(within(form).getByLabelText(hy.catalog.form.slug), 'a')
    await user.click(within(form).getByRole('button', { name: hy.catalog.form.save }))

    expect(await within(form).findByText(hy.errors.name.default_language_required)).toBeInTheDocument()
  })

  it('shows other failures as a message', async () => {
    capture('post', 'categories', () => problem(422, 'category.too_deep'))
    const user = userEvent.setup()
    await openTab()

    await user.click(screen.getByRole('button', { name: buttonNamed(hy.catalog.categories.add) }))
    const form = await dialog(hy.catalog.categories.add)
    await user.type(within(form).getByLabelText(nameLabel('Հայերեն')), 'Ա')
    await user.type(within(form).getByLabelText(hy.catalog.form.slug), 'a')
    await user.click(within(form).getByRole('button', { name: hy.catalog.form.save }))

    expect(await screen.findByText(hy.errors.category.too_deep)).toBeInTheDocument()
  })

  it('edits a category and keeps translations for inactive languages', async () => {
    const requests = capture('put', 'categories/:id')
    const user = userEvent.setup()
    await openTab()

    await user.click(await screen.findByLabelText(`${hy.catalog.actions.edit}: Սանտեխնիկա`))
    const form = await dialog(hy.catalog.categories.edit)
    expect(within(form).getByLabelText(hy.catalog.form.slug)).toHaveValue('plumbing')
    const english = within(form).getByLabelText(nameLabel('English'))
    await user.clear(english)
    await user.type(english, 'Plumbing works')
    await user.click(within(form).getByRole('button', { name: hy.catalog.form.save }))

    await waitFor(() => expect(requests).toHaveLength(1))
    expect(requests[0].url).toBe('/api/v1/admin/catalog/categories/cat-plumbing')
    expect(requests[0].body).toEqual({
      slug: 'plumbing',
      name: { hy: 'Սանտեխնիկա', en: 'Plumbing works', fr: 'Plomberie' },
      icon: 'pipe',
      parentId: null,
      sortOrder: 2,
    })
    expect(await screen.findByText(hy.catalog.saved)).toBeInTheDocument()
  })

  it('keeps a category with subcategories at the top level', async () => {
    const user = userEvent.setup()
    await openTab()

    await user.click(await screen.findByLabelText(`${hy.catalog.actions.edit}: Վերանորոգում`))
    const form = await dialog(hy.catalog.categories.edit)

    expect(within(form).getByText(hy.catalog.categories.parentLocked)).toBeInTheDocument()
  })

  it('adds a subcategory under the chosen parent', async () => {
    const requests = capture('post', 'categories')
    const user = userEvent.setup()
    await openTab()

    await user.click(await screen.findByLabelText(`${hy.catalog.categories.addSub}: Վերանորոգում`))
    const form = await dialog(hy.catalog.categories.add)
    await user.type(within(form).getByLabelText(nameLabel('Հայերեն')), 'Ներկում')
    await user.type(within(form).getByLabelText(hy.catalog.form.slug), 'painting')
    await user.click(within(form).getByRole('button', { name: hy.catalog.form.save }))

    await waitFor(() => expect(requests).toHaveLength(1))
    expect(requests[0].body.parentId).toBe('cat-renovation')
  })

  it('hides and shows categories', async () => {
    const requests = capture('post', 'categories/:id/:action', () => HttpResponse.json({}))
    const user = userEvent.setup()
    await openTab()

    await user.click(await screen.findByLabelText(`${hy.catalog.actions.hide}: Սանտեխնիկա`))
    await user.click(screen.getByLabelText(`${hy.catalog.actions.show}: Սալիկապատում`))

    await waitFor(() => expect(requests).toHaveLength(2))
    expect(requests.map((r) => r.url)).toEqual([
      '/api/v1/admin/catalog/categories/cat-plumbing/deactivate',
      '/api/v1/admin/catalog/categories/cat-tiling/activate',
    ])
    expect(await screen.findByText(hy.catalog.hiddenDone)).toBeInTheDocument()
  })

  it('deletes a category after confirmation', async () => {
    const requests = capture('delete', 'categories/:id', () => new HttpResponse(null, { status: 204 }))
    const user = userEvent.setup()
    await openTab()

    await user.click(await screen.findByLabelText(`${hy.catalog.actions.delete}: Սանտեխնիկա`))
    const confirm = await screen.findByRole('tooltip')
    await user.click(within(confirm).getByRole('button', { name: hy.catalog.actions.delete }))

    await waitFor(() => expect(requests.map((r) => r.url)).toEqual(['/api/v1/admin/catalog/categories/cat-plumbing']))
    expect(await screen.findByText(hy.catalog.deleted)).toBeInTheDocument()
  })

  it('explains why a category cannot be deleted', async () => {
    capture('delete', 'categories/:id', () => problem(422, 'category.has_subcategories'))
    const user = userEvent.setup()
    await openTab()

    await user.click(await screen.findByLabelText(`${hy.catalog.actions.delete}: Վերանորոգում`))
    const confirm = await screen.findByRole('tooltip')
    await user.click(within(confirm).getByRole('button', { name: hy.catalog.actions.delete }))

    expect(await screen.findByText(hy.errors.category.has_subcategories)).toBeInTheDocument()
  })

  it('shows an error when the categories cannot be loaded', async () => {
    server.use(http.get('*/api/v1/admin/catalog/categories', () => new HttpResponse(null, { status: 500 })))
    renderRoute('/catalog/categories')

    expect(await screen.findByText(hy.errors.generic)).toBeInTheDocument()
  })
})
