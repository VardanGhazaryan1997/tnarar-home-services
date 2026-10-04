import { screen, waitFor, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { http, HttpResponse } from 'msw'
import hy from '@/i18n/locales/hy/common.json'
import { problem, signedInAs, staffMember } from '@/test/auth'
import { recipient, requestDetail, requestRow } from '@/test/operations'
import { page, partnerRow } from '@/test/partners'
import { renderRoute } from '@/test/renderWithProviders'
import { server } from '@/test/server'

function captureList() {
  const requests = []
  server.use(
    http.get('*/api/v1/admin/requests', ({ request }) => {
      requests.push(Object.fromEntries(new URL(request.url).searchParams))
      return HttpResponse.json(page([requestRow()]))
    }),
  )
  return requests
}

// Several dialogs in one test: slow machines need more than the default 60 s.
const SLOW = 180_000

describe('Requests page', () => {
  it('opens on the operator queue and filters by status, type and search', async () => {
    const requests = captureList()
    const user = userEvent.setup()
    const { router } = renderRoute('/requests')

    const table = await screen.findByRole('table')
    expect(await within(table).findByRole('link', { name: 'Ծորակը կաթում է' })).toHaveAttribute('href', '/requests/request-1')
    expect(within(table).getByText(hy.requests.attention.NoMatchingPartners)).toBeInTheDocument()
    expect(requests[0]).toEqual({ needsAttention: 'true', page: '1', pageSize: '20' })

    await user.click(screen.getByText(hy.requests.status.Cancelled))
    await waitFor(() => expect(requests.at(-1)).toEqual({ status: 'Cancelled', page: '1', pageSize: '20' }))
    expect(router.state.location.search).toBe('?show=Cancelled')

    await user.click(screen.getByRole('combobox', { name: hy.requests.filters.kind }))
    await user.click(await screen.findByTitle(hy.requests.kind.Direct))
    await user.type(screen.getByRole('searchbox', { name: hy.requests.filters.search }), '091{Enter}')
    await waitFor(() => expect(requests.at(-1)).toEqual({ status: 'Cancelled', kind: 'Direct', search: '091', page: '1', pageSize: '20' }))

    await user.click(screen.getByText(hy.requests.filters.all))
    await waitFor(() => expect(requests.at(-1)).toEqual({ kind: 'Direct', search: '091', page: '1', pageSize: '20' }))
  })

  it('pages through the list and shows errors', async () => {
    const requests = []
    server.use(
      http.get('*/api/v1/admin/requests', ({ request }) => {
        requests.push(Object.fromEntries(new URL(request.url).searchParams))
        return requests.length > 2 ? problem(500, 'boom') : HttpResponse.json(page([requestRow()], { totalCount: 45, totalPages: 3 }))
      }),
    )
    const user = userEvent.setup()
    renderRoute('/requests?show=Open')
    await screen.findByRole('table')
    await user.click(await screen.findByTitle('2'))
    await waitFor(() => expect(requests.at(-1)).toEqual({ status: 'Open', page: '2', pageSize: '20' }))
    await user.click(await screen.findByTitle('3'))
    expect(await screen.findByText(hy.errors.generic)).toBeInTheDocument()
  })
})

describe('Request detail', () => {
  it('shows the request, the customer and who received it', async () => {
    server.use(
      http.get('*/api/v1/admin/requests/:id', () =>
        HttpResponse.json(requestDetail({ recipients: [recipient()], media: [{ id: 'f1', fileName: 'tap.jpg', url: 'https://storage.test/f1', thumbnailUrl: null }], budgetMax: null })),
      ),
    )
    renderRoute('/requests/request-1')

    expect(await screen.findByRole('heading', { level: 1, name: 'Սանտեխնիկա · Երևան' })).toBeInTheDocument()
    expect(screen.getByText('Ծորակը կաթում է, պետք է փոխել։')).toBeInTheDocument()
    expect(screen.getByText('+37491000111')).toBeInTheDocument()
    expect(screen.getByRole('link', { name: 'Արամ Սանտեխնիկ' })).toHaveAttribute('href', '/partners/partner-aram')
    expect(screen.getByText('Զբաղված եմ')).toBeInTheDocument()
    expect(screen.getByRole('img', { name: 'tap.jpg' })).toBeInTheDocument()
  })

  it('sends the request to chosen partners', async () => {
    const sent = []
    server.use(
      http.get('*/api/v1/admin/partners', ({ request }) => {
        expect(new URL(request.url).searchParams.get('status')).toBe('Approved')
        return HttpResponse.json(page([partnerRow({ id: 'partner-new', displayName: 'Նոր Վարպետ' })]))
      }),
      http.post('*/api/v1/admin/requests/:id/recipients', async ({ request }) => {
        sent.push(await request.json())
        return HttpResponse.json(requestDetail())
      }),
    )
    const user = userEvent.setup()
    renderRoute('/requests/request-1')

    await user.click(await screen.findByRole('button', { name: hy.requests.assign.button }))
    const dialog = await screen.findByRole('dialog')
    await user.click(within(dialog).getByRole('button', { name: hy.requests.assign.confirm }))
    expect(await within(dialog).findByText(hy.requests.assign.required)).toBeInTheDocument()

    await user.click(within(dialog).getByRole('combobox'))
    await user.click(await screen.findByTitle('Նոր Վարպետ · +37477123456'))
    await user.click(within(dialog).getByRole('button', { name: hy.requests.assign.confirm }))
    await waitFor(() => expect(sent).toEqual([{ partnerIds: ['partner-new'] }]))
  }, SLOW)

  it('shows why partners could not be added', async () => {
    server.use(http.post('*/api/v1/admin/requests/:id/recipients', () => problem(422, 'request.partner_unavailable')))
    const user = userEvent.setup()
    renderRoute('/requests/request-1')

    await user.click(await screen.findByRole('button', { name: hy.requests.assign.button }))
    const dialog = await screen.findByRole('dialog')
    await user.click(within(dialog).getByRole('combobox'))
    await user.click(await screen.findByTitle('Արամ Սանտեխնիկ · +37477123456'))
    await user.click(within(dialog).getByRole('button', { name: hy.requests.assign.confirm }))
    expect(await within(dialog).findByText(hy.errors.request.partner_unavailable)).toBeInTheDocument()
    await user.click(within(dialog).getByRole('button', { name: hy.common.cancel }))
  }, SLOW)

  it('cancels the request with a reason', async () => {
    const bodies = []
    server.use(
      http.post('*/api/v1/admin/requests/:id/cancel', async ({ request }) => {
        bodies.push(await request.json())
        return bodies.length === 1 ? HttpResponse.json(requestDetail({ status: 'Cancelled' })) : problem(422, 'request.not_open')
      }),
    )
    const user = userEvent.setup()
    renderRoute('/requests/request-1')

    await user.click(await screen.findByRole('button', { name: hy.requests.cancel.button }))
    let dialog = (await screen.findAllByRole('dialog')).at(-1)
    await user.click(within(dialog).getByRole('button', { name: hy.requests.cancel.confirm }))
    expect(await within(dialog).findByText(hy.common.reasonRequired)).toBeInTheDocument()
    await user.type(within(dialog).getByRole('textbox'), 'Կրկնօրինակ')
    await user.click(within(dialog).getByRole('button', { name: hy.requests.cancel.confirm }))
    await waitFor(() => expect(bodies).toEqual([{ reason: 'Կրկնօրինակ' }]))
    expect(await screen.findByText(hy.requests.cancel.done)).toBeInTheDocument()

    await user.click(screen.getByRole('button', { name: hy.requests.cancel.button }))
    dialog = (await screen.findAllByRole('dialog')).at(-1)
    await user.type(within(dialog).getByRole('textbox'), 'Կրկին')
    await user.click(within(dialog).getByRole('button', { name: hy.requests.cancel.confirm }))
    expect(await screen.findByText(hy.errors.request.not_open)).toBeInTheDocument()
  }, SLOW)

  it('hides actions from staff who can only view, and shows load errors', async () => {
    signedInAs(staffMember(['requests.view']))
    renderRoute('/requests/request-1')
    expect(await screen.findByText('Ծորակը կաթում է, պետք է փոխել։')).toBeInTheDocument()
    expect(screen.queryByRole('button', { name: hy.requests.assign.button })).not.toBeInTheDocument()
  })

  it('says when the request does not exist', async () => {
    server.use(http.get('*/api/v1/admin/requests/:id', () => problem(404, 'request.not_found')))
    renderRoute('/requests/missing')
    expect(await screen.findByText(hy.errors.request.not_found)).toBeInTheDocument()
  })
})
