// API response shapes used by feature tests (as the backend returns them).

export const CATEGORIES = [
  { id: 'cat-plumbing', slug: 'plumbing', name: 'Plumbing', icon: 'pipe', children: [] },
  { id: 'cat-heating', slug: 'heating', name: 'Heating', icon: null, children: [{ id: 'cat-boilers', slug: 'boilers', name: 'Boilers', icon: null, children: [] }] },
]

export const REGIONS = [
  { id: 'region-yerevan', slug: 'yerevan', name: 'Yerevan' },
  { id: 'region-ararat', slug: 'ararat', name: 'Ararat' },
]

export const CITIES = [
  { id: 'city-yerevan', slug: 'yerevan', name: 'Yerevan', regionId: 'region-yerevan', kind: 'City', districts: [{ id: 'dist-kentron', slug: 'kentron', name: 'Kentron' }] },
  { id: 'city-masis', slug: 'masis', name: 'Masis', regionId: 'region-ararat', kind: 'City', districts: [] },
  { id: 'city-dalar', slug: 'dalar', name: 'Dalar', regionId: 'region-ararat', kind: 'Village', districts: [] },
]

export const PLACE = { categoryId: 'cat-plumbing', categoryName: 'Plumbing', cityId: 'city-yerevan', cityName: 'Yerevan', districtId: 'dist-kentron', districtName: 'Kentron' }

export const page = (items, { page: number = 1, pageSize = 20, totalCount = items.length } = {}) => ({ items, page: number, pageSize, totalCount })

export const myRequest = (overrides = {}) => ({
  id: 'req-1',
  kind: 'Open',
  status: 'Open',
  place: PLACE,
  description: 'The kitchen tap is leaking, please help.',
  preferredDate: '2026-10-07',
  timeNote: 'evenings',
  budgetMin: 10000,
  budgetMax: 30000,
  media: [
    { id: 'file-1', kind: 'Image', status: 'Ready', fileName: 'leak.jpg', contentType: 'image/jpeg', size: 100, width: 10, height: 10, url: 'https://files/leak.jpg', thumbnailUrl: 'https://files/leak-t.jpg' },
    { id: 'file-2', kind: 'Video', status: 'Ready', fileName: 'leak.mp4', contentType: 'video/mp4', size: 100, width: null, height: null, url: 'https://files/leak.mp4', thumbnailUrl: null },
  ],
  sentTo: 3,
  partners: [{ partnerId: 'partner-1', displayName: 'Aram Plumbing', slug: 'aram-plumbing', status: 'Responded' }],
  findingPartners: false,
  createdAt: '2026-10-05T09:00:00Z',
  cancelledAt: null,
  cancelReason: null,
  ...overrides,
})

export const myRequestItem = (overrides = {}) => ({
  id: 'req-1',
  kind: 'Open',
  status: 'Open',
  place: PLACE,
  excerpt: 'The kitchen tap is leaking, please help.',
  preferredDate: '2026-10-07',
  sentTo: 3,
  responded: 1,
  findingPartners: false,
  createdAt: '2026-10-05T09:00:00Z',
  ...overrides,
})

export const inboxItem = (overrides = {}) => ({
  id: 'req-1',
  kind: 'Open',
  requestStatus: 'Open',
  myStatus: 'New',
  place: PLACE,
  excerpt: 'The kitchen tap is leaking, please help.',
  preferredDate: '2026-10-07',
  mediaCount: 2,
  sentAt: '2026-10-05T09:00:00Z',
  ...overrides,
})

export const inboxRequest = (overrides = {}) => ({
  id: 'req-1',
  kind: 'Open',
  requestStatus: 'Open',
  myStatus: 'Viewed',
  place: PLACE,
  description: 'The kitchen tap is leaking, please help.',
  preferredDate: '2026-10-07',
  timeNote: 'evenings',
  media: [],
  customerFirstName: 'Ani',
  sentAt: '2026-10-05T09:00:00Z',
  createdAt: '2026-10-05T09:00:00Z',
  ...overrides,
})

