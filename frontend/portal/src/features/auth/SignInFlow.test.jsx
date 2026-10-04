import { screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { http, HttpResponse } from 'msw'
import en from '@/i18n/locales/en/common.json'
import { CUSTOMER, NEW_USER, problem, sessionFor, signedInAs } from '@/test/auth'
import { renderRoute } from '@/test/renderWithProviders'
import { server } from '@/test/server'

function smsApi({ user = CUSTOMER, resendAfterSeconds = 0 } = {}) {
  const calls = { sent: [], verified: [], profile: [] }
  server.use(
    http.post('*/api/v1/auth/otp/send', async ({ request }) => {
      calls.sent.push(await request.json())
      return HttpResponse.json({ expiresInSeconds: 300, resendAfterSeconds })
    }),
    http.post('*/api/v1/auth/otp/verify', async ({ request }) => {
      const body = await request.json()
      calls.verified.push(body)
      return body.code === '123456' ? HttpResponse.json(sessionFor(user)) : problem(422, 'otp.invalid')
    }),
    http.put('*/api/v1/me', async ({ request }) => {
      const body = await request.json()
      calls.profile.push(body)
      return body.fullName === 'x'.repeat(5)
        ? problem(400, 'validation_failed', { errors: { fullName: ['name.too_long'] } })
        : HttpResponse.json({ ...user, fullName: body.fullName, email: body.email, isProfileComplete: true })
    }),
  )
  return calls
}

async function enterPhoneAndCode(user, code = '123456') {
  await user.type(await screen.findByLabelText(en.auth.phoneLabel), '091 23 45 67')
  await user.click(screen.getByRole('button', { name: en.auth.sendCode }))
  await user.type(await screen.findByLabelText(en.auth.codeLabel), code)
  await user.click(screen.getByRole('button', { name: en.auth.confirm }))
}

describe('Sign-in flow', () => {
  it('signs a returning user in with a phone and SMS code and goes back to the page they wanted', async () => {
    const user = userEvent.setup()
    const calls = smsApi()
    const { router } = renderRoute('/en/account')

    await waitFor(() => expect(router.state.location.pathname).toBe('/en/sign-in'))
    await enterPhoneAndCode(user)

    expect(await screen.findByRole('heading', { name: en.account.title })).toBeInTheDocument()
    expect(router.state.location.pathname).toBe('/en/account')
    expect(calls.sent).toEqual([{ phone: '091 23 45 67' }])
    expect(calls.verified).toEqual([{ phone: '091 23 45 67', code: '123456' }])
  })

  it('asks new users for their name', async () => {
    const user = userEvent.setup()
    const calls = smsApi({ user: NEW_USER })
    const { router } = renderRoute('/en/sign-in')

    await enterPhoneAndCode(user)
    await user.type(await screen.findByLabelText(en.account.name), 'x'.repeat(5))
    await user.click(screen.getByRole('button', { name: en.auth.finish }))
    expect(await screen.findByText(en.errors.name.too_long)).toBeInTheDocument()

    await user.clear(screen.getByLabelText(en.account.name))
    await user.type(screen.getByLabelText(en.account.name), 'Ani Petrosyan')
    await user.type(screen.getByLabelText(/Email/), 'ani@example.com')
    await user.click(screen.getByRole('button', { name: en.auth.finish }))

    await waitFor(() => expect(router.state.location.pathname).toBe('/en'))
    expect(calls.profile.at(-1)).toEqual({ fullName: 'Ani Petrosyan', email: 'ani@example.com' })
  })

  it('shows a wrong code, lets the user resend it or change the number', async () => {
    const user = userEvent.setup()
    const calls = smsApi()
    renderRoute('/en/sign-in')

    await enterPhoneAndCode(user, '000000')
    expect(await screen.findByText(en.errors.otp.invalid)).toBeInTheDocument()

    await user.click(screen.getByRole('button', { name: en.auth.resend }))
    await waitFor(() => expect(calls.sent).toHaveLength(2))
    await user.click(screen.getByRole('button', { name: en.auth.changePhone }))
    expect(screen.getByLabelText(en.auth.phoneLabel)).toHaveValue('091 23 45 67')
  })

  it('counts down before a new code can be sent', async () => {
    const user = userEvent.setup()
    smsApi({ resendAfterSeconds: 60 })
    renderRoute('/en/sign-in')

    await user.type(await screen.findByLabelText(en.auth.phoneLabel), '091234567')
    await user.click(screen.getByRole('button', { name: en.auth.sendCode }))

    expect(await screen.findByRole('button', { name: /Send again in (60|59) s/ })).toBeDisabled()
  })

  it('shows phone errors from the API', async () => {
    const user = userEvent.setup()
    server.use(
      http.post('*/api/v1/auth/otp/send', () => problem(400, 'validation_failed', { errors: { phone: ['phone.invalid'] } })),
    )
    renderRoute('/en/sign-in')

    await user.type(await screen.findByLabelText(en.auth.phoneLabel), '12')
    await user.click(screen.getByRole('button', { name: en.auth.sendCode }))

    expect(await screen.findByText(en.errors.phone.invalid)).toBeInTheDocument()
  })

  it('shows other send failures, such as too many requests', async () => {
    const user = userEvent.setup()
    server.use(http.post('*/api/v1/auth/otp/send', () => problem(429, 'otp.too_many_requests')))
    renderRoute('/en/sign-in')

    await user.type(await screen.findByLabelText(en.auth.phoneLabel), '091234567')
    await user.click(screen.getByRole('button', { name: en.auth.sendCode }))

    expect(await screen.findByText(en.errors.otp.too_many_requests)).toBeInTheDocument()
  })

  it('sends signed-in users straight on', async () => {
    signedInAs(CUSTOMER)
    const { router } = renderRoute('/en/sign-in')
    await waitFor(() => expect(router.state.location.pathname).toBe('/en'))
  })

  it('tells users why they were signed out', async () => {
    renderRoute('/en/sign-in', {
      preloadedState: { auth: { status: 'anonymous', checking: false, accessToken: null, accessTokenExpiresAt: null, user: null, notice: 'session.revoked' } },
    })

    expect(await screen.findByText(en.errors.session.revoked)).toBeInTheDocument()
  })
})

describe('Account page', () => {
  it('edits the profile and signs out', async () => {
    const user = userEvent.setup()
    signedInAs(CUSTOMER)
    const calls = smsApi()
    let loggedOut = false
    server.use(
      http.post('*/api/v1/auth/logout', () => {
        loggedOut = true
        return new HttpResponse(null, { status: 204 })
      }),
    )
    const { router } = renderRoute('/en/account')

    const name = await screen.findByLabelText(en.account.name)
    expect(screen.getByLabelText(en.account.phone)).toHaveValue(CUSTOMER.phoneNumber)
    expect(screen.getByText(en.account.role.Customer)).toBeInTheDocument()
    await user.clear(name)
    await user.type(name, 'Ani P')
    await user.click(screen.getByRole('button', { name: en.common.save }))
    expect(await screen.findByText(en.account.saved)).toBeInTheDocument()
    expect(calls.profile).toEqual([{ fullName: 'Ani P', email: null }])

    await user.click(screen.getByRole('button', { name: en.account.signOut }))
    await waitFor(() => expect(router.state.location.pathname).toBe('/en'))
    expect(loggedOut).toBe(true)
    expect(await screen.findAllByRole('link', { name: en.nav.signIn })).toHaveLength(2)
  })

  it('shows save failures', async () => {
    const user = userEvent.setup()
    signedInAs(CUSTOMER)
    server.use(http.put('*/api/v1/me', () => problem(500, 'internal_error')))
    renderRoute('/en/account')

    await user.click(await screen.findByRole('button', { name: en.common.save }))
    expect(await screen.findByText(en.errors.generic)).toBeInTheDocument()
  })
})
