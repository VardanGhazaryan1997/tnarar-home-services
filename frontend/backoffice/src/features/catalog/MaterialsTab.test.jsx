import { screen, waitFor, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { http, HttpResponse } from 'msw'
import i18n from '@/i18n'
import hy from '@/i18n/locales/hy/common.json'
import { renderRoute } from '@/test/renderWithProviders'
import { server } from '@/test/server'

const SLOW = 60_000
const m = hy.catalog.materials
const w = hy.catalog.workItemMaterials
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

describe('Materials tab', () => {
  it('opens from the Catalog tabs and lists materials with their prices', async () => {
    const user = userEvent.setup()
    const { router } = renderRoute('/catalog/categories')

    await user.click(await screen.findByRole('tab', { name: hy.catalog.tabs.materials }))

    await waitFor(() => expect(router.state.location.pathname).toBe('/catalog/materials'))
    const table = (await screen.findByText('Վերջնական շպաքլ')).closest('table')
    expect(within(table).getByText('finishing-putty')).toBeInTheDocument()
    expect(within(table).getByText(m.units.Kilogram)).toBeInTheDocument()
    expect(within(table).getByText(/^230 ֏$/)).toBeInTheDocument()
    expect(within(table).getByText(/^1\D?400 ֏$/)).toBeInTheDocument()
    expect(within(table).getByText(hy.catalog.status.hidden)).toBeInTheDocument()
  }, SLOW)

  it('adds a material and checks the price order first', async () => {
    const requests = capture('post', 'materials')
    const user = userEvent.setup()
    renderRoute('/catalog/materials')

    await user.click(await screen.findByRole('button', { name: buttonNamed(m.add) }))
    const form = await screen.findByRole('dialog')
    await user.type(within(form).getByLabelText(nameLabel('Հայերեն')), 'Ներկ')
    await user.type(within(form).getByLabelText(nameLabel('English')), 'Interior paint')
    await user.type(within(form).getByLabelText(m.priceBudget), '2500')
    await user.type(within(form).getByLabelText(m.priceStandard), '1200')
    await user.type(within(form).getByLabelText(m.pricePremium), '5000')
    await user.click(within(form).getByRole('button', { name: hy.catalog.form.save }))
    expect(await within(form).findByText(m.priceOrder)).toBeInTheDocument()
    expect(requests).toHaveLength(0)

    await user.clear(within(form).getByLabelText(m.priceStandard))
    await user.type(within(form).getByLabelText(m.priceStandard), '3000')
    await user.click(within(form).getByRole('button', { name: hy.catalog.form.save }))

    await waitFor(() => expect(requests).toHaveLength(1))
    expect(requests[0].body).toEqual({
      slug: 'interior-paint',
      name: { hy: 'Ներկ', en: 'Interior paint' },
      unit: 'Kilogram',
      priceBudget: 2500,
      priceStandard: 3000,
      pricePremium: 5000,
      sortOrder: 0,
      isActive: true,
    })
    expect(await screen.findByText(hy.catalog.created)).toBeInTheDocument()
  }, SLOW)

  it('explains why a material in use can’t be deleted', async () => {
    capture('delete', 'materials/mat-putty', () => HttpResponse.json({ status: 422, code: 'material.in_use' }, { status: 422 }))
    const user = userEvent.setup()
    renderRoute('/catalog/materials')

    await user.click(await screen.findByRole('button', { name: `${hy.catalog.actions.delete}: Վերջնական շպաքլ` }))
    await user.click(await screen.findByRole('button', { name: hy.catalog.actions.delete }))

    expect(await screen.findByText(hy.errors.material.in_use)).toBeInTheDocument()
  }, SLOW)
})

describe('Materials of a work item', () => {
  it('shows, changes and saves what the work uses', async () => {
    const requests = capture('put', 'work-items/wi-plastering/materials', (body) => HttpResponse.json(body.items))
    const user = userEvent.setup()
    renderRoute('/catalog/work-items')

    await user.click(await screen.findByRole('button', { name: `${w.open}: Պատերի սվաղում` }))
    const dialog = await screen.findByRole('dialog')
    const amount = await within(dialog).findByLabelText(`${w.amount}: Վերջնական շպաքլ`)
    expect(amount).toHaveValue('1.200')
    expect(within(dialog).getByText(/^13[.,]2 կգ$/)).toBeInTheDocument() // 10 m² × 1.2 kg + 10%

    await user.clear(amount)
    await user.type(amount, '1.5')
    await user.click(within(dialog).getByLabelText(w.add))
    await user.click((await screen.findAllByTitle('primer')).find((element) => element.closest('.ant-select-dropdown')))
    await user.click(within(dialog).getByRole('button', { name: hy.catalog.form.save }))

    await waitFor(() => expect(requests).toHaveLength(1))
    expect(requests[0].body).toEqual({
      items: [
        { materialId: 'mat-putty', quantityPerUnit: 1.5, wastePercent: 10 },
        { materialId: 'mat-primer', quantityPerUnit: 1, wastePercent: 0 },
      ],
    })
  }, SLOW)
})
