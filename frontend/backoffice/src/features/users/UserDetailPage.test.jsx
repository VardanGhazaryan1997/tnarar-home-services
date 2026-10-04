import { screen, waitFor, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { http, HttpResponse } from 'msw'
import i18n from '@/i18n'
import hy from '@/i18n/locales/hy/common.json'
import { problem, signedInAs, staffMember } from '@/test/auth'
import { renderRoute } from '@/test/renderWithProviders'
import { server } from '@/test/server'
import { userDetail } from '@/test/users'

const t = (key, options) => i18n.t(key, options)
const NAME = 'Արամ Պետրոսյան'

async function openUser(detail = userDetail()) {
  server.use(http.get('*/api/v1/admin/users/:id', () => HttpResponse.json(detail)))
  const result = renderRoute(`/users/${detail.id}`)
  await screen.findByRole('heading', { level: 1, name: detail.fullName ?? detail.phone })
  return result
}

function captureAction(respond = () => HttpResponse.json(userDetail())) {
  const requests = []
  server.use(
    http.post('*/api/v1/admin/users/:id/:action', async ({ request, params }) => {
      requests.push({ action: params.action, body: await request.json().catch(() => null) })
      return respond()
    }),
  )
  return requests
}

const dialogTitled = async (title) => (await screen.findAllByText(title)).map((element) => element.closest('[role="dialog"]')).find(Boolean)

describe('User detail page', () => {
  it('shows contact details, roles and the partner profile', async () => {
    await openUser()

    expect(screen.getByText('+37491234567')).toBeInTheDocument()
    expect(screen.getByText('aram@example.com')).toBeInTheDocument()
    expect(screen.getByText(hy.users.role.Partner)).toBeInTheDocument()
    expect(screen.getByRole('link', { name: 'Aram Plumbing' })).toHaveAttribute('href', '/partners/partner-aram')
    expect(screen.getByText(hy.partners.status.Approved)).toBeInTheDocument()
    expect(screen.queryByText(hy.users.detail.blockReason)).not.toBeInTheDocument()
  })

  it('blocks a user with a reason', async () => {
    const requests = captureAction()
    const user = userEvent.setup()
    await openUser()

    await user.click(screen.getByRole('button', { name: hy.users.block.button }))
    const dialog = await dialogTitled(t('users.block.title', { name: NAME }))
    await user.click(within(dialog).getByRole('button', { name: hy.users.block.confirm }))
    expect(await within(dialog).findByText(hy.errors.reason.required)).toBeInTheDocument()

    await user.type(within(dialog).getByLabelText(hy.users.block.reason), ' Spam requests ')
    await user.click(within(dialog).getByRole('button', { name: hy.users.block.confirm }))

    await waitFor(() => expect(requests).toEqual([{ action: 'block', body: { reason: 'Spam requests' } }]))
    expect(await screen.findByText(hy.users.block.done)).toBeInTheDocument()
  })

  it('shows block failures on the reason field', async () => {
    let response = () => HttpResponse.json({ status: 400, code: 'validation_failed', errors: { reason: ['reason.too_long'] } }, { status: 400 })
    captureAction(() => response())
    const user = userEvent.setup()
    await openUser()

    await user.click(screen.getByRole('button', { name: hy.users.block.button }))
    const dialog = await dialogTitled(t('users.block.title', { name: NAME }))
    await user.type(within(dialog).getByLabelText(hy.users.block.reason), 'Spam')
    await user.click(within(dialog).getByRole('button', { name: hy.users.block.confirm }))
    expect(await within(dialog).findByText(hy.errors.reason.too_long)).toBeInTheDocument()

    response = () => problem(404, 'user.not_found')
    await user.click(within(dialog).getByRole('button', { name: hy.users.block.confirm }))
    expect(await within(dialog).findByText(hy.errors.user.not_found)).toBeInTheDocument()

    await user.click(within(dialog).getByRole('button', { name: hy.catalog.form.cancel }))
  })

  it('shows why a blocked user is blocked and unblocks them', async () => {
    const requests = captureAction()
    const user = userEvent.setup()
    await openUser(userDetail({ status: 'Blocked', blockReason: 'Spam requests' }))

    expect(screen.getByText('Spam requests')).toBeInTheDocument()
    expect(screen.getByText(hy.users.detail.partnerHidden)).toBeInTheDocument()

    await user.click(screen.getByRole('button', { name: hy.users.unblock.button }))
    const dialog = await dialogTitled(t('users.unblock.title', { name: NAME }))
    await user.click(within(dialog).getByRole('button', { name: hy.users.unblock.confirm }))

    await waitFor(() => expect(requests).toEqual([{ action: 'unblock', body: null }]))
    expect(await screen.findByText(hy.users.unblock.done)).toBeInTheDocument()
  })

  it('explains a failed unblock', async () => {
    captureAction(() => problem(404, 'user.not_found'))
    const user = userEvent.setup()
    await openUser(userDetail({ status: 'Blocked', blockReason: null }))

    await user.click(screen.getByRole('button', { name: hy.users.unblock.button }))
    const dialog = await dialogTitled(t('users.unblock.title', { name: NAME }))
    await user.click(within(dialog).getByRole('button', { name: hy.users.unblock.confirm }))

    expect(await screen.findByText(hy.errors.user.not_found)).toBeInTheDocument()
  })

  it('shows users without a name, email, sign-in or partner profile', async () => {
    await openUser(userDetail({ fullName: null, email: null, lastSignInAt: null, roles: ['Customer'], partner: null }))

    expect(screen.getByText(hy.staff.never)).toBeInTheDocument()
    expect(screen.queryByText(hy.users.detail.partner)).not.toBeInTheDocument()
  })

  it('is read-only for staff who can only view, without partner links if they cannot see partners', async () => {
    signedInAs(staffMember(['users.view']))
    await openUser()

    expect(screen.queryByRole('button', { name: hy.users.block.button })).not.toBeInTheDocument()
    expect(screen.getByText('Aram Plumbing')).toBeInTheDocument()
    expect(screen.queryByRole('link', { name: 'Aram Plumbing' })).not.toBeInTheDocument()
  })

  it('says when the user does not exist', async () => {
    server.use(http.get('*/api/v1/admin/users/:id', () => problem(404, 'user.not_found')))
    const user = userEvent.setup()
    const { router } = renderRoute('/users/missing')

    expect(await screen.findByText(hy.errors.user.not_found)).toBeInTheDocument()
    await user.click(screen.getByRole('link', { name: new RegExp(hy.users.detail.back) }))
    await waitFor(() => expect(router.state.location.pathname).toBe('/users'))
  })
})
