/** Users as GET /admin/users and /admin/users/:id return them. */
export const userRow = (overrides = {}) => ({
  id: 'user-aram',
  phone: '+37491234567',
  fullName: 'Արամ Պետրոսյան',
  email: 'aram@example.com',
  roles: ['Customer', 'Partner'],
  status: 'Active',
  partnerStatus: 'Approved',
  lastSignInAt: '2026-10-02T08:30:00Z',
  createdAt: '2026-09-01T10:00:00Z',
  ...overrides,
})

export const userDetail = (overrides = {}) => ({
  id: 'user-aram',
  phone: '+37491234567',
  fullName: 'Արամ Պետրոսյան',
  email: 'aram@example.com',
  roles: ['Customer', 'Partner'],
  status: 'Active',
  blockReason: null,
  partner: { id: 'partner-aram', displayName: 'Aram Plumbing', status: 'Approved', slug: 'aram-plumbing-1a2b3c' },
  lastSignInAt: '2026-10-02T08:30:00Z',
  createdAt: '2026-09-01T10:00:00Z',
  ...overrides,
})

export const usersPage = (items, overrides = {}) => ({ items, page: 1, pageSize: 50, totalCount: items.length, ...overrides })
