import { screen, waitFor, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { http, HttpResponse } from 'msw'
import i18n from '@/i18n'
import hy from '@/i18n/locales/hy/common.json'
import { renderRoute } from '@/test/renderWithProviders'
import { server } from '@/test/server'

const SLOW = 60_000
// The fixture's Tiling subcategory is hidden, so the form marks it.
const TILING = `Սալիկապատում (${hy.catalog.status.hidden})`
const nameLabel = (language) => i18n.t('catalog.form.nameIn', { language })
const buttonNamed = (text) => new RegExp(text.replace(/[.*+?^${}()|[\]\\]/g, '\\$&'))

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
  renderRoute('/catalog/work-items')
  return (await screen.findByText('Պատերի սվաղում')).closest('table')
}

async function chooseOption(user, label, option, container = document.body) {
  await user.click(within(container).getByLabelText(label))
  await user.click((await screen.findAllByTitle(option)).find((element) => element.closest('.ant-select-dropdown')))
}

describe('Work items tab', () => {
  it('opens from the Catalog tabs and lists items with their category, unit and price', async () => {
    const user = userEvent.setup()
    const { router } = renderRoute('/catalog/categories')

    await user.click(await screen.findByRole('tab', { name: hy.catalog.tabs['work-items'] }))

    await waitFor(() => expect(router.state.location.pathname).toBe('/catalog/work-items'))
    const table = (await screen.findByText('Պատերի սվաղում')).closest('table')
    expect(within(table).getByText('wall-plastering')).toBeInTheDocument()
    expect(within(table).getAllByText('Սալիկապատում').length).toBeGreaterThan(0)
    expect(within(table).getAllByText(hy.catalog.workItems.units.SquareMeter)).toHaveLength(2)
    expect(within(table).getByText(/^3\D?500 ֏$/)).toBeInTheDocument()
    expect(within(table).getByLabelText(hy.catalog.workItems.locked)).toBeInTheDocument()
    expect(within(table).getByText(hy.catalog.workItems.noPrice)).toBeInTheDocument()
    expect(within(table).getByText('4')).toBeInTheDocument() // partners who priced wall plastering
    expect(within(table).getByText(/^Գործընկերներ՝ 6\D?000 – 8\D?000 ֏$/)).toBeInTheDocument() // floor tiling's market range
    expect(screen.getByText(i18n.t('catalog.workItems.count', { shown: 3, total: 3 }))).toBeInTheDocument()
  }, SLOW)

  it('filters by status and text', async () => {
    const user = userEvent.setup()
    const table = await openTab()

    await user.type(screen.getByLabelText(hy.catalog.workItems.search), 'leak')
    await waitFor(() => expect(within(table).queryByText('Պատերի սվաղում')).not.toBeInTheDocument())
    expect(within(table).getByText('Արտահոսքի վերացում')).toBeInTheDocument()

    await user.clear(screen.getByLabelText(hy.catalog.workItems.search))
    await chooseOption(user, hy.catalog.columns.status, hy.catalog.workItems.statuses.unpriced)
    await waitFor(() => expect(within(table).queryByText('Պատերի սվաղում')).not.toBeInTheDocument())
    expect(within(table).getByText('Արտահոսքի վերացում')).toBeInTheDocument()
    expect(within(table).queryByText('Հատակի սալիկապատում')).not.toBeInTheDocument()
  }, SLOW)

  it('filters by a main category including its subcategories', async () => {
    const user = userEvent.setup()
    const table = await openTab()

    await chooseOption(user, hy.catalog.workItems.category, i18n.t('catalog.workItems.allIn', { name: 'Սանտեխնիկա' }))

    await waitFor(() => expect(within(table).queryByText('Պատերի սվաղում')).not.toBeInTheDocument())
    expect(within(table).getByText('Արտահոսքի վերացում')).toBeInTheDocument()
  }, SLOW)

  it('adds a work item with a price range', async () => {
    const requests = capture('post', 'work-items')
    const user = userEvent.setup()
    await openTab()

    await user.click(screen.getByRole('button', { name: buttonNamed(hy.catalog.workItems.add) }))
    const form = await screen.findByRole('dialog')
    await chooseOption(user, hy.catalog.workItems.subcategory, TILING, form)
    await user.type(within(form).getByLabelText(nameLabel('Հայերեն')), 'Խճանկար')
    await user.type(within(form).getByLabelText(nameLabel('English')), 'Mosaic laying')
    await user.type(within(form).getByLabelText(hy.catalog.workItems.priceMin), '8000')
    await user.type(within(form).getByLabelText(hy.catalog.workItems.priceTypical), '12000')
    await user.type(within(form).getByLabelText(hy.catalog.workItems.priceMax), '18000')
    await user.click(within(form).getByRole('button', { name: hy.catalog.form.save }))

    await waitFor(() => expect(requests).toHaveLength(1))
    expect(requests[0].body).toEqual({
      categoryId: 'cat-tiling',
      slug: 'mosaic-laying',
      name: { hy: 'Խճանկար', en: 'Mosaic laying' },
      unit: 'SquareMeter',
      surface: 'None',
      sortOrder: 0,
      priceMin: 8000,
      priceTypical: 12000,
      priceMax: 18000,
      isPriceLocked: false,
    })
    expect(await screen.findByText(hy.catalog.created)).toBeInTheDocument()
  }, SLOW)

  it('checks the price range before saving', async () => {
    const requests = capture('post', 'work-items')
    const user = userEvent.setup()
    await openTab()

    await user.click(screen.getByRole('button', { name: buttonNamed(hy.catalog.workItems.add) }))
    const form = await screen.findByRole('dialog')
    await chooseOption(user, hy.catalog.workItems.subcategory, TILING, form)
    await user.type(within(form).getByLabelText(nameLabel('Հայերեն')), 'Խճանկար')
    await user.type(within(form).getByLabelText(hy.catalog.workItems.priceMin), '5000')
    await user.click(within(form).getByRole('button', { name: hy.catalog.form.save }))
    expect(await within(form).findByText(hy.catalog.workItems.priceIncomplete)).toBeInTheDocument()

    await user.type(within(form).getByLabelText(hy.catalog.workItems.priceTypical), '3000')
    await user.type(within(form).getByLabelText(hy.catalog.workItems.priceMax), '9000')
    expect(await within(form).findByText(hy.catalog.workItems.priceOrder)).toBeInTheDocument()
    expect(requests).toHaveLength(0)
  }, SLOW)

  it('edits a work item and keeps its other translations', async () => {
    const requests = capture('put', 'work-items/:id')
    const user = userEvent.setup()
    await openTab()

    await user.click(await screen.findByLabelText(`${hy.catalog.actions.edit}: Պատերի սվաղում`))
    const form = await screen.findByRole('dialog')
    const typical = within(form).getByLabelText(hy.catalog.workItems.priceTypical)
    await user.clear(typical)
    await user.type(typical, '4000')
    await user.click(within(form).getByLabelText(hy.catalog.workItems.lock))
    await user.click(within(form).getByRole('button', { name: hy.catalog.form.save }))

    await waitFor(() => expect(requests).toHaveLength(1))
    expect(requests[0].url).toBe('/api/v1/admin/catalog/work-items/wi-plastering')
    expect(requests[0].body).toMatchObject({
      categoryId: 'cat-tiling',
      slug: 'wall-plastering',
      name: { hy: 'Պատերի սվաղում', en: 'Wall plastering', ar: 'لياسة الجدران' },
      unit: 'SquareMeter',
      surface: 'Wall',
      priceMin: 2500,
      priceTypical: 4000,
      priceMax: 5000,
      isPriceLocked: false,
    })
  }, SLOW)

  it('shows server errors on the form', async () => {
    capture('post', 'work-items', () =>
      HttpResponse.json(
        { status: 400, code: 'validation_failed', errors: { price: ['price.invalid'], slug: ['work_item.slug_taken'] } },
        { status: 400 },
      ),
    )
    const user = userEvent.setup()
    await openTab()

    await user.click(screen.getByRole('button', { name: buttonNamed(hy.catalog.workItems.add) }))
    const form = await screen.findByRole('dialog')
    await chooseOption(user, hy.catalog.workItems.subcategory, TILING, form)
    await user.type(within(form).getByLabelText(nameLabel('Հայերեն')), 'Խճանկար')
    await user.type(within(form).getByLabelText(nameLabel('English')), 'Mosaic')
    await user.click(within(form).getByRole('button', { name: hy.catalog.form.save }))

    expect(await within(form).findByText(hy.errors.price.invalid)).toBeInTheDocument()
    expect(within(form).getByText(hy.errors.work_item.slug_taken)).toBeInTheDocument()
  }, SLOW)

  it('hides, shows and deletes work items', async () => {
    const toggles = capture('post', 'work-items/:id/:action', () => HttpResponse.json({}))
    const deletes = capture('delete', 'work-items/:id', () => new HttpResponse(null, { status: 204 }))
    const user = userEvent.setup()
    await openTab()

    await user.click(await screen.findByLabelText(`${hy.catalog.actions.show}: Հատակի սալիկապատում`))
    await waitFor(() => expect(toggles.map((r) => r.url)).toEqual(['/api/v1/admin/catalog/work-items/wi-floor-tiling/activate']))

    await user.click(screen.getByLabelText(`${hy.catalog.actions.delete}: Արտահոսքի վերացում`))
    await user.click(await screen.findByRole('button', { name: hy.catalog.actions.delete }))
    await waitFor(() => expect(deletes.map((r) => r.url)).toEqual(['/api/v1/admin/catalog/work-items/wi-leaks']))
    expect(await screen.findByText(hy.catalog.deleted)).toBeInTheDocument()
  }, SLOW)
})
