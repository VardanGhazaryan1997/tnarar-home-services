import { screen, waitFor, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { http, HttpResponse } from 'msw'
import hy from '@/i18n/locales/hy/common.json'
import { problem, signedInAs, staffMember } from '@/test/auth'
import { renderRoute } from '@/test/renderWithProviders'
import { server } from '@/test/server'
import { store } from '@/test/stores'

const SLOW = 60_000
const s = hy.stores

function captureList() {
  const requests = []
  server.use(
    http.get('*/api/v1/admin/stores', ({ request }) => {
      requests.push(Object.fromEntries(new URL(request.url).searchParams))
      return HttpResponse.json([store()])
    }),
  )
  return requests
}

describe('Stores page', () => {
  it('lists stores with their counts and links to each store', async () => {
    renderRoute('/stores')

    const table = (await screen.findByRole('link', { name: 'Demo Hardware' })).closest('table')
    expect(screen.getByRole('link', { name: 'Demo Hardware' })).toHaveAttribute('href', '/stores/store-demo')
    expect(within(table).getByText('demo-hardware')).toBeInTheDocument()
    expect(within(table).getByText('Hidden Shop')).toBeInTheDocument()
    expect(within(table).getByText(hy.catalog.status.hidden)).toBeInTheDocument()
  }, SLOW)

  it('is in the menu only for staff who can manage prices', async () => {
    signedInAs(staffMember(['materials.pricing']))
    renderRoute('/')
    expect(await screen.findByRole('menuitem', { name: new RegExp(hy.nav.stores) })).toBeInTheDocument()
  }, SLOW)

  it('is not open to staff without the permission', async () => {
    signedInAs(staffMember(['catalog.manage']))
    renderRoute('/stores')
    expect(await screen.findByText(hy.forbidden.title)).toBeInTheDocument()
  }, SLOW)

  it('filters by status and search, keeping them in the address', async () => {
    const requests = captureList()
    const user = userEvent.setup()
    const { router } = renderRoute('/stores')
    await screen.findByRole('link', { name: 'Demo Hardware' })
    expect(requests[0]).toEqual({})

    await user.click(screen.getByText(s.statuses.hidden))
    await waitFor(() => expect(router.state.location.search).toBe('?status=hidden'))
    await waitFor(() => expect(requests.at(-1)).toEqual({ isActive: 'false' }))

    await user.type(screen.getByRole('searchbox', { name: s.search }), ' demo {Enter}')
    await waitFor(() => expect(requests.at(-1)).toEqual({ search: 'demo', isActive: 'false' }))

    await user.click(screen.getByText(s.statuses.active))
    await waitFor(() => expect(requests.at(-1)).toEqual({ search: 'demo', isActive: 'true' }))

    await user.click(screen.getByText(s.statuses.all))
    await waitFor(() => expect(requests.at(-1)).toEqual({ search: 'demo' }))
  }, SLOW)

  it('adds a store and opens its page', async () => {
    const bodies = []
    server.use(
      http.post('*/api/v1/admin/stores', async ({ request }) => {
        bodies.push(await request.json())
        return HttpResponse.json(store({ id: 'store-new', slug: 'new-shop', name: 'New Shop' }))
      }),
    )
    const user = userEvent.setup()
    const { router } = renderRoute('/stores')

    await user.click(await screen.findByRole('button', { name: new RegExp(s.add) }))
    const dialog = await screen.findByRole('dialog')
    await user.type(within(dialog).getByLabelText(s.form.name), 'New Shop')
    expect(within(dialog).getByLabelText(hy.catalog.form.slug)).toHaveValue('new-shop')
    await user.type(within(dialog).getByLabelText(s.form.website), 'ftp://nope')
    await user.click(within(dialog).getByRole('button', { name: hy.catalog.form.save }))
    expect(await within(dialog).findByText(s.form.websiteInvalid)).toBeInTheDocument()
    expect(bodies).toHaveLength(0)

    await user.clear(within(dialog).getByLabelText(s.form.website))
    await user.type(within(dialog).getByLabelText(s.form.website), 'https://new.example')
    await user.click(within(dialog).getByRole('button', { name: hy.catalog.form.save }))

    await waitFor(() => expect(router.state.location.pathname).toBe('/stores/store-new'))
    expect(bodies[0]).toMatchObject({ slug: 'new-shop', name: 'New Shop', website: 'https://new.example', phone: null, logoFileId: null, isActive: true })
  }, SLOW)

  it('shows a taken slug on its field', async () => {
    server.use(http.post('*/api/v1/admin/stores', () => problem(409, 'store.slug_taken')))
    const user = userEvent.setup()
    renderRoute('/stores')

    await user.click(await screen.findByRole('button', { name: new RegExp(s.add) }))
    const dialog = await screen.findByRole('dialog')
    await user.type(within(dialog).getByLabelText(s.form.name), 'Demo')
    await user.click(within(dialog).getByRole('button', { name: hy.catalog.form.save }))

    expect(await within(dialog).findByText(hy.errors.store.slug_taken)).toBeInTheDocument()
  }, SLOW)

  it('deletes a store', async () => {
    let deleted = 0
    server.use(http.delete('*/api/v1/admin/stores/store-demo', () => {
      deleted++
      return new HttpResponse(null, { status: 204 })
    }))
    const user = userEvent.setup()
    renderRoute('/stores')

    await user.click(await screen.findByRole('button', { name: `${hy.catalog.actions.delete}: Demo Hardware` }))
    await user.click(await screen.findByRole('button', { name: hy.catalog.actions.delete }))
    await waitFor(() => expect(deleted).toBe(1))
    expect(await screen.findByText(hy.catalog.deleted)).toBeInTheDocument()
  }, SLOW)

  it('explains why a store with products can not be deleted', async () => {
    server.use(http.delete('*/api/v1/admin/stores/store-demo', () => problem(422, 'store.has_products')))
    const user = userEvent.setup()
    renderRoute('/stores')

    await user.click(await screen.findByRole('button', { name: `${hy.catalog.actions.delete}: Demo Hardware` }))
    await user.click(await screen.findByRole('button', { name: hy.catalog.actions.delete }))
    expect(await screen.findByText(hy.errors.store.has_products)).toBeInTheDocument()
  }, SLOW)

  it('shows an error when the list can not be loaded', async () => {
    server.use(http.get('*/api/v1/admin/stores', () => problem(500, 'internal')))
    renderRoute('/stores')
    expect(await screen.findByText(hy.errors.generic)).toBeInTheDocument()
  }, SLOW)
})
