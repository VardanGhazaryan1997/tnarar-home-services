import { screen, waitFor, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { http, HttpResponse } from 'msw'
import i18n from '@/i18n'
import hy from '@/i18n/locales/hy/common.json'
import { problem, signedInAs, staffMember } from '@/test/auth'
import { renderRoute } from '@/test/renderWithProviders'
import { server } from '@/test/server'
import { userRow, usersPage } from '@/test/users'

const t = (key, options) => i18n.t(key, options)

// The card holding a counter's title.
const counter = (title) => screen.getByText(title).closest('.ant-card')

async function openDashboard() {
  const result = renderRoute('/')
  await screen.findByRole('heading', { level: 1, name: hy.dashboard.title })
  return result
}

describe('Dashboard', () => {
  it('shows Super Admins every counter and the recent changes', async () => {
    server.use(
      http.get('*/api/v1/admin/users', ({ request }) =>
        HttpResponse.json(usersPage([userRow()], { totalCount: new URL(request.url).searchParams.get('status') === 'Blocked' ? 2 : 57 })),
      ),
    )
    await openDashboard()

    expect(screen.getByText(t('dashboard.greeting', { name: 'Ani Admin' }))).toBeInTheDocument()
    await waitFor(() => expect(within(counter(hy.dashboard.counters.users)).getByText('57')).toBeInTheDocument())
    expect(within(counter(hy.dashboard.counters.blockedUsers)).getByText('2')).toBeInTheDocument()
    expect(within(counter(hy.dashboard.counters.pendingPartners)).getByText('1')).toBeInTheDocument()
    expect(within(counter(hy.dashboard.counters.invitedStaff)).getByText('1')).toBeInTheDocument()
    // One draft page in the fixtures; missing translations 0 + 1 + 2 + 4.
    expect(within(counter(hy.dashboard.counters.draftPages)).getByText('1')).toBeInTheDocument()
    expect(within(counter(hy.dashboard.counters.missingTexts)).getByText('7')).toBeInTheDocument()

    const activity = screen.getByText(hy.dashboard.recentActivity).closest('.ant-card')
    expect(await within(activity).findByText('Ani Admin')).toBeInTheDocument()
    expect(within(activity).getByRole('link', { name: hy.audit.entities.PartnerProfile })).toHaveAttribute('href', '/partners/partner-aram')
    expect(within(activity).getByRole('link', { name: hy.dashboard.allActivity })).toHaveAttribute('href', '/audit')
  })

  it('links each counter to the list behind it', async () => {
    const user = userEvent.setup()
    const { router } = await openDashboard()

    await waitFor(() => expect(within(counter(hy.dashboard.counters.blockedUsers)).getByText('1')).toBeInTheDocument())
    await user.click(within(counter(hy.dashboard.counters.blockedUsers)).getByRole('link', { name: new RegExp(hy.dashboard.open) }))
    await waitFor(() => expect(`${router.state.location.pathname}${router.state.location.search}`).toBe('/users?status=Blocked'))
  })

  it('shows only the counters a staff member may open', async () => {
    signedInAs(staffMember(['partners.view']))
    await openDashboard()

    expect(screen.getByText(hy.dashboard.counters.pendingPartners)).toBeInTheDocument()
    expect(screen.queryByText(hy.dashboard.counters.users)).not.toBeInTheDocument()
    expect(screen.queryByText(hy.dashboard.recentActivity)).not.toBeInTheDocument()
  })

  it('welcomes staff without any counters', async () => {
    signedInAs(staffMember([]))
    await openDashboard()

    expect(screen.getByText(hy.dashboard.welcome)).toBeInTheDocument()
  })

  it('shows a dash when a counter cannot be loaded', async () => {
    signedInAs(staffMember(['users.view']))
    server.use(http.get('*/api/v1/admin/users', () => problem(500, 'unexpected')))
    await openDashboard()

    await waitFor(() => expect(within(counter(hy.dashboard.counters.users)).getByText('—')).toBeInTheDocument())
  })
})
