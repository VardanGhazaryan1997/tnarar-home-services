import { screen, waitFor, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { http, HttpResponse } from 'msw'
import hy from '@/i18n/locales/hy/common.json'
import { problem, sessionFor, signedInAs, signedOutBrowser, staffMember, SUPER_ADMIN } from '@/test/auth'
import { renderRoute } from '@/test/renderWithProviders'
import { server } from '@/test/server'

const CHALLENGE = { status: 'two_factor_required', challengeToken: 'challenge-1', setupSecret: null, setupUri: null }
const SETUP_CHALLENGE = {
  status: 'two_factor_setup_required',
  challengeToken: 'challenge-2',
  setupSecret: 'JBSWY3DPEHPK3PXP',
  setupUri: 'otpauth://totp/Home%20Services:admin@homeservices.local?secret=JBSWY3DPEHPK3PXP&issuer=Home%20Services',
}

function loginReturns(response) {
  const requests = []
  server.use(
    http.post('*/api/v1/admin/auth/login', async ({ request }) => {
      requests.push(await request.json())
      return response()
    }),
  )
  return requests
}

function twoFactorEndpoint(path, respond) {
  const requests = []
  server.use(
    http.post(`*/api/v1/admin/auth/2fa/${path}`, async ({ request }) => {
      const body = await request.json()
      requests.push(body)
      return respond(body)
    }),
  )
  return requests
}

async function enterPassword(user, email = 'admin@homeservices.local', password = 'Dev-Admin-Password-1') {
  await user.type(await screen.findByLabelText(hy.auth.signIn.email), email)
  await user.type(screen.getByLabelText(hy.auth.signIn.password), password)
  await user.click(screen.getByRole('button', { name: hy.auth.signIn.submit }))
}

async function enterCode(user, code) {
  const inputs = await screen.findAllByRole('textbox', { name: /OTP/i })
  await user.click(inputs[0])
  await user.keyboard(code)
}

describe('signing in to the Back Office', () => {
  describe('at start-up', () => {
    it('restores the session from the refresh cookie', async () => {
      renderRoute('/')
      expect(await screen.findByRole('heading', { level: 1, name: hy.dashboard.title })).toBeInTheDocument()
    })

    it('sends staff without a session to the sign-in page', async () => {
      signedOutBrowser()
      const { router } = renderRoute('/partners')

      expect(await screen.findByRole('heading', { level: 1, name: hy.auth.signIn.title })).toBeInTheDocument()
      expect(router.state.location.pathname).toBe('/login')
    })

    it('sends signed-in staff away from the sign-in page', async () => {
      const { router } = renderRoute('/login')

      expect(await screen.findByRole('heading', { level: 1, name: hy.dashboard.title })).toBeInTheDocument()
      expect(router.state.location.pathname).toBe('/')
    })
  })

  describe('the password step', () => {
    beforeEach(() => signedOutBrowser())

    it('checks that email and password are filled in', async () => {
      const user = userEvent.setup()
      renderRoute('/login')

      await user.click(await screen.findByRole('button', { name: hy.auth.signIn.submit }))

      expect(await screen.findByText(hy.auth.signIn.emailRequired)).toBeInTheDocument()
      expect(screen.getByText(hy.auth.signIn.passwordRequired)).toBeInTheDocument()
    })

    it('shows why the sign-in failed', async () => {
      loginReturns(() => problem(401, 'staff.invalid_credentials'))
      const user = userEvent.setup()
      renderRoute('/login')

      await enterPassword(user, 'admin@homeservices.local', 'wrong')

      expect(await screen.findByText(hy.errors.staff.invalid_credentials)).toBeInTheDocument()
    })

    it('shows server-side validation errors on the fields', async () => {
      loginReturns(() =>
        HttpResponse.json(
          { status: 400, code: 'validation_failed', errors: { password: ['password.required'] } },
          { status: 400 },
        ),
      )
      const user = userEvent.setup()
      renderRoute('/login')

      await enterPassword(user, ' admin@homeservices.local ', ' ')

      expect(await screen.findByText(hy.errors.password.required)).toBeInTheDocument()
    })

    it('continues to the authenticator step with a trimmed email', async () => {
      const requests = loginReturns(() => HttpResponse.json(CHALLENGE))
      const user = userEvent.setup()
      const { router } = renderRoute('/login')

      await enterPassword(user, '  admin@homeservices.local  ')

      expect(await screen.findByRole('heading', { level: 1, name: hy.auth.twoFactor.title })).toBeInTheDocument()
      expect(router.state.location.pathname).toBe('/login/2fa')
      expect(requests).toEqual([{ email: 'admin@homeservices.local', password: 'Dev-Admin-Password-1' }])
    })

    it('opens the authenticator step only after the password step', async () => {
      const { router } = renderRoute('/login/2fa')

      expect(await screen.findByRole('heading', { level: 1, name: hy.auth.signIn.title })).toBeInTheDocument()
      expect(router.state.location.pathname).toBe('/login')
    })
  })

  describe('the authenticator step', () => {
    beforeEach(() => signedOutBrowser())

    it('signs in with a correct code and returns to the page the staff member wanted', async () => {
      loginReturns(() => HttpResponse.json(CHALLENGE))
      const requests = twoFactorEndpoint('verify', () => HttpResponse.json(sessionFor(SUPER_ADMIN)))
      const user = userEvent.setup()
      const { router } = renderRoute('/catalog')

      await enterPassword(user)
      await screen.findByRole('heading', { level: 1, name: hy.auth.twoFactor.title })
      await enterCode(user, '123456')
      await user.click(screen.getByRole('button', { name: hy.auth.twoFactor.submit }))

      expect(await screen.findByRole('heading', { name: hy.nav.catalog })).toBeInTheDocument()
      await waitFor(() => expect(router.state.location.pathname).toBe('/catalog/categories'))
      expect(requests).toEqual([{ challengeToken: 'challenge-1', code: '123456' }])
    })

    it('asks for the full 6-digit code', async () => {
      loginReturns(() => HttpResponse.json(CHALLENGE))
      const user = userEvent.setup()
      renderRoute('/login')

      await enterPassword(user)
      await screen.findByRole('heading', { level: 1, name: hy.auth.twoFactor.title })
      await enterCode(user, '123')
      await user.click(screen.getByRole('button', { name: hy.auth.twoFactor.submit }))

      expect(await screen.findByText(hy.auth.twoFactor.codeRequired)).toBeInTheDocument()
    })

    it('lets the staff member try again after a wrong code', async () => {
      loginReturns(() => HttpResponse.json(CHALLENGE))
      twoFactorEndpoint('verify', () => problem(422, 'staff.totp_invalid'))
      const user = userEvent.setup()
      renderRoute('/login')

      await enterPassword(user)
      await screen.findByRole('heading', { level: 1, name: hy.auth.twoFactor.title })
      await enterCode(user, '000000')
      await user.click(screen.getByRole('button', { name: hy.auth.twoFactor.submit }))

      expect(await screen.findByText(hy.errors.staff.totp_invalid)).toBeInTheDocument()
      expect(screen.getByRole('heading', { level: 1, name: hy.auth.twoFactor.title })).toBeInTheDocument()
    })

    it('goes back to the password step when the challenge has expired', async () => {
      loginReturns(() => HttpResponse.json(CHALLENGE))
      twoFactorEndpoint('verify', () => problem(401, 'staff.challenge_invalid'))
      const user = userEvent.setup()
      const { router } = renderRoute('/login')

      await enterPassword(user)
      await screen.findByRole('heading', { level: 1, name: hy.auth.twoFactor.title })
      await enterCode(user, '123456')
      await user.click(screen.getByRole('button', { name: hy.auth.twoFactor.submit }))

      expect(await screen.findByText(hy.errors.staff.challenge_invalid)).toBeInTheDocument()
      expect(router.state.location.pathname).toBe('/login')
    })

    it('can go back to sign in with a different account', async () => {
      loginReturns(() => HttpResponse.json(CHALLENGE))
      const user = userEvent.setup()
      const { router } = renderRoute('/login')

      await enterPassword(user)
      await user.click(await screen.findByRole('button', { name: hy.auth.twoFactor.back }))

      expect(await screen.findByRole('heading', { level: 1, name: hy.auth.signIn.title })).toBeInTheDocument()
      expect(router.state.location.pathname).toBe('/login')
    })

    it('on first sign-in shows the QR code and key, then turns on two-step verification', async () => {
      loginReturns(() => HttpResponse.json(SETUP_CHALLENGE))
      const requests = twoFactorEndpoint('setup', () => HttpResponse.json(sessionFor(SUPER_ADMIN)))
      const user = userEvent.setup()
      renderRoute('/login')

      await enterPassword(user)

      expect(await screen.findByRole('heading', { level: 1, name: hy.auth.twoFactor.setupTitle })).toBeInTheDocument()
      expect(screen.getByLabelText(hy.auth.twoFactor.qrLabel)).toBeInTheDocument()
      expect(screen.getByText(SETUP_CHALLENGE.setupSecret)).toBeInTheDocument()

      await enterCode(user, '654321')
      await user.click(screen.getByRole('button', { name: hy.auth.twoFactor.setupSubmit }))

      expect(await screen.findByRole('heading', { level: 1, name: hy.dashboard.title })).toBeInTheDocument()
      expect(requests).toEqual([{ challengeToken: 'challenge-2', code: '654321' }])
    })
  })

  describe('signing out', () => {
    it('ends the session and returns to the sign-in page', async () => {
      const logout = { called: false }
      server.use(
        http.post('*/api/v1/admin/auth/logout', () => {
          logout.called = true
          return new HttpResponse(null, { status: 204 })
        }),
      )
      const user = userEvent.setup()
      const { router } = renderRoute('/')

      await user.click(await screen.findByRole('button', { name: hy.auth.userMenu }))
      expect(await screen.findByText(SUPER_ADMIN.email)).toBeInTheDocument()
      await user.click(await screen.findByText(hy.auth.signOut))

      expect(await screen.findByRole('heading', { level: 1, name: hy.auth.signIn.title })).toBeInTheDocument()
      expect(router.state.location.pathname).toBe('/login')
      expect(logout.called).toBe(true)
    })

    it('shows why the staff member was signed out', async () => {
      const { store } = renderRoute('/')
      await screen.findByRole('heading', { level: 1, name: hy.dashboard.title })

      store.dispatch({ type: 'auth/signedOut', payload: 'session.revoked' })

      expect(await screen.findByText(hy.errors.session.revoked)).toBeInTheDocument()
    })
  })

  describe('permissions', () => {
    it('shows Super Admins every menu section', async () => {
      renderRoute('/')
      const nav = await screen.findByRole('navigation', { name: hy.nav.label })
      expect(within(nav).getAllByRole('menuitem')).toHaveLength(13)
    })

    it('shows other staff only the sections their roles allow', async () => {
      signedInAs(staffMember(['partners.view']))
      renderRoute('/')

      const nav = await screen.findByRole('navigation', { name: hy.nav.label })
      await waitFor(() => expect(within(nav).getAllByRole('menuitem')).toHaveLength(2))
      expect(within(nav).getByRole('menuitem', { name: new RegExp(hy.nav.partners) })).toBeInTheDocument()
      expect(within(nav).queryByRole('menuitem', { name: new RegExp(hy.nav.staff) })).not.toBeInTheDocument()
    })

    it('shows a no-access page for sections the staff member cannot open', async () => {
      signedInAs(staffMember(['partners.view']))
      renderRoute('/staff')

      expect(await screen.findByText(hy.forbidden.title)).toBeInTheDocument()
    })
  })
})
