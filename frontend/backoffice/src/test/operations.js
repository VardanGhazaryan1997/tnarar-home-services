const PLACE = { categoryId: 'cat-plumbing', categoryName: 'Սանտեխնիկա', cityId: 'city-yerevan', cityName: 'Երևան', districtId: null, districtName: null }

/** A row of GET /admin/requests. */
export const requestRow = (overrides = {}) => ({
  id: 'request-1',
  kind: 'Open',
  status: 'Open',
  place: PLACE,
  excerpt: 'Ծորակը կաթում է',
  customerId: 'user-ani',
  customerPhone: '+37491000111',
  customerName: 'Անի Պետրոսյան',
  sentTo: 0,
  responded: 0,
  declined: 0,
  attentionReason: 'NoMatchingPartners',
  needsAttentionSince: '2026-10-05T09:00:00Z',
  createdAt: '2026-10-05T09:00:00Z',
  ...overrides,
})

/** GET /admin/requests/{id}. */
export const requestDetail = (overrides = {}) => ({
  id: 'request-1',
  kind: 'Open',
  status: 'Open',
  place: PLACE,
  description: 'Ծորակը կաթում է, պետք է փոխել։',
  preferredDate: '2026-10-07',
  timeNote: 'երեկոյան',
  budgetMin: 10000,
  budgetMax: 30000,
  media: [],
  customer: { userId: 'user-ani', phone: '+37491000111', fullName: 'Անի Պետրոսյան', email: null, isBlocked: false },
  recipients: [],
  attentionReason: 'NoMatchingPartners',
  needsAttentionSince: '2026-10-05T09:00:00Z',
  createdAt: '2026-10-05T09:00:00Z',
  cancelledAt: null,
  cancelReason: null,
  ...overrides,
})

export const recipient = (overrides = {}) => ({
  partnerId: 'partner-aram',
  displayName: 'Արամ Սանտեխնիկ',
  partnerStatus: 'Approved',
  source: 'Matched',
  status: 'Declined',
  sentAt: '2026-10-05T09:00:00Z',
  viewedAt: '2026-10-05T10:00:00Z',
  declinedAt: '2026-10-05T10:05:00Z',
  declineReason: 'Զբաղված եմ',
  respondedAt: null,
  ...overrides,
})

/** A row of GET /admin/orders. */
export const orderRow = (overrides = {}) => ({
  id: 'order-1',
  kind: 'Work',
  status: 'Cancelled',
  place: PLACE,
  summary: 'Ծորակի փոխարինում',
  price: 100000,
  customerId: 'user-ani',
  customerName: 'Անի Պետրոսյան',
  customerPhone: '+37491000111',
  partnerId: 'partner-aram',
  partnerName: 'Արամ Սանտեխնիկ',
  pendingChange: false,
  needsAttentionSince: '2026-10-08T10:00:00Z',
  createdAt: '2026-10-05T11:00:00Z',
  ...overrides,
})

export const orderPayment = (overrides = {}) => ({
  id: 'payment-1',
  stageId: null,
  stageTitle: null,
  amount: 40000,
  method: 'Cash',
  paidOn: '2026-10-06',
  note: null,
  recordedBy: 'Partner',
  mine: false,
  status: 'Disputed',
  recordedAt: '2026-10-06T12:00:00Z',
  answeredAt: '2026-10-06T13:00:00Z',
  disputeReason: 'Չեմ վճարել',
  resolvedAt: null,
  resolutionNote: null,
  ...overrides,
})

/** GET /admin/orders/{id} (an OrderDto as staff see it). */
export const orderDetail = (overrides = {}) => ({
  id: 'order-1',
  kind: 'Work',
  status: 'Cancelled',
  myRole: 'Staff',
  requestId: 'request-1',
  offerId: 'offer-1',
  parentOrderId: null,
  place: PLACE,
  price: 120000,
  terms: { version: 1, summary: 'Ծորակի և խողովակների փոխարինում', lines: [], price: 100000, materialsIncluded: false, materialsNote: null, startDate: '2026-10-08', durationDays: 2, visitAt: null, stages: [], offerSentAt: '2026-10-05T10:00:00Z' },
  stages: [
    { id: 'stage-1', title: null, purpose: 'Deposit', amount: 40000 },
    { id: 'stage-2', title: null, purpose: 'Final', amount: 80000 },
  ],
  customer: { userId: 'user-ani', fullName: 'Անի Պետրոսյան', phone: '+37491000111' },
  partner: { partnerId: 'partner-aram', displayName: 'Արամ Սանտեխնիկ', slug: 'aram', phone: '+37499111222' },
  history: [
    { status: 'Confirmed', changedAt: '2026-10-05T11:00:00Z', by: 'Customer', note: null },
    { status: 'InProgress', changedAt: '2026-10-06T09:00:00Z', by: 'Partner', note: null },
    { status: 'Cancelled', changedAt: '2026-10-08T10:00:00Z', by: 'Customer', note: 'Աշխատանքը կիսատ է' },
  ],
  createdAt: '2026-10-05T11:00:00Z',
  startDate: '2026-10-08',
  durationDays: 3,
  visitAt: null,
  startedAt: '2026-10-06T09:00:00Z',
  completionRequestedAt: null,
  autoCompleteAt: null,
  completedAt: null,
  cancelledAt: '2026-10-08T10:00:00Z',
  cancelledBy: 'Customer',
  cancelReason: 'Աշխատանքը կիսատ է',
  needsAttentionSince: '2026-10-08T10:00:00Z',
  changeRequests: [
    { id: 'change-1', kind: 'ExtraWork', status: 'Accepted', proposedBy: 'Partner', mine: false, title: 'Սիֆոն', description: null, amount: 20000, newStartDate: null, newDurationDays: null, newVisitAt: null, responseNote: null, proposedAt: '2026-10-06T12:00:00Z', decidedAt: '2026-10-06T13:00:00Z' },
  ],
  payments: [orderPayment()],
  paidAmount: 0,
  review: null,
  actions: ['resolve'],
  ...overrides,
})

