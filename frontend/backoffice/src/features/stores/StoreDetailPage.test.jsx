import { screen, waitFor, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { http, HttpResponse } from 'msw'
import hy from '@/i18n/locales/hy/common.json'
import { problem } from '@/test/auth'
import { renderRoute } from '@/test/renderWithProviders'
import { server } from '@/test/server'
import { PRODUCTS, branch, productsPage, storeDetail } from '@/test/stores'

const SLOW = 60_000
const HEAVY = 240_000 // dialogs with several selects are slow to drive in jsdom
const s = hy.stores
const p = s.product
const b = s.branches

function capture(method, path, respond = (body) => HttpResponse.json(body)) {
  const requests = []
  server.use(
    http[method](`*/api/v1/admin/stores/${path}`, async ({ request }) => {
      const body = request.method === 'GET' || request.method === 'DELETE' ? null : await request.json().catch(() => null)
      requests.push({ url: new URL(request.url), body })
      return respond(body)
    }),
  )
  return requests
}

describe('Store page: details', () => {
  it('shows the store and keeps the tab in the address', async () => {
    const user = userEvent.setup()
    const { router } = renderRoute('/stores/store-demo')

    expect(await screen.findByRole('heading', { name: 'Demo Hardware', level: 1 })).toBeInTheDocument()
    expect(screen.getByLabelText(s.form.website)).toHaveValue('https://demo-hardware.example')
    expect(screen.getByLabelText(s.form.descriptionIn.replace('{{language}}', 'English'))).toHaveValue('Building materials')

    await user.click(screen.getByRole('tab', { name: new RegExp(s.tabs.branches) }))
    await waitFor(() => expect(router.state.location.search).toBe('?tab=branches'))
    await user.click(screen.getByRole('tab', { name: new RegExp(s.tabs.details) }))
    await waitFor(() => expect(router.state.location.search).toBe(''))
  }, SLOW)

  it('opens on the tab named in the address, and ignores an unknown one', async () => {
    const { unmount } = renderRoute('/stores/store-demo?tab=products')
    expect(await screen.findByText('Putty 25 kg')).toBeInTheDocument()
    unmount()
    renderRoute('/stores/store-demo?tab=nonsense')
    expect(await screen.findByLabelText(s.form.website)).toBeInTheDocument()
  }, SLOW)

  it('saves changes to the details', async () => {
    const requests = capture('put', 'store-demo', () => HttpResponse.json(storeDetail()))
    const user = userEvent.setup()
    renderRoute('/stores/store-demo')

    const phone = await screen.findByLabelText(s.form.phone)
    await user.clear(phone)
    await user.type(phone, '+37499000000')
    await user.click(screen.getByRole('button', { name: hy.catalog.form.save }))

    await waitFor(() => expect(requests).toHaveLength(1))
    expect(requests[0].body).toMatchObject({ slug: 'demo-hardware', name: 'Demo Hardware', phone: '+37499000000', website: 'https://demo-hardware.example', isActive: true })
    expect(await screen.findByText(hy.catalog.saved)).toBeInTheDocument()
  }, SLOW)

  it('shows an error when the store does not exist', async () => {
    server.use(http.get('*/api/v1/admin/stores/missing', () => problem(404, 'store.not_found')))
    renderRoute('/stores/missing')
    expect(await screen.findByText(hy.errors.store.not_found)).toBeInTheDocument()
    expect(screen.getByRole('link', { name: s.back })).toHaveAttribute('href', '/stores')
  }, SLOW)
})

describe('Store page: branches', () => {
  it('shows the saved branches', async () => {
    renderRoute('/stores/store-demo?tab=branches')
    expect(await screen.findByLabelText(b.name)).toHaveValue('Kentron branch')
    expect(screen.getByLabelText(b.address)).toHaveValue('1 Demo street')
    expect(screen.getByLabelText(b.hours)).toHaveValue('09:00–18:00')
  }, SLOW)

  it('adds a branch, checks the required fields, and saves them all at once', async () => {
    const requests = capture('put', 'store-demo/branches', () => HttpResponse.json(storeDetail()))
    const user = userEvent.setup()
    renderRoute('/stores/store-demo?tab=branches')
    await screen.findByLabelText(b.name)

    await user.click(screen.getByRole('button', { name: new RegExp(b.add) }))
    await user.click(screen.getByRole('button', { name: b.save }))
    expect(await screen.findByText(b.townRequired)).toBeInTheDocument()
    expect(requests).toHaveLength(0)

    const names = screen.getAllByLabelText(b.name)
    await user.type(names[1], 'Masis branch')
    const addresses = screen.getAllByLabelText(b.address)
    await user.type(addresses[1], '5 Masis road')
    const towns = screen.getAllByLabelText(b.town)
    await user.click(towns[1])
    await user.click(await screen.findByTitle('Մասիս'))
    await user.click(screen.getByRole('button', { name: b.save }))

    await waitFor(() => expect(requests).toHaveLength(1))
    expect(requests[0].body.branches).toEqual([
      { name: 'Kentron branch', cityId: 'city-yerevan', districtId: 'district-kentron', address: '1 Demo street', phone: '+37411000001', openingHours: '09:00–18:00' },
      { name: 'Masis branch', cityId: 'city-masis', districtId: null, address: '5 Masis road', phone: null, openingHours: null },
    ])
  }, HEAVY)

  it('removes a branch and shows an error from the API', async () => {
    capture('put', 'store-demo/branches', () => problem(422, 'store_branch.city_not_found'))
    server.use(http.get('*/api/v1/admin/stores/store-demo', () => HttpResponse.json(storeDetail({ branches: [branch(), branch({ id: 'b2', name: 'Second' })] }))))
    const user = userEvent.setup()
    renderRoute('/stores/store-demo?tab=branches')
    await screen.findAllByLabelText(b.name)

    await user.click(screen.getByRole('button', { name: `${hy.catalog.actions.delete}: ${b.branch.replace('{{number}}', '2')}` }))
    expect(screen.getAllByLabelText(b.name)).toHaveLength(1)
    await user.click(screen.getByRole('button', { name: b.save }))
    expect(await screen.findByText(hy.errors.store_branch.city_not_found)).toBeInTheDocument()
  }, SLOW)

  it('says when there are no branches', async () => {
    server.use(http.get('*/api/v1/admin/stores/store-demo', () => HttpResponse.json(storeDetail({ branches: [] }))))
    renderRoute('/stores/store-demo?tab=branches')
    expect(await screen.findByText(b.empty)).toBeInTheDocument()
  }, SLOW)
})

describe('Store page: products', () => {
  it('lists products with their prices, sale and link', async () => {
    renderRoute('/stores/store-demo?tab=products')

    const table = (await screen.findByText('Putty 25 kg')).closest('table')
    expect(within(table).getByText('DemoBrand · P-25')).toBeInTheDocument()
    expect(within(table).getByText('Finishing putty')).toBeInTheDocument()
    expect(within(table).getByText(s.grades.Standard)).toBeInTheDocument()
    expect(within(table).getByText('25 kg bag')).toBeInTheDocument()
    expect(within(table).getByText(/^200 ֏$/)).toBeInTheDocument()
    expect(within(table).getByText(p.notLinked)).toBeInTheDocument()
    expect(within(table).getByText(/^7\D?500 ֏$/)).toBeInTheDocument()
    expect(within(table).getByText(p.outOfStock)).toBeInTheDocument()
    expect(within(table).getByText(p.stale)).toBeInTheDocument()
  }, SLOW)

  it('filters by what is shown and by search, keeping them in the address', async () => {
    const requests = []
    server.use(
      http.get('*/api/v1/admin/stores/store-demo/products', ({ request }) => {
        requests.push(Object.fromEntries(new URL(request.url).searchParams))
        return HttpResponse.json(productsPage())
      }),
    )
    const user = userEvent.setup()
    const { router } = renderRoute('/stores/store-demo?tab=products')
    await screen.findByText('Putty 25 kg')
    expect(requests[0]).toEqual({ page: '1', pageSize: '25' })

    await user.click(screen.getByRole('combobox', { name: p.show }))
    await user.click(await screen.findByTitle(p.shown.stale))
    await waitFor(() => expect(requests.at(-1)).toEqual({ stale: 'true', page: '1', pageSize: '25' }))
    expect(router.state.location.search).toContain('show=stale')

    await user.click(screen.getByRole('combobox', { name: p.show }))
    await user.click(await screen.findByTitle(p.shown.linked))
    await waitFor(() => expect(requests.at(-1)).toMatchObject({ linked: 'true' }))

    await user.click(screen.getByRole('combobox', { name: p.show }))
    await user.click(await screen.findByTitle(p.shown.unlinked))
    await waitFor(() => expect(requests.at(-1)).toMatchObject({ linked: 'false' }))

    await user.click(screen.getByRole('combobox', { name: p.show }))
    await user.click(await screen.findByTitle(p.shown.all))
    await user.type(screen.getByRole('searchbox', { name: p.search }), 'putty{Enter}')
    await waitFor(() => expect(requests.at(-1)).toMatchObject({ search: 'putty' }))
  }, SLOW)

  it('pages through products', async () => {
    const requests = []
    server.use(
      http.get('*/api/v1/admin/stores/store-demo/products', ({ request }) => {
        requests.push(Object.fromEntries(new URL(request.url).searchParams))
        return HttpResponse.json(productsPage(PRODUCTS, { totalCount: 60 }))
      }),
    )
    const user = userEvent.setup()
    renderRoute('/stores/store-demo?tab=products')
    await screen.findByText('Putty 25 kg')

    await user.click(await screen.findByTitle('2'))
    await waitFor(() => expect(requests.at(-1)).toMatchObject({ page: '2' }))
  }, SLOW)

  it('confirms a price', async () => {
    const requests = capture('post', 'store-demo/products/product-paint/confirm', () => HttpResponse.json(PRODUCTS[1]))
    const user = userEvent.setup()
    renderRoute('/stores/store-demo?tab=products')

    await user.click(await screen.findByRole('button', { name: `${p.confirmPrice}: Paint 10 L` }))
    await waitFor(() => expect(requests).toHaveLength(1))
    expect(await screen.findByText(p.priceConfirmed)).toBeInTheDocument()
  }, SLOW)

  it('deletes a product', async () => {
    const requests = capture('delete', 'store-demo/products/product-paint', () => new HttpResponse(null, { status: 204 }))
    const user = userEvent.setup()
    renderRoute('/stores/store-demo?tab=products')

    await user.click(await screen.findByRole('button', { name: `${hy.catalog.actions.delete}: Paint 10 L` }))
    await user.click(await screen.findByRole('button', { name: hy.catalog.actions.delete }))
    await waitFor(() => expect(requests).toHaveLength(1))
    expect(await screen.findByText(hy.catalog.deleted)).toBeInTheDocument()
  }, SLOW)

  it('shows an error when a product can not be deleted', async () => {
    capture('delete', 'store-demo/products/product-paint', () => problem(404, 'store_product.not_found'))
    const user = userEvent.setup()
    renderRoute('/stores/store-demo?tab=products')

    await user.click(await screen.findByRole('button', { name: `${hy.catalog.actions.delete}: Paint 10 L` }))
    await user.click(await screen.findByRole('button', { name: hy.catalog.actions.delete }))
    expect(await screen.findByText(hy.errors.store_product.not_found)).toBeInTheDocument()
  }, SLOW)

  it('adds a product linked to a material', async () => {
    const requests = capture('post', 'store-demo/products', () => HttpResponse.json(PRODUCTS[0]))
    const user = userEvent.setup()
    renderRoute('/stores/store-demo?tab=products')

    await user.click(await screen.findByRole('button', { name: new RegExp(p.add) }))
    const dialog = await screen.findByRole('dialog')
    await user.type(within(dialog).getByLabelText(p.name), 'Primer 10 L')
    await user.click(within(dialog).getByLabelText(p.material))
    await user.click(await screen.findByTitle('Գրունտ'))
    await user.click(within(dialog).getByRole('button', { name: hy.catalog.form.save }))
    expect(await within(dialog).findByText(p.gradeRequired)).toBeInTheDocument()
    expect(requests).toHaveLength(0)

    await user.click(within(dialog).getByLabelText(p.grade))
    await user.click(await screen.findByTitle(s.grades.Premium))
    await user.type(within(dialog).getByLabelText(p.packSizeIn.replace('{{unit}}', hy.catalog.materials.units.Liter)), '10')
    await user.type(within(dialog).getByLabelText(p.price), '9000')
    await user.click(within(dialog).getByRole('button', { name: hy.catalog.form.save }))

    await waitFor(() => expect(requests).toHaveLength(1))
    expect(requests[0].body).toEqual({
      name: 'Primer 10 L',
      brand: null,
      code: null,
      packSize: 10,
      packLabel: null,
      price: 9000,
      salePrice: null,
      saleEndsOn: null,
      inStock: true,
      photoFileId: null,
      materialId: 'mat-primer',
      grade: 'Premium',
      isActive: true,
    })
    expect(await screen.findByText(hy.catalog.created)).toBeInTheDocument()
  }, HEAVY)

  it('checks the sale fields before saving', async () => {
    const requests = capture('post', 'store-demo/products', () => HttpResponse.json(PRODUCTS[0]))
    const user = userEvent.setup()
    renderRoute('/stores/store-demo?tab=products')

    await user.click(await screen.findByRole('button', { name: new RegExp(p.add) }))
    const dialog = await screen.findByRole('dialog')
    await user.type(within(dialog).getByLabelText(p.name), 'Glue')
    await user.type(within(dialog).getByLabelText(p.packSize), '1')
    await user.type(within(dialog).getByLabelText(p.price), '1000')
    await user.type(within(dialog).getByLabelText(p.salePrice), '800')
    await user.click(within(dialog).getByRole('button', { name: hy.catalog.form.save }))

    expect((await within(dialog).findAllByText(p.saleNeedsBoth)).length).toBeGreaterThan(0)
    expect(requests).toHaveLength(0)
  }, HEAVY)

  it('edits a product and shows a taken code on its field', async () => {
    const requests = capture('put', 'store-demo/products/product-putty', () => problem(409, 'store_product.code_taken'))
    const user = userEvent.setup()
    renderRoute('/stores/store-demo?tab=products')

    await user.click(await screen.findByRole('button', { name: `${hy.catalog.actions.edit}: Putty 25 kg` }))
    const dialog = await screen.findByRole('dialog')
    expect(within(dialog).getByLabelText(p.name)).toHaveValue('Putty 25 kg')
    expect(within(dialog).getByLabelText(p.code)).toHaveValue('P-25')
    await user.click(within(dialog).getByRole('button', { name: hy.catalog.form.save }))

    expect(await within(dialog).findByText(hy.errors.store_product.code_taken)).toBeInTheDocument()
    expect(requests[0].body).toMatchObject({ code: 'P-25', materialId: 'mat-putty', grade: 'Standard', packSize: 25, price: 5000 })
  }, SLOW)

  it('shows other API errors as a message', async () => {
    capture('put', 'store-demo/products/product-putty', () => problem(422, 'store_product.material_not_found'))
    const user = userEvent.setup()
    renderRoute('/stores/store-demo?tab=products')

    await user.click(await screen.findByRole('button', { name: `${hy.catalog.actions.edit}: Putty 25 kg` }))
    await user.click(within(await screen.findByRole('dialog')).getByRole('button', { name: hy.catalog.form.save }))
    expect(await screen.findByText(hy.errors.store_product.material_not_found)).toBeInTheDocument()
  }, SLOW)
})
