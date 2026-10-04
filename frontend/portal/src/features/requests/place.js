/** "Plumbing · Yerevan, Kentron" for a request's place DTO. */
export function placeText(place, t) {
  const where = place.districtName ? `${place.cityName}, ${place.districtName}` : place.cityName || t('requests.anyPlace')
  return `${place.categoryName} · ${where}`
}
