import { screen, waitFor, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import hyAM from 'antd/locale/hy_AM'
import { http, HttpResponse } from 'msw'
import i18n from '@/i18n'
import hy from '@/i18n/locales/hy/common.json'
import { problem } from '@/test/auth'
import { renderRoute } from '@/test/renderWithProviders'
import { server } from '@/test/server'

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

async function dialog(title) {
  const element = await screen.findByRole('dialog')
  expect(within(element).getByText(title)).toBeInTheDocument()
  return element
}

async function openTab() {
  renderRoute('/catalog/cities')
  return screen.findByRole('table')
}

async function expandYerevan(user) {
  const [firstExpand] = await screen.findAllByRole('button', { name: hyAM.Table.expand })
  await user.click(firstExpand)
}

describe('Cities tab', () => {
  it('opens from the Catalog tabs and lists cities with their status', async () => {
    const user = userEvent.setup()
    const { router } = renderRoute('/catalog/categories')

    await user.click(await screen.findByRole('tab', { name: hy.catalog.tabs.cities }))

    await waitFor(() => expect(router.state.location.pathname).toBe('/catalog/cities'))
    const table = (await screen.findByText('Երևան')).closest('table')
    expect(within(table).getByText('Մասիս')).toBeInTheDocument()
    expect(within(table).getByText(hy.catalog.status.hidden)).toBeInTheDocument()
  })

  it("shows a city's districts when expanded", async () => {
    const user = userEvent.setup()
    await openTab()

    await expandYerevan(user)

    expect(await screen.findByText('Կենտրոն')).toBeInTheDocument()
  })

  it('adds a city', async () => {
    const requests = capture('post', 'cities')
    const user = userEvent.setup()
    await openTab()

    await user.click(screen.getByRole('button', { name: buttonNamed(hy.catalog.cities.add) }))
    const form = await dialog(hy.catalog.cities.add)
    await user.type(within(form).getByLabelText(nameLabel('Հայերեն')), 'Գյումրի')
    await user.type(within(form).getByLabelText(nameLabel('English')), 'Gyumri')
    await user.click(within(form).getByRole('button', { name: hy.catalog.form.save }))

    await waitFor(() => expect(requests).toHaveLength(1))
    expect(requests[0].body).toEqual({ slug: 'gyumri', name: { hy: 'Գյումրի', en: 'Gyumri' }, sortOrder: 0 })
    expect(await screen.findByText(hy.catalog.created)).toBeInTheDocument()
  })

  it('edits a city', async () => {
    const requests = capture('put', 'cities/:id')
    const user = userEvent.setup()
    await openTab()

    await user.click(await screen.findByLabelText(`${hy.catalog.actions.edit}: Մասիս`))
    const form = await dialog(hy.catalog.cities.edit)
    await user.type(within(form).getByLabelText(nameLabel('English')), 'Masis')
    await user.click(within(form).getByRole('button', { name: hy.catalog.form.save }))

    await waitFor(() => expect(requests).toHaveLength(1))
    expect(requests[0].url).toBe('/api/v1/admin/catalog/cities/city-masis')
    expect(requests[0].body).toEqual({ slug: 'masis', name: { hy: 'Մասիս', en: 'Masis' }, sortOrder: 2 })
  })

  it('shows and hides cities', async () => {
    const requests = capture('post', 'cities/:id/:action', () => HttpResponse.json({}))
    const user = userEvent.setup()
    await openTab()

    await user.click(await screen.findByLabelText(`${hy.catalog.actions.show}: Մասիս`))

    await waitFor(() => expect(requests.map((r) => r.url)).toEqual(['/api/v1/admin/catalog/cities/city-masis/activate']))
    expect(await screen.findByText(hy.catalog.shownDone)).toBeInTheDocument()
  })

  it('adds a district to a city', async () => {
    const requests = capture('post', 'cities/:id/districts')
    const user = userEvent.setup()
    await openTab()
    await expandYerevan(user)

    const addLabel = i18n.t('catalog.districts.addTo', { city: 'Երևան' })
    await user.click(await screen.findByRole('button', { name: buttonNamed(addLabel) }))
    const form = await dialog(addLabel)
    await user.type(within(form).getByLabelText(nameLabel('Հայերեն')), 'Արաբկիր')
    await user.type(within(form).getByLabelText(hy.catalog.form.slug), 'arabkir')
    await user.click(within(form).getByRole('button', { name: hy.catalog.form.save }))

    await waitFor(() => expect(requests).toHaveLength(1))
    expect(requests[0].url).toBe('/api/v1/admin/catalog/cities/city-yerevan/districts')
    expect(requests[0].body).toEqual({ slug: 'arabkir', name: { hy: 'Արաբկիր' }, sortOrder: 0 })
  })

  it('edits and hides a district', async () => {
    const edits = capture('put', 'cities/:cityId/districts/:id')
    const toggles = capture('post', 'cities/:cityId/districts/:id/:action', () => HttpResponse.json({}))
    const user = userEvent.setup()
    await openTab()
    await expandYerevan(user)

    await user.click(await screen.findByLabelText(`${hy.catalog.actions.edit}: Կենտրոն`))
    const form = await dialog(hy.catalog.districts.edit)
    const english = within(form).getByLabelText(nameLabel('English'))
    await user.clear(english)
    await user.type(english, 'Center')
    await user.click(within(form).getByRole('button', { name: hy.catalog.form.save }))
    await waitFor(() => expect(edits).toHaveLength(1))
    expect(edits[0].url).toBe('/api/v1/admin/catalog/cities/city-yerevan/districts/district-kentron')
    expect(edits[0].body.name).toEqual({ hy: 'Կենտրոն', en: 'Center' })

    await user.click(await screen.findByLabelText(`${hy.catalog.actions.hide}: Կենտրոն`))
    await waitFor(() =>
      expect(toggles.map((r) => r.url)).toEqual(['/api/v1/admin/catalog/cities/city-yerevan/districts/district-kentron/deactivate']),
    )
  })

  it('shows a taken district slug on the slug field', async () => {
    capture('post', 'cities/:id/districts', () => problem(409, 'district.slug_taken'))
    const user = userEvent.setup()
    await openTab()
    await expandYerevan(user)

    const addLabel = i18n.t('catalog.districts.addTo', { city: 'Երևան' })
    await user.click(await screen.findByRole('button', { name: buttonNamed(addLabel) }))
    const form = await dialog(addLabel)
    await user.type(within(form).getByLabelText(nameLabel('Հայերեն')), 'Կենտրոն')
    await user.type(within(form).getByLabelText(hy.catalog.form.slug), 'kentron')
    await user.click(within(form).getByRole('button', { name: hy.catalog.form.save }))

    expect(await within(form).findByText(hy.errors.district.slug_taken)).toBeInTheDocument()
  })

  it('explains a failed change', async () => {
    capture('post', 'cities/:id/:action', () => problem(404, 'city.not_found'))
    const user = userEvent.setup()
    await openTab()

    await user.click(await screen.findByLabelText(`${hy.catalog.actions.hide}: Երևան`))

    expect(await screen.findByText(hy.errors.city.not_found)).toBeInTheDocument()
  })

  it('shows an error when the cities cannot be loaded', async () => {
    server.use(http.get('*/api/v1/admin/catalog/cities', () => new HttpResponse(null, { status: 500 })))
    renderRoute('/catalog/cities')

    expect(await screen.findByText(hy.errors.generic)).toBeInTheDocument()
  })
})
