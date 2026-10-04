import { fireEvent, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { http, HttpResponse } from 'msw'
import i18n from '@/i18n'
import hy from '@/i18n/locales/hy/common.json'
import { AUDIT_ENTRIES, auditPage } from '@/test/audit'
import { problem, signedInAs, staffMember } from '@/test/auth'
import { renderRoute } from '@/test/renderWithProviders'
import { server } from '@/test/server'

const t = (key, options) => i18n.t(key, options)

function captureList(respond = () => HttpResponse.json(auditPage(AUDIT_ENTRIES))) {
  const requests = []
  server.use(
    http.get('*/api/v1/admin/audit-log', ({ request }) => {
      requests.push(Object.fromEntries(new URL(request.url).searchParams))
      return respond()
    }),
  )
  return requests
}

async function openLog(url = '/audit') {
  const result = renderRoute(url)
  await screen.findByText('Ani Admin')
  return result
}

describe('Audit log', () => {
  it('lists changes newest first with who, what and how many fields changed', async () => {
    const requests = captureList()
    await openLog()

    expect(screen.getByRole('heading', { level: 1, name: hy.nav.audit })).toBeInTheDocument()
    expect(requests[0]).toEqual({ page: '1', pageSize: '50' })
    expect(screen.getByText(hy.audit.actions.Updated)).toBeInTheDocument()
    expect(screen.getByText(hy.audit.actions.Created)).toBeInTheDocument()
    expect(screen.getByText(hy.audit.actions.Deleted)).toBeInTheDocument()
    expect(screen.getByRole('link', { name: hy.audit.entities.PartnerProfile })).toHaveAttribute('href', '/partners/partner-aram')
    // Entities without a Back Office page aren't links; unknown types show their server name.
    expect(screen.getByText(hy.audit.entities.PartnerService)).not.toHaveAttribute('href')
    expect(screen.getByText('OtpCode')).toBeInTheDocument()
    // Portal user without a name shows the id; system changes say so.
    expect(screen.getByText('user-aram')).toBeInTheDocument()
    expect(screen.getByText(hy.audit.actorTypes.System)).toBeInTheDocument()
    expect(screen.getByText(t('audit.changeCount', { n: 2 }))).toBeInTheDocument()
  })

  it('shows old and new values on demand', async () => {
    captureList()
    const user = userEvent.setup()
    const { container } = await openLog()

    await user.click(container.querySelector('.ant-table-row-expand-icon:not(.ant-table-row-expand-icon-spaced)'))
    expect(await screen.findByText('Status')).toBeInTheDocument()
    expect(screen.getByText('UnderReview')).toBeInTheDocument()
    expect(screen.getByText('Approved')).toBeInTheDocument()
    expect(screen.getByText('Add a licence')).toBeInTheDocument()
    expect(screen.getAllByText('—').length).toBeGreaterThan(0)
  })

  it('filters by record type, record id and action, keeping the filters in the address', async () => {
    const requests = captureList()
    const user = userEvent.setup()
    const { router } = await openLog()

    await user.click(screen.getByRole('combobox', { name: hy.audit.filters.entityType }))
    await user.click(await screen.findByTitle(hy.audit.entities.User))
    await waitFor(() => expect(requests.at(-1)).toEqual({ entityType: 'User', page: '1', pageSize: '50' }))

    await user.type(screen.getByRole('searchbox', { name: hy.audit.filters.entityId }), ' user-aram {Enter}')
    await waitFor(() => expect(requests.at(-1)).toEqual({ entityType: 'User', entityId: 'user-aram', page: '1', pageSize: '50' }))

    await user.click(screen.getByRole('combobox', { name: hy.audit.filters.action }))
    await user.click(await screen.findByTitle(hy.audit.actions.Deleted))
    await waitFor(() => expect(requests.at(-1)).toEqual({ entityType: 'User', entityId: 'user-aram', action: 'Deleted', page: '1', pageSize: '50' }))
    expect(router.state.location.search).toBe('?entityType=User&entityId=user-aram&action=Deleted')
  })

  it('reads a period from the address and can clear it', async () => {
    const requests = captureList()
    const { router, container } = await openLog('/audit?from=2026-10-01T00:00:00.000Z&to=2026-10-02T23:59:59.999Z')

    expect(requests[0]).toEqual({ from: '2026-10-01T00:00:00.000Z', to: '2026-10-02T23:59:59.999Z', page: '1', pageSize: '50' })
    const picker = container.querySelector('.ant-picker-range')
    // The clear icon only takes clicks on hover (CSS), which jsdom doesn't apply.
    fireEvent.mouseDown(picker.querySelector('.ant-picker-clear'))
    fireEvent.mouseUp(picker.querySelector('.ant-picker-clear'))
    fireEvent.click(picker.querySelector('.ant-picker-clear'))
    await waitFor(() => expect(router.state.location.search).toBe(''))
  })

  it('pages through long logs', async () => {
    const requests = captureList(() => HttpResponse.json(auditPage(AUDIT_ENTRIES, { totalCount: 120 })))
    const user = userEvent.setup()
    const { router } = await openLog()

    await user.click(screen.getByTitle('2'))
    await waitFor(() => expect(requests.at(-1)).toEqual({ page: '2', pageSize: '50' }))
    await user.click(screen.getByTitle('1'))
    await waitFor(() => expect(router.state.location.search).toBe(''))
  })

  it('shows load errors and an empty log', async () => {
    captureList(() => problem(400, 'range.invalid'))
    renderRoute('/audit')

    expect(await screen.findByText(hy.errors.range.invalid)).toBeInTheDocument()
    expect(screen.getByText(hy.audit.empty)).toBeInTheDocument()
  })

  it('is closed to staff without audit.view', async () => {
    signedInAs(staffMember(['users.view']))
    renderRoute('/audit')
    expect(await screen.findByText(hy.forbidden.title)).toBeInTheDocument()
  })
})
