/** The partner profile form: limits (the same as the API's) and conversions to and from the API shape. */

export const LIMITS = {
  nameMin: 2,
  nameMax: 100,
  aboutMin: 50,
  aboutMax: 2000,
  yearsMax: 70,
  services: 20,
  areas: 30,
  workExamples: 30,
  documents: 10,
}

export const STEPS = ['type', 'services', 'areas', 'about', 'work', 'review']

export const PARTNER_TYPES = ['Specialist', 'Company', 'Supplier']

/** Which step holds the field an API error is about. */
export const STEP_OF_FIELD = {
  type: 'type',
  displayName: 'type',
  yearsOfExperience: 'type',
  categoryIds: 'services',
  areas: 'areas',
  about: 'about',
  avatarFileId: 'about',
}

/** What `missingForSubmit` names → the step that fixes it. */
export const STEP_OF_MISSING = { about: 'about', services: 'services', areas: 'areas', work_examples: 'work' }

export function fromProfile(profile) {
  return {
    type: profile?.type ?? 'Specialist',
    displayName: profile?.displayName ?? '',
    yearsOfExperience: profile?.yearsOfExperience ?? '',
    about: profile?.about ?? '',
    avatarFileId: profile?.avatar?.id ?? null,
    avatarUrl: profile?.avatar?.thumbnailUrl ?? profile?.avatar?.url ?? null,
    categoryIds: profile?.categoryIds ?? [],
    areas: profile?.areas ?? [],
  }
}

export function toPayload(form) {
  return {
    type: form.type,
    displayName: form.displayName.trim(),
    yearsOfExperience: form.yearsOfExperience === '' ? null : Number(form.yearsOfExperience),
    about: form.about.trim(),
    avatarFileId: form.avatarFileId,
    categoryIds: form.categoryIds,
    areas: form.areas.map((area) => ({ cityId: area.cityId, districtId: area.districtId ?? null })),
  }
}

/** Checks a step before saving it; returns { field: translation key } (empty when fine). */
export function stepErrors(step, form) {
  const errors = {}
  if (step === 'type') {
    const name = form.displayName.trim().length
    if (name < LIMITS.nameMin || name > LIMITS.nameMax) errors.displayName = 'partner.errors.name'
    const years = form.yearsOfExperience
    if (years !== '' && (!Number.isInteger(Number(years)) || Number(years) < 0 || Number(years) > LIMITS.yearsMax)) errors.yearsOfExperience = 'partner.errors.years'
  }
  if (step === 'services' && form.categoryIds.length === 0) errors.categoryIds = 'partner.errors.services'
  if (step === 'areas' && form.areas.length === 0) errors.areas = 'partner.errors.areas'
  if (step === 'about' && form.about.trim().length < LIMITS.aboutMin) errors.about = 'partner.errors.about'
  return errors
}

/** True when the area list covers this city or district. */
export const hasArea = (areas, cityId, districtId = null) => areas.some((area) => area.cityId === cityId && (area.districtId ?? null) === districtId)

const MB = 1024 * 1024

/** Profile photos and logos. */
export const IMAGE_TYPES = { 'image/jpeg': 10 * MB, 'image/png': 10 * MB, 'image/webp': 10 * MB }

/** Documents (ID, license, registration): photos or PDFs. */
export const DOCUMENT_TYPES = { ...IMAGE_TYPES, 'application/pdf': 10 * MB }

/** Area names: "Yerevan" for the whole city, "Yerevan — Kentron" for a district. */
export function areaNames(areas, cities = []) {
  return areas.map((area) => {
    const city = cities.find((item) => item.id === area.cityId)
    const district = city?.districts.find((item) => item.id === area.districtId)
    return district ? `${city.name} — ${district.name}` : (city?.name ?? '')
  })
}
