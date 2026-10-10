/**
 * The estimate being edited, as plain data the page keeps in state (and, for visitors, in the browser):
 * `{ title, oldBuilding, rooms: [room] }`. Numbers are kept as the text typed ("4,2"), so half-typed values don't jump
 * around; they're read when the estimate is priced or saved.
 *
 * room: `{ key, name, type, sizeMode: 'sides' | 'area', length, width, area, height, openings: [opening], lines: [line] }`
 * opening: `{ key, kind: 'Door' | 'Window', width, height, count }`
 * line: `{ workItemId, quantity, perSquareMeter }`: `quantity` '' means "work it out": measured from the room, or
 * `perSquareMeter` × floor area (sockets) when the template gives one.
 */

export const LIMITS = {
  side: 100,
  area: 2000,
  minHeight: 1.5,
  maxHeight: 10,
  defaultHeight: 2.7,
  openingSize: 10,
  openingCount: 50,
  quantity: 100_000,
  rooms: 30,
  openings: 20,
  lines: 60,
  title: 120,
  name: 60,
}

/** Usual sizes of a new door and window (m). */
export const OPENING_DEFAULTS = {
  Door: { width: '0.9', height: '2' },
  Window: { width: '1.4', height: '1.5' },
}

const DRAFT_KEY = 'hs_estimate_draft'

let counter = 0
/** A key for React lists and for finding a room again. */
export const newKey = (prefix = 'k') => `${prefix}-${Date.now().toString(36)}-${(counter += 1)}`

/** "4,2" or "4.2" → 4.2; blank → null; anything else → NaN. */
export function parseNumber(text) {
  const cleaned = String(text ?? '').trim().replace(',', '.')
  if (cleaned === '') return null
  return /^\d+(\.\d+)?$/.test(cleaned) ? Number(cleaned) : Number.NaN
}

const within = (value, min, max) => typeof value === 'number' && value > min && value <= max

/** What's wrong with a room's sizes and openings: `{ field: i18nKey }` and `openings: [{ field: i18nKey }]`. */
export function roomProblems(room) {
  const problems = {}
  if (room.sizeMode === 'sides') {
    if (!within(parseNumber(room.length), 0, LIMITS.side)) problems.length = 'estimator.errors.side'
    if (!within(parseNumber(room.width), 0, LIMITS.side)) problems.width = 'estimator.errors.side'
  } else if (!within(parseNumber(room.area), 0, LIMITS.area)) {
    problems.area = 'estimator.errors.roomArea'
  }

  const height = parseNumber(room.height)
  if (height !== null && !(height >= LIMITS.minHeight && height <= LIMITS.maxHeight)) problems.height = 'estimator.errors.height'

  const openings = room.openings.map((opening) => {
    const found = {}
    if (!within(parseNumber(opening.width), 0, LIMITS.openingSize)) found.width = 'estimator.errors.openingSize'
    if (!within(parseNumber(opening.height), 0, LIMITS.openingSize)) found.height = 'estimator.errors.openingSize'
    const count = parseNumber(opening.count)
    if (!Number.isInteger(count) || count < 1 || count > LIMITS.openingCount) found.count = 'estimator.errors.count'
    return found
  })
  if (openings.some((found) => Object.keys(found).length)) problems.openings = openings

  const lines = room.lines.map((line) => {
    const quantity = parseNumber(line.quantity)
    return quantity === null || within(quantity, 0, LIMITS.quantity) ? null : 'estimator.errors.quantity'
  })
  if (lines.some(Boolean)) problems.lines = lines

  return problems
}

/** True when the room can be measured (its sizes, openings and quantities make sense). */
export const isRoomValid = (room) => Object.keys(roomProblems(room)).length === 0

/** The floor area in m², or null while the sizes aren't valid. */
export function floorArea(room) {
  if (room.sizeMode === 'sides') {
    const length = parseNumber(room.length)
    const width = parseNumber(room.width)
    return within(length, 0, LIMITS.side) && within(width, 0, LIMITS.side) ? Math.round(length * width * 100) / 100 : null
  }
  const area = parseNumber(room.area)
  return within(area, 0, LIMITS.area) ? area : null
}

/** The quantity sent for a line: the customer's, else per m² of floor when the template gives a rate, else null (measured). */
export function lineQuantity(line, room) {
  const typed = parseNumber(line.quantity)
  if (typed !== null) return Number.isNaN(typed) ? null : typed
  const area = floorArea(room)
  return line.perSquareMeter && area ? Math.max(1, Math.ceil(area * line.perSquareMeter)) : null
}

