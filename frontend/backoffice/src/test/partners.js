import { CATEGORIES, CITIES, REGIONS } from './catalog'

const pick = (name, language) => name[language] ?? name.hy

/** GET /categories (public): the catalog tree with names in one language. */
export const publicCategories = (language = 'hy') => {
  const map = (category) => ({ id: category.id, slug: category.slug, name: pick(category.name, language), icon: category.icon, children: category.children.map(map) })
  return CATEGORIES.map(map)
}

/** GET /regions (public): regions with names in one language. */
export const publicRegions = (language = 'hy') => REGIONS.map((region) => ({ id: region.id, slug: region.slug, name: pick(region.name, language) }))

/** GET /cities (public): cities and districts with names in one language. */
export const publicCities = (language = 'hy') =>
  CITIES.map((city) => ({
    id: city.id,
    slug: city.slug,
    name: pick(city.name, language),
    districts: city.districts.map((district) => ({ id: district.id, slug: district.slug, name: pick(district.name, language) })),
  }))

const file = (id, kind, name, overrides = {}) => ({
  id,
  kind,
  status: 'Ready',
  fileName: name,
  contentType: kind === 'Image' ? 'image/jpeg' : kind === 'Video' ? 'video/mp4' : 'application/pdf',
  size: 1000,
  width: kind === 'Image' ? 1600 : null,
  height: kind === 'Image' ? 1200 : null,
  url: `https://storage.test/${id}`,
  thumbnailUrl: kind === 'Image' ? `https://storage.test/${id}/thumbnail` : null,
  urlExpiresAt: '2026-10-05T10:00:00Z',
  createdAt: '2026-10-01T09:00:00Z',
  ...overrides,
})

/** A row of GET /admin/partners. */
export const partnerRow = (overrides = {}) => ({
  id: 'partner-aram',
  displayName: 'Արամ Սանտեխնիկ',
  type: 'Specialist',
  status: 'UnderReview',
  userId: 'user-aram',
  phone: '+37477123456',
  ownerName: 'Արամ Պետրոսյան',
  serviceCount: 2,
  areaCount: 1,
  submittedAt: '2026-10-04T10:30:00Z',
  createdAt: '2026-10-01T09:00:00Z',
  ...overrides,
})

export const page = (items, overrides = {}) => ({ items, page: 1, pageSize: 20, totalCount: items.length, totalPages: 1, ...overrides })

/** GET /admin/partners/{id}. */
export const partnerDetail = ({ status = 'UnderReview', reviewComment = null, ...profile } = {}) => ({
  profile: {
    id: 'partner-aram',
    slug: 'aram-santekhnik-3f9a2c',
    type: 'Specialist',
    status,
    displayName: 'Արամ Սանտեխնիկ',
    about: 'Տեղադրում եմ ջրատաքացուցիչներ և վերանորոգում խողովակներ 12 տարի։',
    yearsOfExperience: 12,
    avatar: null,
    categoryIds: ['cat-plumbing', 'cat-tiling'],
    areas: [
      { cityId: 'city-yerevan', districtId: 'district-kentron' },
      { cityId: 'city-masis', districtId: null },
    ],
    workExamples: [
      { id: 'media-1', caption: 'Լոգարան', sortOrder: 1, file: file('file-1', 'Image', 'bathroom.jpg') },
      { id: 'media-2', caption: null, sortOrder: 2, file: file('file-2', 'Video', 'boiler.mp4') },
    ],
    documents: [{ id: 'media-3', caption: 'Լիցենզիա', sortOrder: 1, file: file('file-3', 'Document', 'license.pdf') }],
    canEdit: false,
    canSubmit: false,
    missingForSubmit: [],
    reviewComment,
    submittedAt: '2026-10-04T10:30:00Z',
    ...profile,
  },
  owner: { userId: 'user-aram', phone: '+37477123456', fullName: 'Արամ Պետրոսյան', email: 'aram@example.com', isBlocked: false },
  history: [
    { sequence: 1, fromStatus: 'Draft', toStatus: 'UnderReview', comment: null, at: '2026-10-04T10:30:00Z', actorId: 'user-aram', actorName: 'Արամ Պետրոսյան' },
  ],
})
