import { intlLocale } from '@/i18n/languages'
import { formatMoney } from '@/shared/format'

/**
 * The shopping list for a measurement's materials (R5), from the where-to-buy answer. A list is
 * `{ groups: [{ storeName, storeSlug, items, total }], missing, total }`: an item is `{ materialId, name, unit,
 * packs, packLabel, packSize, cost }`, and `missing` the materials no listed store sells (`{ name, quantity, unit }`).
 */

const itemOf = (offer, material) => ({
  materialId: offer.materialId,
  name: material?.name ?? offer.productName,
  product: offer.productName,
  unit: material?.unit,
  packs: offer.packs,
  packLabel: offer.packLabel,
  packSize: offer.packSize,
  cost: offer.cost,
})

const sum = (items) => items.reduce((total, item) => total + item.cost, 0)

function build(groups, measurement, covered) {
  const missing = (measurement?.materials ?? [])
    .filter((material) => !covered.has(material.materialId))
    .map((material) => ({ name: material.name, quantity: material.quantity, unit: material.unit }))
  return { groups, missing, total: sum(groups.flatMap((group) => group.items)) }
}

/** Each material at the store selling it cheapest, grouped by store in the order the stores first come up. */
export function byCheapest(data, measurement) {
  const materials = new Map((measurement?.materials ?? []).map((material) => [material.materialId, material]))
  const groups = []
  for (const offer of data?.cheapest ?? []) {
    let group = groups.find((g) => g.storeSlug === offer.storeSlug)
    if (!group) {
      group = { storeName: offer.storeName, storeSlug: offer.storeSlug, items: [], total: 0 }
      groups.push(group)
    }
    group.items.push(itemOf(offer, materials.get(offer.materialId)))
  }
  for (const group of groups) group.total = sum(group.items)
  return build(groups, measurement, new Set((data?.cheapest ?? []).map((offer) => offer.materialId)))
}

/** Everything one store has (its cheapest product of each material); the rest is missing. */
export function atOneStore(data, measurement, storeSlug) {
  const materials = new Map((measurement?.materials ?? []).map((material) => [material.materialId, material]))
  const store = (data?.stores ?? []).find((s) => s.storeSlug === storeSlug)
  if (!store) return build([], measurement, new Set())
  const items = (store.items ?? []).map((offer) => itemOf(offer, materials.get(offer.materialId)))
  const group = { storeName: store.storeName, storeSlug: store.storeSlug, items, total: sum(items) }
  return build(items.length ? [group] : [], measurement, new Set(items.map((item) => item.materialId)))
}

const number = (value, lng) => new Intl.NumberFormat(intlLocale(lng), { maximumFractionDigits: 3 }).format(value)

/** "25 kg bag", or the pack size with the unit when the store gave no label. */
export const packText = (item, t, lng) => item.packLabel || `${number(item.packSize, lng)} ${t(`estimator.materials.units.${item.unit}`)}`

/** "Floor tiles — 3 × 1.44 m² box — 38,900 ֏" */
export const itemText = (item, t, lng) =>
  t('whereToBuy.list.item', { name: item.name, packs: item.packs, pack: packText(item, t, lng), cost: formatMoney(item.cost, lng) })

/** "Interior paint (9.5 l)" */
export const missingText = (material, t, lng) =>
  t('whereToBuy.list.missingItem', {
    name: material.name,
    quantity: number(material.quantity, lng),
    unit: t(`estimator.materials.units.${material.unit}`),
  })

/** The list as plain text, for copying and for WhatsApp. */
export function toText(list, t, lng, title) {
  const lines = [title]
  for (const group of list.groups) {
    lines.push('', group.storeName)
    for (const item of group.items) lines.push(`• ${itemText(item, t, lng)}`)
    lines.push(t('whereToBuy.list.storeTotal', { amount: formatMoney(group.total, lng) }))
  }
  if (list.missing.length) {
    lines.push('', t('whereToBuy.list.missing'))
    for (const material of list.missing) lines.push(`• ${missingText(material, t, lng)}`)
  }
  lines.push('', t('whereToBuy.list.total', { amount: formatMoney(list.total, lng) }))
  return lines.join('\n')
}
