/** Room types in the order the estimator offers them. */
export const ROOM_TYPES = ['LivingRoom', 'Bedroom', 'KidsRoom', 'Kitchen', 'Bathroom', 'Toilet', 'Hallway', 'Balcony', 'Office', 'Garage', 'Other']

/** How a template line gets its quantity: measured from the room, a fixed count, or a count per m² of floor. */
export const MODES = ['measured', 'count', 'perSquareMeter']

/** The largest fixed count and count per m² the API takes. */
export const MAX_COUNT = 1000
export const MAX_PER_SQUARE_METER = 100

/** Most work lines a room can have. */
export const MAX_LINES = 60

/**
 * True when the work's quantity can be worked out from room sizes (the backend's RoomGeometry.CanMeasure): m² of any
 * surface, running metres of floor, walls or ceiling, and fixed-price jobs. Pieces, points, hours and m³ need a count.
 */
export function canMeasure(item) {
  if (!item) return false
  if (item.unit === 'SquareMeter' || item.unit === 'Fixed') return true
  return item.unit === 'RunningMeter' && ['Floor', 'Wall', 'Ceiling'].includes(item.surface)
}

/** A template's items from the API as editable lines: { workItemId, mode, value }. */
export const toLines = (items = []) =>
  items.map((item) => {
    if (item.quantity != null) return { workItemId: item.workItemId, mode: 'count', value: item.quantity }
    if (item.quantityPerSquareMeter != null) return { workItemId: item.workItemId, mode: 'perSquareMeter', value: item.quantityPerSquareMeter }
    return { workItemId: item.workItemId, mode: 'measured', value: null }
  })

/** Lines back as the API's template items. */
export const toInputs = (lines) =>
  lines.map((line) => ({
    workItemId: line.workItemId,
    quantity: line.mode === 'count' ? line.value : null,
    quantityPerSquareMeter: line.mode === 'perSquareMeter' ? line.value : null,
  }))

/** A new line for the work: measured when it can be, otherwise one piece. */
export const newLine = (item) => (canMeasure(item) ? { workItemId: item.id, mode: 'measured', value: null } : { workItemId: item.id, mode: 'count', value: 1 })

/** What's wrong with a line, as an i18n key, or null. */
export function lineProblem(line, item) {
  if (line.mode === 'measured') return canMeasure(item) ? null : 'catalog.roomTemplates.needsCount'
  const max = line.mode === 'count' ? MAX_COUNT : MAX_PER_SQUARE_METER
  if (line.value == null || !(line.value > 0) || line.value > max) return line.mode === 'count' ? 'catalog.roomTemplates.countInvalid' : 'catalog.roomTemplates.perSquareMeterInvalid'
  return null
}

/** True when two line lists are the same (order, modes and values). */
export const sameLines = (a, b) => JSON.stringify(toInputs(a)) === JSON.stringify(toInputs(b))

/** Moves the line at `index` by `step` (−1 up, +1 down). */
export function moveLine(lines, index, step) {
  const target = index + step
  if (target < 0 || target >= lines.length) return lines
  const next = [...lines]
  ;[next[index], next[target]] = [next[target], next[index]]
  return next
}
