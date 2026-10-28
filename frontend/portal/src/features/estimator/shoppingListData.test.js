import en from '@/i18n/locales/en/common.json'
import { atOneStore, byCheapest, toText } from './shoppingListData'

// A tiny t(): looks the key up in the English texts and fills {{placeholders}}.
const t = (key, values = {}) => {
  const text = key.split('.').reduce((node, part) => node?.[part], en) ?? key
  return text.replace(/\{\{(\w+)\}\}/g, (_, name) => values[name] ?? '')
}

const MEASUREMENT = {
  materialGrade: 'Standard',
  materials: [
    { materialId: 'm-putty', name: 'Finishing putty', unit: 'Kilogram', quantity: 60 },
    { materialId: 'm-paint', name: 'Interior paint', unit: 'Liter', quantity: 9.5 },
    { materialId: 'm-tiles', name: 'Floor tiles', unit: 'SquareMeter', quantity: 4 },
  ],
}

const offer = (materialId, store, cost, extra = {}) => ({
  materialId,
  materialSlug: materialId,
  storeName: store === 'a' ? 'Alpha' : 'Beta',
  storeSlug: store,
  productName: `${materialId} product`,
  packLabel: '25 kg bag',
  packSize: 25,
  packs: 3,
  cost,
  ...extra,
})

const DATA = {
  cheapest: [offer('m-putty', 'b', 6000), offer('m-paint', 'a', 8000, { packLabel: null, packSize: 10, packs: 1 })],
  stores: [
    { storeName: 'Alpha', storeSlug: 'a', total: 15500, covered: 2, missing: 1, items: [offer('m-putty', 'a', 7500), offer('m-paint', 'a', 8000)] },
    { storeName: 'Beta', storeSlug: 'b', total: 6000, covered: 1, missing: 2, items: [offer('m-putty', 'b', 6000)] },
  ],
}

describe('shopping list', () => {
  it('groups each material under the store selling it cheapest and lists what no store has', () => {
    const list = byCheapest(DATA, MEASUREMENT)

    expect(list.groups.map((g) => [g.storeSlug, g.items.map((i) => i.name), g.total])).toEqual([
      ['b', ['Finishing putty'], 6000],
      ['a', ['Interior paint'], 8000],
    ])
    expect(list.missing).toEqual([{ name: 'Floor tiles', quantity: 4, unit: 'SquareMeter' }])
    expect(list.total).toBe(14000)
  })

  it('lists everything one store has, the rest as missing', () => {
    const list = atOneStore(DATA, MEASUREMENT, 'b')

    expect(list.groups).toHaveLength(1)
    expect(list.groups[0].items.map((i) => [i.name, i.cost])).toEqual([['Finishing putty', 6000]])
    expect(list.missing.map((m) => m.name)).toEqual(['Interior paint', 'Floor tiles'])
    expect(list.total).toBe(6000)
    expect(atOneStore(DATA, MEASUREMENT, 'nowhere')).toEqual({ groups: [], missing: expect.any(Array), total: 0 })
  })

  it('writes the list as plain text', () => {
    const text = toText(byCheapest(DATA, MEASUREMENT), t, 'en', 'Shopping list: Standard materials')

    expect(text).toBe(
      [
        'Shopping list: Standard materials',
        '',
        'Beta',
        '• Finishing putty — 3 × 25 kg bag — 6,000 ֏',
        'Store total: 6,000 ֏',
        '',
        'Alpha',
        '• Interior paint — 1 × 10 l — 8,000 ֏',
        'Store total: 8,000 ֏',
        '',
        'Not found in stores',
        '• Floor tiles (4 m²)',
        '',
        'Total: 14,000 ֏',
      ].join('\n'),
    )
  })
})
