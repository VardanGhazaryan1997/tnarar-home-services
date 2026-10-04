import { makeStore } from '@/app/store'
import { SUPER_ADMIN, sessionFor } from '@/test/auth'
import {
  challengeCleared,
  challengeReceived,
  selectAccessToken,
  selectAuthStatus,
  selectChallenge,
  selectNotice,
  selectStaff,
  sessionStarted,
  signedOut,
} from './authSlice'

const challenge = { status: 'two_factor_required', challengeToken: 'challenge-1', setupSecret: null, setupUri: null }

describe('authSlice', () => {
  it('starts with an unknown session', () => {
    const state = makeStore().getState()
    expect(selectAuthStatus(state)).toBe('unknown')
    expect(selectStaff(state)).toBeNull()
  })

  it('keeps the access token and staff profile when a session starts', () => {
    const store = makeStore()
    store.dispatch(challengeReceived(challenge))
    store.dispatch(sessionStarted(sessionFor(SUPER_ADMIN)))

    const state = store.getState()
    expect(selectAuthStatus(state)).toBe('authenticated')
    expect(selectAccessToken(state)).toMatch(/^access-/)
    expect(selectStaff(state)).toEqual(SUPER_ADMIN)
    expect(selectChallenge(state)).toBeNull()
  })

  it('forgets everything when signed out, optionally with a reason to show', () => {
    const store = makeStore()
    store.dispatch(sessionStarted(sessionFor()))
    store.dispatch(signedOut('session.expired'))

    const state = store.getState()
    expect(selectAuthStatus(state)).toBe('anonymous')
    expect(selectAccessToken(state)).toBeNull()
    expect(selectStaff(state)).toBeNull()
    expect(selectNotice(state)).toBe('session.expired')
  })

  it('holds the sign-in challenge between the password and authenticator steps', () => {
    const store = makeStore()
    store.dispatch(signedOut('session.expired'))
    store.dispatch(challengeReceived(challenge))

    expect(selectChallenge(store.getState())).toEqual(challenge)
    expect(selectNotice(store.getState())).toBeNull()

    store.dispatch(challengeCleared('staff.challenge_invalid'))
    expect(selectChallenge(store.getState())).toBeNull()
    expect(selectNotice(store.getState())).toBe('staff.challenge_invalid')
  })
})
