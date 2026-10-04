import { screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { http, HttpResponse } from 'msw'
import hy from '@/i18n/locales/hy/common.json'
import { problem, signedOutBrowser } from '@/test/auth'
import { renderRoute } from '@/test/renderWithProviders'
import { server } from '@/test/server'

const PASSWORD = 'correct horse battery'

function captureAccept(respond = () => new HttpResponse(null, { status: 204 })) {
  const bodies = []
  server.use(
    http.post('*/api/v1/admin/auth/accept-invite', async ({ request }) => {
      bodies.push(await request.json())
      return respond()
    }),
  )
  return bodies
}

async function choosePassword(user, password = PASSWORD, confirm = password) {
  await user.type(await screen.findByLabelText(hy.auth.acceptInvite.password), password)
  await user.type(screen.getByLabelText(hy.auth.acceptInvite.confirm), confirm)
  await user.click(screen.getByRole('button', { name: hy.auth.acceptInvite.submit }))
}

describe('Accept invitation page', () => {
  beforeEach(() => signedOutBrowser())

  it('saves the new password and sends the staff member to sign in', async () => {
    const bodies = captureAccept()
    const user = userEvent.setup()
    const { router } = renderRoute('/accept-invite?token=tok%2Fen%2B1')

    await choosePassword(user)

    await waitFor(() => expect(bodies).toEqual([{ token: 'tok/en+1', password: PASSWORD }]))
    expect(await screen.findByText(hy.auth.acceptInvite.doneTitle)).toBeInTheDocument()
    await user.click(screen.getByRole('button', { name: hy.auth.signIn.submit }))
    await waitFor(() => expect(router.state.location.pathname).toBe('/login'))
  })

  it('checks the password length and that both passwords match', async () => {
    const bodies = captureAccept()
    const user = userEvent.setup()
    renderRoute('/accept-invite?token=abc')

    await choosePassword(user, 'short', 'shorter')

    expect(await screen.findByText(hy.errors.password.too_short)).toBeInTheDocument()
    expect(screen.getByText(hy.auth.acceptInvite.mismatch)).toBeInTheDocument()
    expect(bodies).toEqual([])
  })

  it('needs the password twice', async () => {
    const user = userEvent.setup()
    renderRoute('/accept-invite?token=abc')

    await user.click(await screen.findByRole('button', { name: hy.auth.acceptInvite.submit }))

    expect(await screen.findByText(hy.auth.signIn.passwordRequired)).toBeInTheDocument()
    expect(screen.getByText(hy.auth.acceptInvite.confirmRequired)).toBeInTheDocument()
  })

  it.each([
    ['an expired link', () => problem(422, 'staff.invite_expired'), hy.errors.staff.invite_expired],
    ['an unknown link', () => problem(404, 'staff.invite_invalid'), hy.errors.staff.invite_invalid],
  ])('explains %s', async (_, respond, expected) => {
    captureAccept(respond)
    const user = userEvent.setup()
    renderRoute('/accept-invite?token=abc')

    await choosePassword(user)

    expect(await screen.findByText(expected)).toBeInTheDocument()
  })

  it('shows server validation errors on the password', async () => {
    captureAccept(() => HttpResponse.json({ status: 400, code: 'validation_failed', errors: { password: ['password.too_long'] } }, { status: 400 }))
    const user = userEvent.setup()
    renderRoute('/accept-invite?token=abc')

    await choosePassword(user)

    expect(await screen.findByText(hy.errors.password.too_long)).toBeInTheDocument()
  })

  it('says the link is broken when it has no token', async () => {
    renderRoute('/accept-invite')

    expect(await screen.findByText(hy.errors.staff.invite_invalid)).toBeInTheDocument()
    expect(screen.queryByLabelText(hy.auth.acceptInvite.password)).not.toBeInTheDocument()
  })
})
