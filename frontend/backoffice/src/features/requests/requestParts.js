export const REQUEST_STATUS_COLORS = { Open: 'processing', Closed: 'success', Cancelled: 'default' }

export const RECIPIENT_STATUS_COLORS = { New: 'default', Viewed: 'processing', Declined: 'error', Responded: 'success' }

/** "Plumbing · Yerevan, Kentron". */
export const placeText = (place) => `${place.categoryName} · ${[place.cityName, place.districtName].filter(Boolean).join(', ')}`