export const offer = (overrides = {}) => ({
  id: 'offer-1',
  requestId: 'req-1',
  kind: 'Work',
  status: 'Sent',
  partner: { partnerId: 'partner-1', displayName: 'Aram Plumbing', slug: 'aram-plumbing' },
  summary: 'Replace the tap and the pipes under the sink.',
  lines: [
    { title: 'Remove the old tap', included: true },
    { title: 'Tiling', included: false },
  ],
  price: 100000,
  materialsIncluded: true,
  materialsNote: 'Tap included',
  startDate: '2026-10-08',
  durationDays: 2,
  visitAt: null,
  stages: [
    { title: 'Deposit', purpose: 'Deposit', amount: 30000 },
    { title: null, purpose: 'Final', amount: 70000 },
  ],
  expiresAt: '2026-10-12T09:00:00Z',
  sentAt: '2026-10-05T10:00:00Z',
  decidedAt: null,
  rejectReason: null,
  orderId: null,
  ...overrides,
})

export const myOfferItem = (overrides = {}) => ({
  id: 'offer-1',
  requestId: 'req-1',
  kind: 'Work',
  status: 'Sent',
  place: PLACE,
  requestExcerpt: 'The kitchen tap is leaking, please help.',
  price: 100000,
  visitAt: null,
  expiresAt: '2026-10-12T09:00:00Z',
  sentAt: '2026-10-05T10:00:00Z',
  orderId: null,
  ...overrides,
})

export const order = (overrides = {}) => ({
  id: 'order-1',
  kind: 'Work',
  status: 'Confirmed',
  myRole: 'Customer',
  requestId: 'req-1',
  offerId: 'offer-1',
  parentOrderId: null,
  place: PLACE,
  price: 100000,
  terms: {
    version: 1,
    summary: 'Replace the tap and the pipes under the sink.',
    lines: [{ title: 'Remove the old tap', included: true }],
    price: 100000,
    materialsIncluded: false,
    materialsNote: null,
    startDate: null,
    durationDays: null,
    visitAt: null,
    stages: [{ title: null, purpose: 'Final', amount: 100000 }],
    offerSentAt: '2026-10-05T10:00:00Z',
  },
  stages: [{ id: 'stage-1', title: null, purpose: 'Final', amount: 100000 }],
  customer: { userId: 'user-1', fullName: 'Ani Petrosyan', phone: '+37491234567' },
  partner: { partnerId: 'partner-1', displayName: 'Aram Plumbing', slug: 'aram-plumbing', phone: '+37499111222' },
  history: [{ status: 'Confirmed', changedAt: '2026-10-05T11:00:00Z', by: 'Customer', note: null }],
  createdAt: '2026-10-05T11:00:00Z',
  startDate: null,
  durationDays: null,
  visitAt: null,
  startedAt: null,
  completionRequestedAt: null,
  autoCompleteAt: null,
  completedAt: null,
  cancelledAt: null,
  cancelledBy: null,
  cancelReason: null,
  needsAttentionSince: null,
  changeRequests: [],
  payments: [],
  paidAmount: 0,
  review: null,
  actions: ['proposeChange', 'cancel'],
  ...overrides,
})

export const payment = (overrides = {}) => ({
  id: 'payment-1',
  stageId: null,
  stageTitle: null,
  amount: 40000,
  method: 'Cash',
  paidOn: '2026-10-06',
  note: null,
  recordedBy: 'Partner',
  mine: false,
  status: 'Pending',
  recordedAt: '2026-10-06T12:00:00Z',
  answeredAt: null,
  disputeReason: null,
  resolvedAt: null,
  resolutionNote: null,
  ...overrides,
})

export const orderReview = (overrides = {}) => ({
  id: 'review-1',
  rating: 5,
  text: 'Quick and tidy work.',
  submittedAt: '2026-10-09T10:00:00Z',
  reply: null,
  repliedAt: null,
  isHidden: false,
  hiddenReason: null,
  ...overrides,
})

export const publicReview = (overrides = {}) => ({
  id: 'review-1',
  rating: 4,
  text: 'Fixed the leak in an hour.',
  customerName: 'Ani',
  submittedAt: '2026-10-09T10:00:00Z',
  reply: null,
  repliedAt: null,
  ...overrides,
})

