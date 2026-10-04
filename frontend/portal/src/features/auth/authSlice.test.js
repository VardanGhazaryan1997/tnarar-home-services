import { makeStore } from '@/app/store'
import { CUSTOMER, PARTNER_USER, sessionFor } from '@/test/auth'
import { noticeShown, profileUpdated, selectAuthStatus, selectIsPartner, selectNotice, selectUser, sessionStarted, signedOut } from './authSlice'

describe('authSlice', () => {
  it('starts unknown, then keeps the user when a session starts', () => {
    const store = makeStore()
    expect(selectAuthStatus(store.getState())).toBe('unknown')

    store.dispatch(sessionStarted(sessionFor(PARTNER_USER)))
    expect(selectAuthStatus(store.getState())).toBe('authenticated')
    expect(selectUser(store.getState())).toEqual(PARTNER_USER)
    expect(selectIsPartner(store.getState())).toBe(true)
  })

  it('updates the profile, and forgets everything when signed out', () => {
    const store = makeStore()
    store.dispatch(sessionStarted(sessionFor(CUSTOMER)))
    store.dispatch(profileUpdated({ ...CUSTOMER, fullName: 'Ani P.' }))
    expect(selectUser(store.getState()).fullName).toBe('Ani P.')
    expect(selectIsPartner(store.getState())).toBe(false)

    store.dispatch(signedOut('session.expired'))
    expect(selectUser(store.getState())).toBeNull()
    expect(selectNotice(store.getState())).toBe('session.expired')
    store.dispatch(noticeShown())
    expect(selectNotice(store.getState())).toBeNull()
  })
})
