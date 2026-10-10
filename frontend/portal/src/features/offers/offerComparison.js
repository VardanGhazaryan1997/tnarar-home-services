/**
 * The comparison table of work offers priced against a request's lines, or null when no offer answers them.
 * `rows[i].entries[j]`: offer j's line for request line i (`{ included, amount }`) or null; `rows[i].best`: the lowest
 * amount when at least two offers priced the line; `extras[j]`: offer j's priced lines that answer no request line.
 */
export function compareOffers(lines = [], offers = []) {
  const priced = offers.filter(
    (offer) => offer.kind === 'Work' && !['Withdrawn', 'Rejected', 'Expired'].includes(offer.status) && offer.lines.some((line) => line.requestLineId),
  )
  if (!lines.length || !priced.length) return null

  const rows = lines.map((line) => {
    const entries = priced.map((offer) => offer.lines.find((offerLine) => offerLine.requestLineId === line.id) ?? null)
    const amounts = entries.filter((entry) => entry?.included && entry.amount != null).map((entry) => entry.amount)
    return { line, entries, best: amounts.length > 1 ? Math.min(...amounts) : null }
  })
  const extras = priced.map((offer) =>
    offer.lines.filter((line) => !line.requestLineId && line.included).reduce((sum, line) => sum + (line.amount ?? 0), 0),
  )
  const withEstimate = lines.filter((line) => line.estimateMax != null)
  const estimate = withEstimate.length
    ? { min: withEstimate.reduce((sum, line) => sum + line.estimateMin, 0), max: withEstimate.reduce((sum, line) => sum + line.estimateMax, 0) }
    : null

  return { offers: priced, rows, extras, estimate }
}
