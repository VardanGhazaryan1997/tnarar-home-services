import { priceList } from '@/test/fixtures'
import { draftErrors, draftFrom, fillWithTypical, groupItems, isChanged, parsePrice, pricedCount, rowError, toPayload } from './priceList'

const row = (from = '', to = '', materials = false) => ({ from, to, materials })

describe('price list helpers', () => {
  it.each([
    ['', null],
    ['  ', null],
    ['15000', 15000],
    ['15 000', 15000],
    ['15,000', 15000],
    ['15 000', 15000],
    ['abc', Number.NaN],
    ['-5', Number.NaN],
  ])('reads %j as %s', (text, expected) => {
    expect(parsePrice(text)).toBe(expected)
  })

  it.each([
    [row(), null],
    [row('5000'), null],
    [row('5000', '5000'), null],
    [row('5000', '9000'), null],
    [row('', '9000'), 'partner.prices.errors.fromRequired'],
    [row('0'), 'partner.prices.errors.invalid'],
    [row('12.5x'), 'partner.prices.errors.invalid'],
    [row('100', 'abc'), 'partner.prices.errors.invalid'],
    [row('100000001'), 'partner.prices.errors.tooHigh'],
    [row('9000', '5000'), 'partner.prices.errors.toBelowFrom'],
  ])('checks %j', (value, expected) => {
    expect(rowError(value)).toBe(expected)
  })

  it('turns items into rows and rows into the API body', () => {
    const { items } = priceList()
    const draft = draftFrom(items)

    expect(draft['wi-toilet']).toEqual(row('14000', '18000'))
    expect(draft['wi-faucet']).toEqual(row())
    expect(pricedCount(draft)).toBe(1)
    expect(isChanged(draft, items)).toBe(false)

    const edited = { ...draft, 'wi-faucet': row('7000', '7000', true) }
    expect(isChanged(edited, items)).toBe(true)
    expect(toPayload(edited)).toEqual([
      { workItemId: 'wi-toilet', priceFrom: 14000, priceTo: 18000, includesMaterials: false },
      { workItemId: 'wi-faucet', priceFrom: 7000, priceTo: null, includesMaterials: true },
    ])
    expect(draftErrors({ ...edited, 'wi-leak': row('', '5') })).toEqual({ 'wi-leak': 'partner.prices.errors.fromRequired' })
  })

  it('fills only empty rows that have a usual price', () => {
    const { items } = priceList()
    const filled = fillWithTypical(draftFrom(items), items)

    expect(filled['wi-toilet']).toEqual(row('14000', '18000'))
    expect(filled['wi-faucet']).toEqual(row('6000'))
    expect(filled['wi-leak']).toEqual(row())
  })

  it('groups by main category and subcategory and searches', () => {
    const { items } = priceList()

    const groups = groupItems(items)
    expect(groups).toHaveLength(1)
    expect(groups[0].subcategories.map((sub) => [sub.name, sub.items.map((item) => item.slug)])).toEqual([
      ['Fixture installation', ['toilet-installation', 'faucet-installation']],
      ['Leak repair', ['leak-repair']],
    ])
    expect(groupItems(items, 'FAUCET')[0].subcategories[0].items.map((item) => item.slug)).toEqual(['faucet-installation'])
    expect(groupItems(items, 'fixture')[0].subcategories[0].items).toHaveLength(2)
    expect(groupItems(items, 'zzz')).toEqual([])
  })
})