export const notification = (overrides = {}) => ({
  id: 'notification-1',
  type: 'OfferReceived',
  link: '/requests/req-1',
  params: { name: 'Aram Plumbing', kind: 'Work', price: '25000' },
  createdAt: '2026-10-06T09:00:00Z',
  readAt: null,
  ...overrides,
})

export const orderChange = (overrides = {}) => ({
  id: 'change-1',
  kind: 'ExtraWork',
  status: 'Pending',
  proposedBy: 'Partner',
  mine: false,
  title: 'Replace the siphon',
  description: 'It is cracked.',
  amount: 20000,
  newStartDate: null,
  newDurationDays: null,
  newVisitAt: null,
  responseNote: null,
  proposedAt: '2026-10-06T09:00:00Z',
  decidedAt: null,
  ...overrides,
})

export const orderItem = (overrides = {}) => ({
  id: 'order-1',
  kind: 'Work',
  status: 'Confirmed',
  myRole: 'Customer',
  place: PLACE,
  summary: 'Replace the tap and the pipes under the sink.',
  price: 100000,
  otherParty: 'Aram Plumbing',
  startDate: '2026-10-08',
  visitAt: null,
  createdAt: '2026-10-05T11:00:00Z',
  ...overrides,
})

export const conversation = (overrides = {}) => ({
  id: 'conv-1',
  requestId: 'req-1',
  requestStatus: 'Open',
  place: PLACE,
  myRole: 'Customer',
  otherParty: { name: 'Aram Plumbing', partnerId: 'partner-1', slug: null },
  unreadCount: 0,
  lastMessage: { senderRole: 'Partner', excerpt: 'When can I come?', attachmentCount: 0, sentAt: '2026-10-05T10:00:00Z' },
  otherReadAt: null,
  canSend: true,
  orderId: null,
  ...overrides,
})

export const message = (overrides = {}) => ({
  id: 'msg-1',
  conversationId: 'conv-1',
  senderRole: 'Partner',
  body: 'When can I come?',
  attachments: [],
  sentAt: '2026-10-05T10:00:00Z',
  ...overrides,
})

const media = (name, overrides = {}) => ({ kind: 'Image', caption: null, url: `https://files/${name}.jpg`, thumbnailUrl: `https://files/${name}-t.jpg`, width: 10, height: 10, ...overrides })

export const partnerCard = (overrides = {}) => ({
  id: 'partner-1',
  slug: 'aram-plumbing',
  displayName: 'Aram Plumbing',
  type: 'Specialist',
  aboutExcerpt: 'Twenty years of fixing taps and pipes.',
  yearsOfExperience: 20,
  avatar: null,
  cover: media('bathroom'),
  categories: [{ slug: 'plumbing', name: 'Plumbing' }],
  cities: ['Yerevan'],
  workExampleCount: 3,
  rating: null,
  reviewCount: 0,
  ...overrides,
})

export const publicPartner = (overrides = {}) => ({
  id: 'partner-1',
  slug: 'aram-plumbing',
  displayName: 'Aram Plumbing',
  type: 'Specialist',
  about: 'Twenty years of fixing taps and pipes.\nClean and on time.',
  yearsOfExperience: 20,
  avatar: media('aram'),
  categories: [{ slug: 'plumbing', name: 'Plumbing' }, { slug: 'boilers', name: 'Boilers' }],
  areas: [
    { citySlug: 'yerevan', cityName: 'Yerevan', districtSlug: 'kentron', districtName: 'Kentron' },
    { citySlug: 'yerevan', cityName: 'Yerevan', districtSlug: 'arabkir', districtName: 'Arabkir' },
    { citySlug: 'masis', cityName: 'Masis', districtSlug: null, districtName: null },
  ],
  workExamples: [media('bathroom', { caption: 'A new bathroom' }), media('boiler', { kind: 'Video', url: 'https://files/boiler.mp4', thumbnailUrl: null })],
  memberSince: '2026-03-01T10:00:00Z',
  rating: null,
  reviewCount: 0,
  ...overrides,
})

