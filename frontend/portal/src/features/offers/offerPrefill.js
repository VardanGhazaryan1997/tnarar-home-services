import { todayIso } from '@/shared/format'
import { initialRows } from './linePricing'

let nextKey = 0
const key = () => `from-${(nextKey += 1)}`
const text = (value) => (value == null ? '' : String(value))

/** An ISO time as a datetime-local value in the browser's time zone; '' for none or a past time. */
function localInput(iso) {
  if (!iso || new Date(iso) <= new Date()) return ''
  const at = new Date(iso)
  const pad = (n) => String(n).padStart(2, '0')
  return `${at.getFullYear()}-${pad(at.getMonth() + 1)}-${pad(at.getDate())}T${pad(at.getHours())}:${pad(at.getMinutes())}`
}

/**
 * The builder's state filled in from an earlier offer, to change it or start a new one from it. Lines answering the
 * request's lines go into the line-by-line rows; other lines are extra lines. Past dates are left out, and the single
 * final payment the server adds to an offer without a plan isn't a plan.
 */
export function fromOffer(offer, requestLines, priceItems) {
  const answers = new Map(offer.lines.filter((line) => line.requestLineId).map((line) => [line.requestLineId, line]))
  const rows = initialRows(requestLines, priceItems).map((row) => {
    const line = answers.get(row.requestLineId)
    if (!line) return row
    return {
      ...row,
      included: line.included,
      quantity: line.quantity != null ? String(line.quantity) : row.quantity,
      unitPrice: line.unitPrice != null ? String(line.unitPrice) : row.unitPrice,
      source: line.unitPrice != null ? null : row.source,
    }
  })
  const onlyFinal = offer.stages.length === 1 && offer.stages[0].purpose === 'Final' && offer.stages[0].amount === offer.price
  return {
    kind: offer.kind,
    form: {
      summary: offer.summary,
      price: text(offer.price),
      materialsIncluded: offer.materialsIncluded,
      materialsNote: offer.materialsNote ?? '',
      materialsAmount: text(offer.materialsAmount),
      startDate: offer.startDate && offer.startDate >= todayIso() ? offer.startDate : '',
      durationDays: text(offer.durationDays),
      visitAt: localInput(offer.visitAt),
      validDays: '7',
    },
    byLine: requestLines.length === 0 || answers.size > 0,
    rows,
    lines: offer.lines.filter((line) => !line.requestLineId).map((line) => ({ key: key(), title: line.title, included: line.included, price: text(line.amount) })),
    stages: onlyFinal ? [] : offer.stages.map((stage) => ({ key: key(), title: stage.title ?? '', purpose: stage.purpose, amount: text(stage.amount) })),
  }
}

