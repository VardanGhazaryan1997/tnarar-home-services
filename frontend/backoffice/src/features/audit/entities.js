/** Audited entity types (server class names) and where each one is shown in the Back Office. */
export const ENTITY_TYPES = [
  'PartnerProfile', 'User', 'StaffUser', 'Role', 'Category', 'City', 'District', 'StaticPage', 'FaqItem', 'Language',
  'PartnerService', 'PartnerArea', 'PartnerMedia', 'StaffUserRole', 'CommissionRate', 'CommissionStatement', 'Settlement',
]

const PAGES = {
  PartnerProfile: (id) => `/partners/${id}`,
  User: (id) => `/users/${id}`,
  StaffUser: (id) => `/staff/members/${id}`,
  StaticPage: (id) => `/content/pages/${id}`,
  CommissionStatement: (id) => `/commissions/statements/${id}`,
}

/** The Back Office page of an entity, or null when it has none. */
export const entityPage = (type, id) => PAGES[type]?.(id) ?? null

export const ACTIONS = ['Created', 'Updated', 'Deleted']

export const ACTION_COLORS = { Created: 'success', Updated: 'processing', Deleted: 'error' }