const file = (id, overrides = {}) => ({
  id,
  kind: 'Image',
  status: 'Ready',
  fileName: `${id}.jpg`,
  contentType: 'image/jpeg',
  size: 100,
  width: 10,
  height: 10,
  url: `https://files/${id}.jpg`,
  thumbnailUrl: `https://files/${id}-t.jpg`,
  urlExpiresAt: null,
  createdAt: '2026-10-01T10:00:00Z',
  ...overrides,
})

export const partnerMedia = (id, overrides = {}) => ({ id: `media-${id}`, caption: null, sortOrder: 1, file: file(id), ...overrides })

export const partnerProfile = (overrides = {}) => ({
  id: 'partner-1',
  slug: null,
  type: 'Specialist',
  status: 'Draft',
  displayName: 'Aram Plumbing',
  about: 'Twenty years of fixing taps and pipes in Yerevan. Clean, careful and on time.',
  yearsOfExperience: 20,
  avatar: null,
  categoryIds: ['cat-plumbing'],
  areas: [{ cityId: 'city-yerevan', districtId: null }],
  workExamples: [partnerMedia('work-1')],
  documents: [],
  canEdit: true,
  canSubmit: true,
  missingForSubmit: [],
  reviewComment: null,
  submittedAt: null,
  ...overrides,
})

export const commissionSummary = (overrides = {}) => ({
  unbilled: 3000,
  outstanding: 10000,
  overdue: 0,
  nextDueOn: '2026-10-19',
  pausedSince: null,
  pauseAfterOverdueDays: 14,
  ...overrides,
})

export const myStatement = (overrides = {}) => ({
  id: 'statement-1',
  periodStart: '2026-10-05',
  periodEnd: '2026-10-11',
  total: 15000,
  paidAmount: 5000,
  outstanding: 10000,
  status: 'Open',
  overdue: false,
  dueOn: '2026-10-19',
  issuedAt: '2026-10-12T09:00:00Z',
  paidAt: null,
  ...overrides,
})

export const commissionLine = (overrides = {}) => ({
  id: 'line-1',
  orderId: 'order-1',
  summary: 'Replace the kitchen tap',
  completedAt: '2026-10-06T12:00:00Z',
  orderPrice: 150000,
  ratePercent: 10,
  amount: 15000,
  statementId: 'statement-1',
  ...overrides,
})

export const statementDetail = (overrides = {}) => ({
  statement: myStatement(),
  lines: [commissionLine()],
  settlements: [{ id: 'settlement-1', amount: 5000, method: 'Cash', paidOn: '2026-10-13', reference: 'Receipt 7', recordedAt: '2026-10-13T10:00:00Z' }],
  ...overrides,
})

/** A work item on the partner's price list as GET /me/partner-profile/prices returns it. */
export const priceItem = (overrides = {}) => ({
  workItemId: 'wi-toilet',
  slug: 'toilet-installation',
  name: 'Toilet installation',
  unit: 'Piece',
  categoryId: 'cat-fixtures',
  categoryName: 'Fixture installation',
  mainCategoryId: 'cat-plumbing',
  mainCategoryName: 'Plumbing',
  marketMin: 10000,
  marketTypical: 15000,
  marketMax: 22000,
  priceFrom: null,
  priceTo: null,
  includesMaterials: false,
  ...overrides,
})

/** A partner's price list: a toilet (priced), a faucet (not priced) and a leak repair (no market price). */
export const priceList = () => {
  const items = [
    priceItem({ priceFrom: 14000, priceTo: 18000 }),
    priceItem({ workItemId: 'wi-faucet', slug: 'faucet-installation', name: 'Faucet installation', marketMin: 4000, marketTypical: 6000, marketMax: 9000 }),
    priceItem({
      workItemId: 'wi-leak',
      slug: 'leak-repair',
      name: 'Leak repair',
      unit: 'Fixed',
      categoryId: 'cat-leaks',
      categoryName: 'Leak repair',
      marketMin: null,
      marketTypical: null,
      marketMax: null,
    }),
  ]
  return { items, pricedCount: 1 }
}
