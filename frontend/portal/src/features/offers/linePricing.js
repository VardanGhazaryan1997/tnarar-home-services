/**
 * Pricing an offer line by line against the request's lines. A row is `{ requestLineId, room, name, unit, quantity,
 * unitPrice, included, source }`, numbers kept as typed; `source` says where the unit price came from: 'mine' (the
 * partner's price list), 'market' (the market's usual price) or null.
 */

/** "12,5" → 12.5, blank → null, anything else → NaN. */
export function parseAmount(text) {
  const cleaned = String(text ?? '').trim().replace(',', '.').replace(/\s/g, '')
  if (cleaned === '') return null
  return /^\d+(\.\d+)?$/.test(cleaned) ? Number(cleaned) : Number.NaN
}

/** The unit price to start from: the partner's own (the middle of a from–to range), else the market's usual one. */
export function suggestedPrice(item) {
  if (!item) return { price: null, source: null }
  if (item.priceFrom != null) return { price: item.priceTo != null ? Math.round((item.priceFrom + item.priceTo) / 2) : item.priceFrom, source: 'mine' }
  if (item.marketTypical != null) return { price: item.marketTypical, source: 'market' }
  return { price: null, source: null }
}

/** Rows for the request's lines, prices filled in from the partner's price list items. */
export function initialRows(lines, priceItems = []) {
  const byWork = new Map(priceItems.map((item) => [item.workItemId, item]))
  return lines.map((line) => {
    const { price, source } = suggestedPrice(byWork.get(line.workItemId))
    return {
      requestLineId: line.id,
      room: line.roomName,
      name: line.name,
      unit: line.unit,
      quantity: line.quantity != null ? String(line.quantity) : '',
      unitPrice: price != null ? String(price) : '',
      included: true,
      source,
    }
  })
}

/** Unit price × quantity in whole drams, or null when either is missing or not a number. */
export function rowAmount(row) {
  if (!row.included) return null
  const quantity = parseAmount(row.quantity)
  const price = parseAmount(row.unitPrice)
  if (!(quantity > 0) || price === null || Number.isNaN(price)) return null
  return Math.round(quantity * Math.round(price))
}

/** True when every included row has a quantity and a unit price. */
export const allPriced = (rows) => rows.every((row) => !row.included || rowAmount(row) !== null)

export const total = (rows) => rows.reduce((sum, row) => sum + (rowAmount(row) ?? 0), 0)

/** The rows as offer lines for the API. */
export const toOfferLines = (rows) =>
  rows.map((row) =>
    row.included
      ? {
          title: `${row.room} · ${row.name}`.slice(0, 200),
          included: true,
          requestLineId: row.requestLineId,
          quantity: parseAmount(row.quantity),
          unitPrice: Math.round(parseAmount(row.unitPrice)),
        }
      : { title: `${row.room} · ${row.name}`.slice(0, 200), included: false, requestLineId: row.requestLineId, quantity: null, unitPrice: null },
  )
