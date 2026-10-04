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
    areas: form.areas.map((area) => ({ regionId: area.regionId ?? null, cityId: area.cityId ?? null, districtId: area.districtId ?? null })),
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

/** True when the area list has this city (whole) or this district of it. */
export const hasArea = (areas, cityId, districtId = null) => areas.some((area) => area.cityId === cityId && (area.districtId ?? null) === districtId)

/** True when the area list has this whole region. */
export const hasRegion = (areas, regionId) => areas.some((area) => area.regionId === regionId)

/** Adds (or removes) a whole region; adding drops the towns, villages and districts chosen inside it. */
export function withRegion(areas, regionId, places, on) {
  const inside = new Set(places.map((city) => city.id))
  const rest = areas.filter((area) => area.regionId !== regionId && !inside.has(area.cityId))
  return on ? [...rest, { regionId, cityId: null, districtId: null }] : rest
}

/** Adds (or removes) a whole town or village; adding drops its districts. */
export function withCity(areas, cityId, on) {
  const rest = areas.filter((area) => area.cityId !== cityId)
  return on ? [...rest, { regionId: null, cityId, districtId: null }] : rest
}

/** Adds (or removes) one district of a city. */
export function withDistrict(areas, cityId, districtId, on) {
  const rest = areas.filter((area) => !(area.cityId === cityId && area.districtId === districtId))
  return on ? [...rest, { regionId: null, cityId, districtId }] : rest
}

/** Cities grouped by region, in the regions' order; places without a known region come last. */
export function groupPlaces(cities = [], regions = []) {
  const groups = regions.map((region) => ({ region, places: cities.filter((city) => city.regionId === region.id) }))
  const known = new Set(regions.map((region) => region.id))
  const other = cities.filter((city) => !known.has(city.regionId))
  return [...groups.filter((group) => group.places.length), ...(other.length ? [{ region: null, places: other }] : [])]
}

const MB = 1024 * 1024

/** Profile photos and logos. */
export const IMAGE_TYPES = { 'image/jpeg': 10 * MB, 'image/png': 10 * MB, 'image/webp': 10 * MB }

/** Documents (ID, license, registration): photos or PDFs. */
export const DOCUMENT_TYPES = { ...IMAGE_TYPES, 'application/pdf': 10 * MB }

/** Area names: "Ararat (whole region)"-style region names, "Yerevan" for a whole city, "Yerevan — Kentron" for a district. */
export function areaNames(areas, cities = [], regions = [], regionLabel = (name) => name) {
  return areas.map((area) => {
    if (area.regionId) {
      const region = regions.find((item) => item.id === area.regionId)
      return region ? regionLabel(region.name) : ''
    }
    const city = cities.find((item) => item.id === area.cityId)
    const district = city?.districts.find((item) => item.id === area.districtId)
    return district ? `${city.name} — ${district.name}` : (city?.name ?? '')
  })
}
