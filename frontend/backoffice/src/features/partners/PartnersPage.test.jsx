import { screen, waitFor, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { http, HttpResponse } from 'msw'
import hy from '@/i18n/locales/hy/common.json'
import { signedInAs, staffMember } from '@/test/auth'
import { page, partnerRow } from '@/test/partners'
import { renderRoute } from '@/test/renderWithProviders'
import { server } from '@/test/server'

function captureList(respond = () => HttpResponse.json(page([partnerRow()]))) {
  const requests = []
  server.use(
    http.get('*/api/v1/admin/partners', ({ request }) => {
      requests.push(Object.fromEntries(new URL(request.url).searchParams))
      return respond()
    }),
  )
  return requests
}

describe('Partners page', () => {
  it('opens on the review queue', async () => {
    const requests = captureList()
    renderRoute('/partners')

    const table = await screen.findByRole('table')
    expect(await within(table).findByRole('link', { name: 'Արամ Սանտեխնիկ' })).toHaveAttribute('href', '/partners/partner-aram')
    expect(within(table).getByText('+37477123456')).toBeInTheDocument()
    expect(within(table).getByText(hy.partners.status.UnderReview)).toBeInTheDocument()
    expect(requests[0]).toEqual({ status: 'UnderReview', page: '1', pageSize: '20' })
  })

  it('filters by status, type and search, keeping the filters in the address', async () => {
    const requests = captureList()
    const user = userEvent.setup()
    const { router } = renderRoute('/partners')
    await screen.findByRole('table')

    await user.click(screen.getByText(hy.partners.status.Approved))
    await waitFor(() => expect(router.state.location.search).toBe('?status=Approved'))

    await user.type(screen.getByRole('searchbox', { name: hy.partners.filters.search }), ' Արամ {Enter}')
    await waitFor(() => expect(requests.at(-1)).toEqual({ status: 'Approved', search: 'Արամ', page: '1', pageSize: '20' }))

    await user.click(screen.getByText(hy.partners.filters.all))
    await waitFor(() => expect(requests.at(-1)).toEqual({ search: 'Արամ', page: '1', pageSize: '20' }))
  })

  it('reads filters from the address', async () => {
    const requests = captureList()
    renderRoute('/partners?status=all&type=Company&search=Best&page=2')

    await waitFor(() => expect(requests[0]).toEqual({ type: 'Company', search: 'Best', page: '2', pageSize: '20' }))
  })

  it('filters by partner type', async () => {
    const requests = captureList()
    const user = userEvent.setup()
    renderRoute('/partners')
    await screen.findByRole('table')

    await user.click(screen.getByRole('combobox', { name: hy.partners.filters.type }))
    await user.click(await screen.findByTitle(hy.partners.type.Company))

    await waitFor(() => expect(requests.at(-1)).toEqual({ status: 'UnderReview', type: 'Company', page: '1', pageSize: '20' }))
  })

  it('pages through long lists', async () => {
    const requests = captureList(() => HttpResponse.json(page([partnerRow()], { totalCount: 45, totalPages: 3 })))
    const user = userEvent.setup()
    const { router } = renderRoute('/partners')
    await screen.findByRole('table')

    await user.click(await screen.findByTitle('2'))
    await waitFor(() => expect(requests.at(-1)).toEqual({ status: 'UnderReview', page: '2', pageSize: '20' }))
    expect(router.state.location.search).toBe('?page=2')

    await user.click(screen.getByTitle('1'))
    await waitFor(() => expect(router.state.location.search).toBe(''))
  })

  it('shows partners without an owner name or submission date', async () => {
    captureList(() => HttpResponse.json(page([partnerRow({ ownerName: null, submittedAt: null, status: 'Draft' })])))
    renderRoute('/partners?status=Draft')

    const table = await screen.findByRole('table')
    expect(await within(table).findByText('—')).toBeInTheDocument()
    expect(within(table).getByText(hy.partners.status.Draft)).toBeInTheDocument()
  })

  it('says when a filtered list is empty', async () => {
    captureList(() => HttpResponse.json(page([])))
    renderRoute('/partners?status=Rejected')

    expect(await screen.findByText(hy.partners.empty)).toBeInTheDocument()
  })

  it('says when the queue is empty', async () => {
    captureList(() => HttpResponse.json(page([])))
    renderRoute('/partners')

    expect(await screen.findByText(hy.partners.queueEmpty)).toBeInTheDocument()
  })

  it('shows an error when partners cannot be loaded', async () => {
    captureList(() => new HttpResponse(null, { status: 500 }))
    renderRoute('/partners')

    expect(await screen.findByText(hy.errors.generic)).toBeInTheDocument()
  })

  it('is closed to staff without partners.view', async () => {
    signedInAs(staffMember(['catalog.manage']))
    renderRoute('/partners')

    expect(await screen.findByText(hy.forbidden.title)).toBeInTheDocument()
  })
})
