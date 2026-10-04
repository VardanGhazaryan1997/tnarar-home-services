import { screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { http, HttpResponse } from 'msw'
import hy from '@/i18n/locales/hy/common.json'
import { problem, signedInAs, staffMember } from '@/test/auth'
import { renderRoute } from '@/test/renderWithProviders'
import { server } from '@/test/server'
import { userRow, usersPage } from '@/test/users'

function captureList(respond = () => HttpResponse.json(usersPage([userRow()]))) {
  const requests = []
  server.use(
    http.get('*/api/v1/admin/users', ({ request }) => {
      requests.push(Object.fromEntries(new URL(request.url).searchParams))
      return respond()
    }),
  )
  return requests
}

describe('Users page', () => {
  it('lists users with their roles, partner status and account status', async () => {
    const requests = captureList(() =>
      HttpResponse.json(
        usersPage([
          userRow(),
          userRow({ id: 'user-new', fullName: null, email: null, roles: ['Customer'], partnerStatus: null, status: 'Blocked' }),
        ]),
      ),
    )
    renderRoute('/users')

    expect(await screen.findByRole('link', { name: 'Արամ Պետրոսյան' })).toHaveAttribute('href', '/users/user-aram')
    expect(screen.getByRole('heading', { level: 1, name: hy.nav.users })).toBeInTheDocument()
    expect(screen.getByText('+37491234567', { selector: 'span' })).toBeInTheDocument()
    expect(screen.getByText('aram@example.com')).toBeInTheDocument()
    expect(screen.getByText(hy.partners.status.Approved)).toBeInTheDocument()
    expect(screen.getAllByText(hy.users.role.Customer)).toHaveLength(2)
    // A user without a name is shown by phone.
    expect(screen.getAllByRole('link', { name: '+37491234567' })[0]).toHaveAttribute('href', '/users/user-new')
    expect(screen.getByText(hy.users.noName)).toBeInTheDocument()
    expect(screen.getByText(hy.users.status.Blocked)).toBeInTheDocument()
    expect(requests[0]).toEqual({ page: '1', pageSize: '50' })
  })

  it('filters by search, role and status, keeping the filters in the address', async () => {
    const requests = captureList()
    const user = userEvent.setup()
    const { router } = renderRoute('/users')
    await screen.findByRole('link', { name: 'Արամ Պետրոսյան' })

    await user.type(screen.getByRole('searchbox', { name: hy.users.filters.search }), '091 23 {Enter}')
    await waitFor(() => expect(requests.at(-1)).toEqual({ search: '091 23', page: '1', pageSize: '50' }))

    await user.click(screen.getByRole('combobox', { name: hy.users.filters.role }))
    await user.click(await screen.findByTitle(hy.users.role.Partner))
    await waitFor(() => expect(requests.at(-1)).toEqual({ search: '091 23', role: 'Partner', page: '1', pageSize: '50' }))

    await user.click(screen.getByRole('combobox', { name: hy.users.filters.status }))
    await user.click(await screen.findByTitle(hy.users.status.Blocked))
    await waitFor(() => expect(requests.at(-1)).toEqual({ search: '091 23', role: 'Partner', status: 'Blocked', page: '1', pageSize: '50' }))
    expect(router.state.location.search).toBe('?search=091+23&role=Partner&status=Blocked')
  })

  it('pages through long lists', async () => {
    const requests = captureList(() => HttpResponse.json(usersPage([userRow()], { totalCount: 120 })))
    const user = userEvent.setup()
    const { router } = renderRoute('/users')
    await screen.findByRole('link', { name: 'Արամ Պետրոսյան' })

    await user.click(screen.getByTitle('2'))
    await waitFor(() => expect(requests.at(-1)).toEqual({ page: '2', pageSize: '50' }))
    expect(router.state.location.search).toBe('?page=2')

    await user.click(screen.getByTitle('1'))
    await waitFor(() => expect(router.state.location.search).toBe(''))
  })

  it('says when nobody matches', async () => {
    captureList(() => HttpResponse.json(usersPage([])))
    renderRoute('/users')
    expect(await screen.findByText(hy.users.empty)).toBeInTheDocument()
  })

  it('shows an error when users cannot be loaded', async () => {
    captureList(() => problem(500, 'unexpected'))
    renderRoute('/users')
    expect(await screen.findByText(hy.errors.generic)).toBeInTheDocument()
  })

  it('is closed to staff without users.view', async () => {
    signedInAs(staffMember(['partners.view']))
    renderRoute('/users')
    expect(await screen.findByText(hy.forbidden.title)).toBeInTheDocument()
  })
})
