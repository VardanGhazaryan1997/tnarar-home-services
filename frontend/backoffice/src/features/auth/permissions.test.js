import { SUPER_ADMIN, staffMember } from '@/test/auth'
import { hasPermission } from './permissions'

describe('hasPermission', () => {
  it('allows anything that needs no permission, even without staff', () => {
    expect(hasPermission(null, undefined)).toBe(true)
  })

  it('denies when nobody is signed in', () => {
    expect(hasPermission(null, 'partners.view')).toBe(false)
  })

  it('allows Super Admins everything', () => {
    expect(hasPermission(SUPER_ADMIN, 'roles.manage')).toBe(true)
  })

  it("checks other staff against their roles' permissions", () => {
    const operator = staffMember(['partners.view'])
    expect(hasPermission(operator, 'partners.view')).toBe(true)
    expect(hasPermission(operator, 'staff.view')).toBe(false)
  })
})
