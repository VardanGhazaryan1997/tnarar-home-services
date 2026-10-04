import { screen, waitFor, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { http, HttpResponse } from 'msw'
import i18n from '@/i18n'
import hy from '@/i18n/locales/hy/common.json'
import { problem, signedInAs, staffMember } from '@/test/auth'
import { renderRoute } from '@/test/renderWithProviders'
import { server } from '@/test/server'
import { ROLES } from '@/test/staff'

const t = (key, options) => i18n.t(key, options)

function capture(method, path, respond) {
  const requests = []
  server.use(
    http[method](`*/api/v1/admin/${path}`, async ({ request }) => {
      requests.push({ url: new URL(request.url).pathname, body: await request.json().catch(() => null) })
      return respond()
    }),
  )
  return requests
}

async function openTab() {
  const result = renderRoute('/staff/roles')
  await screen.findByText('Content editor')
  return result
}

const dialogTitled = async (title) =>
  (await screen.findAllByText(title)).map((element) => element.closest('[role="dialog"]')).find(Boolean)

describe('Roles tab', () => {
  it('lists roles, marks built-in ones and shows their permissions on demand', async () => {
    const user = userEvent.setup()
    const { container } = await openTab()

    expect(screen.getByText('Operator')).toBeInTheDocument()
    expect(screen.getByText(hy.roles.builtIn)).toBeInTheDocument()
    expect(screen.getByText('Handles partners and users')).toBeInTheDocument()
    expect(screen.getByText(t('roles.permissionCount', { count: 3 }))).toBeInTheDocument()
    expect(screen.getByText(hy.roles.help)).toBeInTheDocument()
    // Built-in roles can't be deleted.
    expect(screen.queryByLabelText(t('roles.deleteAria', { name: 'Operator' }))).not.toBeInTheDocument()

    await user.click(container.querySelector('.ant-table-row-expand-icon'))
    expect(await screen.findByText(hy.permissions.partners.approve)).toBeInTheDocument()
  })

  it('adds a role with grantable permissions only', async () => {
    const requests = capture('post', 'roles', () => HttpResponse.json({ ...ROLES[1], id: 'role-new' }, { status: 201 }))
    const user = userEvent.setup()
    await openTab()

    await user.click(screen.getByRole('button', { name: new RegExp(hy.roles.add) }))
    const dialog = await dialogTitled(hy.roles.form.addTitle)
    expect(await within(dialog).findByRole('checkbox', { name: hy.permissions.partners.view })).toBeInTheDocument()
    expect(within(dialog).queryByRole('checkbox', { name: hy.permissions.roles.manage })).not.toBeInTheDocument()
    expect(within(dialog).getByText(hy.permissionGroups.users)).toBeInTheDocument()

    await user.click(within(dialog).getByRole('button', { name: hy.catalog.form.save }))
    expect(await within(dialog).findByText(hy.errors.name.required)).toBeInTheDocument()

    await user.type(within(dialog).getByLabelText(hy.roles.form.name), ' Support ')
    await user.click(within(dialog).getByRole('checkbox', { name: hy.permissions.users.view }))
    await user.click(within(dialog).getByRole('checkbox', { name: hy.permissions.users.block }))
    await user.click(within(dialog).getByRole('button', { name: hy.catalog.form.save }))

    await waitFor(() =>
      expect(requests).toEqual([{ url: '/api/v1/admin/roles', body: { name: 'Support', description: null, permissions: ['users.view', 'users.block'] } }]),
    )
    expect(await screen.findByText(hy.catalog.created)).toBeInTheDocument()
  })

  it('edits a built-in role without renaming it', async () => {
    const requests = capture('put', 'roles/:id', () => HttpResponse.json(ROLES[0]))
    const user = userEvent.setup()
    await openTab()

    await user.click(screen.getByLabelText(t('roles.editAria', { name: 'Operator' })))
    const dialog = await dialogTitled(t('roles.form.editTitle', { name: 'Operator' }))
    expect(within(dialog).getByLabelText(hy.roles.form.name)).toBeDisabled()
    expect(within(dialog).getByText(hy.roles.form.systemName)).toBeInTheDocument()
    await user.click(await within(dialog).findByRole('checkbox', { name: hy.permissions.users.view }))
    await user.type(within(dialog).getByLabelText(hy.roles.form.description), '!')
    await user.click(within(dialog).getByRole('button', { name: hy.catalog.form.save }))

    await waitFor(() =>
      expect(requests).toEqual([
        {
          url: '/api/v1/admin/roles/role-operator',
          body: { name: 'Operator', description: 'Handles partners and users!', permissions: ['partners.view', 'partners.approve'] },
        },
      ]),
    )
    expect(await screen.findByText(hy.catalog.saved)).toBeInTheDocument()
  })

  it('shows "name already used" on the name field and other errors above the form', async () => {
    let response = () => problem(409, 'role.name_taken')
    capture('put', 'roles/:id', () => response())
    const user = userEvent.setup()
    await openTab()

    await user.click(screen.getByLabelText(t('roles.editAria', { name: 'Content editor' })))
    const dialog = await dialogTitled(t('roles.form.editTitle', { name: 'Content editor' }))
    await user.click(within(dialog).getByRole('button', { name: hy.catalog.form.save }))
    expect(await within(dialog).findByText(hy.errors.role.name_taken)).toBeInTheDocument()

    response = () => HttpResponse.json({ status: 400, code: 'validation_failed', errors: { permissions: ['permissions.super_admin_only'] } }, { status: 400 })
    await user.click(within(dialog).getByRole('button', { name: hy.catalog.form.save }))
    expect(await within(dialog).findByText(hy.errors.permissions.super_admin_only)).toBeInTheDocument()

    response = () => problem(404, 'role.not_found')
    await user.click(within(dialog).getByRole('button', { name: hy.catalog.form.save }))
    expect(await within(dialog).findByText(hy.errors.role.not_found)).toBeInTheDocument()

    await user.click(within(dialog).getByRole('button', { name: hy.catalog.form.cancel }))
  })

  it.each([
    ['deletes a role after confirmation', () => new HttpResponse(null, { status: 204 }), hy.roles.deleted],
    ['explains when a role is still in use', () => problem(409, 'role.in_use'), hy.errors.role.in_use],
  ])('%s', async (_, respond, expected) => {
    const requests = capture('delete', 'roles/:id', respond)
    const user = userEvent.setup()
    await openTab()

    await user.click(screen.getByLabelText(t('roles.deleteAria', { name: 'Content editor' })))
    const confirm = await screen.findByRole('tooltip')
    await user.click(within(confirm).getByRole('button', { name: hy.catalog.actions.delete }))

    expect(await screen.findByText(expected)).toBeInTheDocument()
    expect(requests.map((r) => r.url)).toEqual(['/api/v1/admin/roles/role-editor'])
  })

  it('is read-only for staff who are not Super Admins', async () => {
    signedInAs(staffMember(['staff.view', 'staff.manage']))
    await openTab()

    expect(screen.getByText(hy.roles.viewOnly)).toBeInTheDocument()
    expect(screen.queryByRole('button', { name: new RegExp(hy.roles.add) })).not.toBeInTheDocument()
    expect(screen.queryByLabelText(t('roles.editAria', { name: 'Operator' }))).not.toBeInTheDocument()
  })

  it('shows load errors and an empty list', async () => {
    server.use(http.get('*/api/v1/admin/roles', () => problem(500, 'unexpected')))
    renderRoute('/staff/roles')

    expect(await screen.findByText(hy.errors.generic)).toBeInTheDocument()
    expect(screen.getByText(hy.roles.empty)).toBeInTheDocument()
  })
})
