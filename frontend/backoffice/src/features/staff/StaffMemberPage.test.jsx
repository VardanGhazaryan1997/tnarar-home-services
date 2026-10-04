import { screen, waitFor, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { http, HttpResponse } from 'msw'
import i18n from '@/i18n'
import hy from '@/i18n/locales/hy/common.json'
import { problem, SUPER_ADMIN, signedInAs, staffMember } from '@/test/auth'
import { renderRoute } from '@/test/renderWithProviders'
import { server } from '@/test/server'
import { invitation, staffDetail, staffRow } from '@/test/staff'

const t = (key, options) => i18n.t(key, options)
const NAME = 'Գայանե Սարգսյան'

async function openMember(detail = staffDetail()) {
  server.use(http.get('*/api/v1/admin/staff/:id', () => HttpResponse.json(detail)))
  const result = renderRoute(`/staff/members/${detail.member.id}`)
  await screen.findByRole('heading', { level: 1, name: detail.member.fullName })
  return result
}

function captureActions(respond = () => HttpResponse.json(staffRow())) {
  const requests = []
  server.use(
    http.all('*/api/v1/admin/staff/:id/:action', async ({ request, params }) => {
      requests.push(`${request.method} ${params.action}`)
      return respond(params.action)
    }),
  )
  return requests
}

const dialogTitled = async (title) => (await screen.findAllByText(title))[0].closest('[role="dialog"]')

async function confirmAction(user, action, name = NAME) {
  await user.click(screen.getByRole('button', { name: hy.staff.actions[action].button }))
  const dialog = await dialogTitled(t(`staff.actions.${action}.title`, { name }))
  await user.click(within(dialog).getByRole('button', { name: hy.staff.actions[action].confirm }))
}

describe('Staff member page', () => {
  it('shows the account, roles and effective permissions', async () => {
    await openMember()

    expect(screen.getByText('gayane@homeservices.local')).toBeInTheDocument()
    expect(screen.getByText(hy.staff.status.Active)).toBeInTheDocument()
    expect(screen.getByText(hy.staff.detail.twoFactorOn)).toBeInTheDocument()
    expect(screen.getByText('Operator')).toBeInTheDocument()
    expect(within(screen.getByText(hy.staff.detail.permissions).closest('.ant-card')).getByText(hy.permissionGroups.partners)).toBeInTheDocument()
    expect(screen.getByText(hy.permissions.partners.approve)).toBeInTheDocument()
    expect(screen.getByText(hy.permissions.users.view)).toBeInTheDocument()
    expect(screen.queryByText(hy.staff.detail.inviteExpires)).not.toBeInTheDocument()
  })

  it('shows invitation details and lets a manager create a new link', async () => {
    const requests = captureActions(() => HttpResponse.json(invitation()))
    const user = userEvent.setup()
    await openMember(staffDetail({ status: 'Invited', twoFactorEnabled: false, lastSignInAt: null, inviteExpiresAt: '2026-10-10T09:00:00Z', roles: [] }, []))

    expect(screen.getByText(hy.staff.detail.inviteExpires)).toBeInTheDocument()
    expect(screen.getByText(hy.staff.detail.twoFactorOff)).toBeInTheDocument()
    expect(screen.getAllByText(hy.staff.never).length).toBeGreaterThan(0)
    expect(screen.getAllByText(hy.staff.noRoles).length).toBeGreaterThan(0)
    expect(screen.getByText(hy.staff.noPermissions)).toBeInTheDocument()
    expect(screen.queryByRole('button', { name: hy.staff.actions.resetTwoFactor.button })).not.toBeInTheDocument()

    await confirmAction(user, 'renewInvite')

    await waitFor(() => expect(requests).toEqual(['POST invite']))
    expect(await screen.findByText('http://localhost:3000/accept-invite?token=tok%2Fen%2B1')).toBeInTheDocument()
    await user.click(screen.getByRole('button', { name: hy.staff.inviteLink.done }))
  })

  it.each([
    ['suspend', 'POST suspend', {}],
    ['activate', 'POST activate', { status: 'Suspended' }],
    ['resetTwoFactor', 'POST reset-two-factor', {}],
    ['grantSuperAdmin', 'POST super-admin', {}],
    ['revokeSuperAdmin', 'DELETE super-admin', { isSuperAdmin: true }],
  ])('%s after confirmation', async (action, request, overrides) => {
    const requests = captureActions()
    const user = userEvent.setup()
    await openMember(staffDetail(overrides))

    await confirmAction(user, action)

    await waitFor(() => expect(requests).toEqual([request]))
    expect(await screen.findByText(hy.staff.actions[action].done)).toBeInTheDocument()
  })

  it('explains why an action was refused', async () => {
    captureActions(() => problem(409, 'staff.last_super_admin'))
    const user = userEvent.setup()
    await openMember(staffDetail({ isSuperAdmin: true }))

    expect(screen.getByText(hy.staff.detail.allPermissions)).toBeInTheDocument()
    await confirmAction(user, 'revokeSuperAdmin')

    expect(await screen.findByText(hy.errors.staff.last_super_admin)).toBeInTheDocument()
  })

  it('changes the name and roles', async () => {
    const bodies = []
    server.use(
      http.put('*/api/v1/admin/staff/:id', async ({ request }) => {
        bodies.push(await request.json())
        return HttpResponse.json(staffRow())
      }),
    )
    const user = userEvent.setup()
    await openMember()

    await user.click(screen.getByRole('button', { name: hy.staff.detail.edit }))
    const dialog = await dialogTitled(hy.staff.form.editTitle)
    const name = within(dialog).getByLabelText(hy.staff.form.fullName)
    await user.clear(name)
    await user.type(name, 'Գայանե Ս.')
    await user.click(within(dialog).getByRole('combobox', { name: hy.staff.form.roles }))
    await user.click(await screen.findByTitle('Content editor'))
    await user.click(within(dialog).getByRole('button', { name: hy.staff.form.save }))

    await waitFor(() => expect(bodies).toEqual([{ fullName: 'Գայանե Ս.', roleIds: ['role-operator', 'role-editor'] }]))
    expect(await screen.findByText(hy.staff.form.saved)).toBeInTheDocument()
  })

  it('does not let staff suspend themselves', async () => {
    await openMember(staffDetail({ id: SUPER_ADMIN.id, fullName: SUPER_ADMIN.fullName }))

    expect(screen.getByRole('button', { name: hy.staff.detail.edit })).toBeInTheDocument()
    expect(screen.queryByRole('button', { name: hy.staff.actions.suspend.button })).not.toBeInTheDocument()
  })

  it('offers managers who are not Super Admins only the actions they may take', async () => {
    signedInAs(staffMember(['staff.view', 'staff.manage']))
    await openMember()

    expect(screen.getByRole('button', { name: hy.staff.actions.suspend.button })).toBeInTheDocument()
    expect(screen.queryByRole('button', { name: hy.staff.actions.grantSuperAdmin.button })).not.toBeInTheDocument()
  })

  it("doesn't let managers who are not Super Admins change a Super Admin", async () => {
    signedInAs(staffMember(['staff.view', 'staff.manage']))
    await openMember(staffDetail({ isSuperAdmin: true }))

    expect(screen.queryByRole('button', { name: hy.staff.detail.edit })).not.toBeInTheDocument()
  })

  it('is read-only for staff who can only view', async () => {
    signedInAs(staffMember(['staff.view']))
    await openMember()

    expect(screen.queryByRole('button', { name: hy.staff.detail.edit })).not.toBeInTheDocument()
  })

  it('says when the staff member does not exist', async () => {
    server.use(http.get('*/api/v1/admin/staff/:id', () => problem(404, 'staff.not_found')))
    const user = userEvent.setup()
    const { router } = renderRoute('/staff/members/missing')

    expect(await screen.findByText(hy.errors.staff.not_found)).toBeInTheDocument()
    await user.click(screen.getByRole('link', { name: new RegExp(hy.staff.detail.back) }))
    await waitFor(() => expect(router.state.location.pathname).toBe('/staff/members'))
  })
})
