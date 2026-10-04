/** Staff and roles as the /admin/staff, /admin/roles and /admin/permissions endpoints return them. */
export const ROLES = [
  { id: 'role-operator', name: 'Operator', description: 'Handles partners and users', isSystem: true, permissions: ['partners.view', 'partners.approve', 'users.view'] },
  { id: 'role-editor', name: 'Content editor', description: '', isSystem: false, permissions: ['content.manage'] },
]

const CODES = [
  'dashboard.view', 'catalog.manage', 'partners.view', 'partners.approve', 'users.view', 'users.block',
  'staff.view', 'staff.manage', 'roles.manage', 'content.manage', 'translations.manage', 'audit.view', 'settings.payments',
]

export const PERMISSIONS = CODES.map((code) => ({
  code,
  group: code.split('.')[0],
  superAdminOnly: code === 'roles.manage' || code === 'settings.payments',
}))

export const staffRow = (overrides = {}) => ({
  id: 'staff-gayane',
  email: 'gayane@homeservices.local',
  fullName: 'Գայանե Սարգսյան',
  isSuperAdmin: false,
  status: 'Active',
  twoFactorEnabled: true,
  roles: [{ id: 'role-operator', name: 'Operator' }],
  lastSignInAt: '2026-10-02T08:30:00Z',
  inviteExpiresAt: null,
  createdAt: '2026-09-01T10:00:00Z',
  ...overrides,
})

export const staffDetail = (overrides = {}, permissions = ['partners.view', 'partners.approve', 'users.view']) => ({
  member: staffRow(overrides),
  permissions,
})

export const staffPage = (items) => ({ items, page: 1, pageSize: 50, totalCount: items.length })

export const invitation = (member = staffRow({ status: 'Invited', twoFactorEnabled: false, lastSignInAt: null })) => ({
  member,
  inviteToken: 'tok/en+1',
  expiresAt: '2026-10-10T09:00:00Z',
})