/** A row of GET /admin/payments. */
export const paymentRow = (overrides = {}) => ({
  id: 'payment-1',
  orderId: 'order-1',
  orderPrice: 120000,
  amount: 40000,
  method: 'Cash',
  paidOn: '2026-10-06',
  note: null,
  recordedBy: 'Partner',
  status: 'Disputed',
  recordedAt: '2026-10-06T12:00:00Z',
  answeredAt: '2026-10-06T13:00:00Z',
  disputeReason: 'Չեմ վճարել',
  resolvedAt: null,
  resolutionNote: null,
  customerId: 'user-ani',
  customerName: 'Անի Պետրոսյան',
  customerPhone: '+37491000111',
  partnerId: 'partner-aram',
  partnerName: 'Արամ Սանտեխնիկ',
  partnerPhone: '+37499111222',
  ...overrides,
})

/** A row of GET /admin/reviews. */
export const reviewRow = (overrides = {}) => ({
  id: 'review-1',
  orderId: 'order-1',
  rating: 1,
  text: 'Զանգեք ինձ 091000111',
  submittedAt: '2026-10-09T10:00:00Z',
  reply: null,
  repliedAt: null,
  isHidden: false,
  hiddenReason: null,
  hiddenAt: null,
  customerId: 'user-ani',
  customerName: 'Անի Պետրոսյան',
  partnerId: 'partner-aram',
  partnerName: 'Արամ Սանտեխնիկ',
  partnerSlug: 'aram',
  ...overrides,
})

/** A statement as the API returns it (inside rows and the detail). */
export const statement = (overrides = {}) => ({
  id: 'statement-1',
  periodStart: '2026-10-05',
  periodEnd: '2026-10-11',
  total: 15000,
  paidAmount: 5000,
  outstanding: 10000,
  status: 'Open',
  overdue: true,
  dueOn: '2026-10-19',
  issuedAt: '2026-10-12T09:00:00Z',
  paidAt: null,
  ...overrides,
})

/** A row of GET /admin/commission-statements. */
export const statementRow = (overrides = {}, statementOverrides = {}) => ({
  statement: statement(statementOverrides),
  partnerId: 'partner-aram',
  partnerName: 'Արամ Սանտեխնիկ',
  partnerPhone: '+37491000222',
  partnerPaused: false,
  ...overrides,
})

/** GET /admin/commission-statements/{id}. */
export const statementDetail = (overrides = {}, statementOverrides = {}) => ({
  summary: statementRow(overrides, statementOverrides),
  lines: [
    {
      id: 'line-1',
      orderId: 'order-1',
      summary: 'Ծորակի փոխարինում',
      completedAt: '2026-10-06T12:00:00Z',
      orderPrice: 150000,
      ratePercent: 10,
      amount: 15000,
      statementId: 'statement-1',
    },
  ],
  settlements: [
    { id: 'settlement-1', amount: 5000, method: 'Cash', paidOn: '2026-10-13', reference: 'Անդորրագիր 7', recordedAt: '2026-10-13T10:00:00Z' },
  ],
})

/** GET /admin/commission-rates. */
export const commissionRates = (overrides = {}) => ({
  defaultPercent: 10,
  categories: [
    { categoryId: 'cat-plumbing', name: 'Սանտեխնիկա', parentId: null, percent: 12.5, effectivePercent: 12.5 },
    { categoryId: 'cat-heating', name: 'Ջեռուցում', parentId: null, percent: null, effectivePercent: 10 },
    { categoryId: 'cat-boilers', name: 'Կաթսաներ', parentId: 'cat-heating', percent: null, effectivePercent: 10 },
  ],
  ...overrides,
})