/** A room as the API takes it. */
export function toRoomInput(room) {
  const bySides = room.sizeMode === 'sides'
  return {
    name: room.name.trim(),
    type: room.type,
    length: bySides ? parseNumber(room.length) : null,
    width: bySides ? parseNumber(room.width) : null,
    area: bySides ? null : parseNumber(room.area),
    height: parseNumber(room.height),
    openings: room.openings.map((opening) => ({
      kind: opening.kind,
      width: parseNumber(opening.width),
      height: parseNumber(opening.height),
      count: parseNumber(opening.count),
    })),
    lines: room.lines.map((line) => ({ workItemId: line.workItemId, quantity: lineQuantity(line, room) })),
  }
}

/** The whole estimate as the API saves it. */
export const toRequest = (draft) => ({
  title: draft.title.trim(),
  cityId: null,
  oldBuilding: draft.oldBuilding,
  rooms: draft.rooms.map(toRoomInput),
})

/**
 * What to price: the rooms that can be measured, and `indexes[i]` = which draft room the i-th measured room is.
 * Null when no room can be measured yet.
 */
export function measureRequest(draft) {
  const indexes = draft.rooms.map((room, index) => (isRoomValid(room) ? index : -1)).filter((index) => index >= 0)
  if (!indexes.length) return null
  return {
    body: { oldBuilding: draft.oldBuilding, rooms: indexes.map((index) => toRoomInput(draft.rooms[index])) },
    indexes,
  }
}

/** A name for a new room of the type: "Bedroom", then "Bedroom 2", "Bedroom 3"… */
export function roomName(typeName, rooms) {
  const taken = new Set(rooms.map((room) => room.name.trim()))
  if (!taken.has(typeName)) return typeName
  let number = 2
  while (taken.has(`${typeName} ${number}`)) number += 1
  return `${typeName} ${number}`
}

/** A new room of the type, with the work its template ticks by default. */
export function newRoom(type, name, template) {
  return {
    key: newKey('room'),
    name,
    type,
    sizeMode: 'sides',
    length: '',
    width: '',
    area: '',
    height: '',
    openings: [],
    lines: (template?.items ?? []).map((item) => ({
      workItemId: item.workItemId,
      quantity: item.quantity != null ? String(item.quantity) : '',
      perSquareMeter: item.quantityPerSquareMeter ?? null,
    })),
  }
}

export const newOpening = (kind) => ({ key: newKey('opening'), kind, ...OPENING_DEFAULTS[kind], count: '1' })

export const emptyDraft = (title = '') => ({ title, oldBuilding: false, rooms: [] })

const text = (value) => (value == null ? '' : String(value))

/** A saved estimate from the API as a draft to edit. */
export function fromEstimate(estimate) {
  return {
    title: estimate.title,
    oldBuilding: estimate.oldBuilding,
    rooms: estimate.rooms.map((room) => ({
      key: room.id ?? newKey('room'),
      name: room.name,
      type: room.type,
      sizeMode: room.length != null ? 'sides' : 'area',
      length: text(room.length),
      width: text(room.width),
      area: text(room.area),
      height: room.height === LIMITS.defaultHeight ? '' : text(room.height),
      openings: room.openings.map((opening) => ({
        key: newKey('opening'),
        kind: opening.kind,
        width: text(opening.width),
        height: text(opening.height),
        count: text(opening.count),
      })),
      lines: room.lines.map((line) => ({ workItemId: line.workItemId, quantity: text(line.quantity), perSquareMeter: null })),
    })),
  }
}

/** True when two drafts would save the same estimate. */
export const sameDraft = (a, b) => JSON.stringify(toRequest(a)) === JSON.stringify(toRequest(b))

/** True when the draft has nothing worth keeping. */
export const isBlank = (draft) => !draft || (!draft.title.trim() && draft.rooms.length === 0)

/** The visitor's estimate kept in this browser, or null. Storage can be missing or blocked, which reads as none. */
export function loadLocalDraft() {
  try {
    const draft = JSON.parse(window.localStorage.getItem(DRAFT_KEY) ?? 'null')
    return draft && typeof draft.title === 'string' && Array.isArray(draft.rooms) ? draft : null
  } catch {
    return null
  }
}

export function saveLocalDraft(draft) {
  try {
    if (isBlank(draft)) window.localStorage.removeItem(DRAFT_KEY)
    else window.localStorage.setItem(DRAFT_KEY, JSON.stringify(draft))
  } catch {
    // Private windows and full storage: the draft just isn't kept.
  }
}

export function clearLocalDraft() {
  try {
    window.localStorage.removeItem(DRAFT_KEY)
  } catch {
    // Nothing to clear.
  }
}
