/** Partner profile statuses in their natural order, with tag colors. */
export const STATUS_COLORS = {
  Draft: 'default',
  UnderReview: 'processing',
  NeedsChanges: 'warning',
  Approved: 'success',
  Rejected: 'error',
  Suspended: 'magenta',
}

export const PARTNER_STATUSES = Object.keys(STATUS_COLORS)
