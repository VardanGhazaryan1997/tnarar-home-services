/** Audit entries as GET /admin/audit-log returns them. */
export const auditEntry = (overrides = {}) => ({
  id: 'audit-1',
  occurredAt: '2026-10-02T09:15:00Z',
  actorType: 'Staff',
  actorId: '019a0000-0000-7000-8000-000000000001',
  actorName: 'Ani Admin',
  action: 'Updated',
  entityType: 'PartnerProfile',
  entityId: 'partner-aram',
  changes: [
    { property: 'Status', oldValue: 'UnderReview', newValue: 'Approved' },
    { property: 'ReviewComment', oldValue: 'Add a licence', newValue: null },
  ],
  traceId: 'trace-1',
  ...overrides,
})

export const AUDIT_ENTRIES = [
  auditEntry(),
  auditEntry({ id: 'audit-2', actorType: 'User', actorName: null, actorId: 'user-aram', action: 'Created', entityType: 'PartnerService', entityId: 'partner-aram:cat-plumbing', changes: [] }),
  auditEntry({ id: 'audit-3', actorType: 'System', actorName: null, actorId: null, action: 'Deleted', entityType: 'OtpCode', entityId: '42', changes: [] }),
]

export const auditPage = (items, overrides = {}) => ({ items, page: 1, pageSize: 50, totalCount: items.length, ...overrides })
