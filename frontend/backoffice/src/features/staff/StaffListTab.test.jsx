import { screen, waitFor, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { http, HttpResponse } from 'msw'
import i18n from '@/i18n'
import hy from '@/i18n/locales/hy/common.json'
import { problem, signedInAs, staffMember } from '@/test/auth'
import { renderRoute } from '@/test/renderWithProviders'
import { server } from '@/test/server'
import { invitation, staffPage, staffRow } from '@/test/staff'

const t = (key, options) => i18n.t(key, options)

function captureList(respond = () => HttpResponse.json(staffPage([staffRow()]))) {
  const requests = []
  server.use(
    http.get('*/api/v1/admin/staff', ({ request }) => {
      requests.push(Object.fromEntries(new URL(request.url).searchParams))
      return respond()
    }),
  )
  return requests
}

function captureInvite(respond = () => HttpResponse.json(invitation(), { status: 201 })) {
  const bodies = []
  server.use(
    http.post('*/api/v1/admin/staff', async ({ request }) => {
      bodies.push(await request.json())
      return respond()
    }),
  )
  return bodies
}

async function openInviteForm(user) {
  await user.click(await screen.findByRole('button', { name: new RegExp(hy.staff.invite) }))
  return screen.findByRole('dialog')
}

describe('Staff list', () => {
  it('opens on the staff tab and lists accounts with their roles and status', async () => {
    const requests = captureList(() =>
      HttpResponse.json(
        staffPage([
          staffRow(),
          staffRow({ id: 'staff-ani', fullName: 'Ani Admin', email: 'ani@homeservices.local', isSuperAdmin: true, roles: [], lastSignInAt: null }),
          staffRow({ id: 'staff-new', fullName: 'Nare New', email: 'nare@homeservices.local', status: 'Invited', roles: [] }),
        ]),
      ),
    )
    const { router } = renderRoute('/staff')

    expect(await screen.findByRole('link', { name: 'Գայանե Սարգսյան' })).toHaveAttribute('href', '/staff/members/staff-gayane')
    expect(router.state.location.pathname).toBe('/staff/members')
    expect(screen.getByRole('heading', { level: 1, name: hy.nav.staff })).toBeInTheDocument()
    expect(screen.getByText('gayane@homeservices.local')).toBeInTheDocument()
    expect(screen.getAllByText('Operator').length).toBeGreaterThan(0)
    expect(screen.getByText(hy.staff.superAdmin)).toBeInTheDocument()
    expect(screen.getByText(hy.staff.never)).toBeInTheDocument()
    expect(screen.getByText(hy.staff.noRoles)).toBeInTheDocument()
    expect(screen.getByText(hy.staff.status.Invited)).toBeInTheDocument()
    expect(requests[0]).toEqual({ page: '1', pageSize: '50' })
  })

  it('filters by search, status and role, keeping the filters in the address', async () => {
    const requests = captureList()
    const user = userEvent.setup()
    const { router } = renderRoute('/staff/members')
    await screen.findByRole('link', { name: 'Գայանե Սարգսյան' })

    await user.type(screen.getByRole('searchbox', { name: hy.staff.filters.search }), ' Gayane {Enter}')
    await waitFor(() => expect(requests.at(-1)).toEqual({ search: 'Gayane', page: '1', pageSize: '50' }))

    await user.click(screen.getByRole('combobox', { name: hy.staff.filters.status }))
    await user.click(await screen.findByTitle(hy.staff.status.Suspended))
    await waitFor(() => expect(requests.at(-1)).toEqual({ search: 'Gayane', status: 'Suspended', page: '1', pageSize: '50' }))

    await user.click(screen.getByRole('combobox', { name: hy.staff.filters.role }))
    await user.click(await screen.findByTitle('Content editor'))
    await waitFor(() => expect(requests.at(-1)).toEqual({ search: 'Gayane', status: 'Suspended', roleId: 'role-editor', page: '1', pageSize: '50' }))
    expect(router.state.location.search).toBe('?search=Gayane&status=Suspended&roleId=role-editor')
  })

  it('pages through long lists', async () => {
    const requests = captureList(() => HttpResponse.json({ ...staffPage([staffRow()]), totalCount: 120 }))
    const user = userEvent.setup()
    const { router } = renderRoute('/staff/members')
    await screen.findByRole('link', { name: 'Գայանե Սարգսյան' })

    await user.click(screen.getByTitle('2'))
    await waitFor(() => expect(requests.at(-1)).toEqual({ page: '2', pageSize: '50' }))
    expect(router.state.location.search).toBe('?page=2')

    await user.click(screen.getByTitle('1'))
    await waitFor(() => expect(router.state.location.search).toBe(''))
  })

  it('says when nobody matches and shows load errors', async () => {
    captureList(() => HttpResponse.json(staffPage([])))
    renderRoute('/staff/members')
    expect(await screen.findByText(hy.staff.empty)).toBeInTheDocument()
  })

  it('shows an error when the list fails to load', async () => {
    captureList(() => problem(500, 'unexpected'))
    renderRoute('/staff/members')
    expect(await screen.findByText(hy.errors.generic)).toBeInTheDocument()
  })

  it('invites a staff member and shows the invitation link once', async () => {
    const bodies = captureInvite()
    const user = userEvent.setup()
    renderRoute('/staff/members')
    const dialog = await openInviteForm(user)

    await user.click(within(dialog).getByRole('button', { name: hy.staff.form.invite }))
    expect(await within(dialog).findByText(hy.auth.signIn.emailRequired)).toBeInTheDocument()
    expect(within(dialog).getByText(hy.errors.name.required)).toBeInTheDocument()

    await user.type(within(dialog).getByLabelText(hy.staff.form.email), ' gayane@homeservices.local ')
    await user.type(within(dialog).getByLabelText(hy.staff.form.fullName), ' Գայանե Սարգսյան ')
    await user.click(within(dialog).getByRole('combobox', { name: hy.staff.form.roles }))
    await user.click(await screen.findByTitle('Operator'))
    await user.click(within(dialog).getByRole('checkbox', { name: hy.staff.form.superAdmin }))
    await user.click(within(dialog).getByRole('button', { name: hy.staff.form.invite }))

    await waitFor(() =>
      expect(bodies).toEqual([{ email: 'gayane@homeservices.local', fullName: 'Գայանե Սարգսյան', roleIds: ['role-operator'], isSuperAdmin: true }]),
    )
    const linkDialog = (await screen.findByText(hy.staff.inviteLink.title)).closest('[role="dialog"]')
    expect(within(linkDialog).getByText('http://localhost:3000/accept-invite?token=tok%2Fen%2B1')).toBeInTheDocument()
    await user.click(within(linkDialog).getByRole('button', { name: hy.staff.inviteLink.done }))
  })

  it('shows "email already used" on the email field and other errors above the form', async () => {
    let response = () => problem(409, 'staff.email_taken')
    captureInvite(() => response())
    const user = userEvent.setup()
    renderRoute('/staff/members')
    const dialog = await openInviteForm(user)

    await user.type(within(dialog).getByLabelText(hy.staff.form.email), 'gayane@homeservices.local')
    await user.type(within(dialog).getByLabelText(hy.staff.form.fullName), 'Գայանե')
    await user.click(within(dialog).getByRole('button', { name: hy.staff.form.invite }))
    expect(await within(dialog).findByText(hy.errors.staff.email_taken)).toBeInTheDocument()

    response = () => HttpResponse.json({ status: 400, code: 'validation_failed', errors: { roleIds: ['roles.invalid'] } }, { status: 400 })
    await user.click(within(dialog).getByRole('button', { name: hy.staff.form.invite }))
    expect(await within(dialog).findByText(hy.errors.roles.invalid)).toBeInTheDocument()

    response = () => problem(403, 'staff.super_admin_only')
    await user.click(within(dialog).getByRole('button', { name: hy.staff.form.invite }))
    expect(await within(dialog).findByText(hy.errors.staff.super_admin_only)).toBeInTheDocument()

    await user.click(within(dialog).getByRole('button', { name: hy.catalog.form.cancel }))
  })

  it('does not offer the Super Admin option to staff managers who are not Super Admins', async () => {
    signedInAs(staffMember(['staff.view', 'staff.manage']))
    const user = userEvent.setup()
    renderRoute('/staff/members')
    const dialog = await openInviteForm(user)

    expect(within(dialog).getByLabelText(hy.staff.form.fullName)).toBeInTheDocument()
    expect(within(dialog).queryByRole('checkbox', { name: hy.staff.form.superAdmin })).not.toBeInTheDocument()
  })

  it('shows the list without the invite button to staff who can only view', async () => {
    signedInAs(staffMember(['staff.view']))
    renderRoute('/staff/members')

    await screen.findByRole('link', { name: 'Գայանե Սարգսյան' })
    expect(screen.queryByRole('button', { name: new RegExp(hy.staff.invite) })).not.toBeInTheDocument()
  })

  it('switches between the staff and roles tabs', async () => {
    const user = userEvent.setup()
    const { router } = renderRoute('/staff/members')
    await screen.findByRole('link', { name: 'Գայանե Սարգսյան' })

    await user.click(screen.getByRole('tab', { name: hy.staff.tabs.roles }))
    await waitFor(() => expect(router.state.location.pathname).toBe('/staff/roles'))
    expect(await screen.findByText('Content editor')).toBeInTheDocument()
    expect(t('roles.permissionCount', { count: 3 })).toBe(hy.roles.permissionCount.replace('{{count}}', '3'))
  })
})
