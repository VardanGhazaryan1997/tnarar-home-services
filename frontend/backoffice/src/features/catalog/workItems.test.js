import { CATEGORIES, WORK_ITEMS } from '@/test/catalog'
import { categoryLookup, filterWorkItems, marketRangeText, priceProblem, priceRangeText, subcategoryGroups } from './workItems'

describe('work item helpers', () => {
  it.each([
    [{}, null],
    [{ priceMin: 1, priceTypical: 2, priceMax: 3 }, null],
    [{ priceMin: 5, priceTypical: 5, priceMax: 5 }, null],
    [{ priceMin: 1 }, 'catalog.workItems.priceIncomplete'],
    [{ priceMin: 1, priceMax: 3 }, 'catalog.workItems.priceIncomplete'],
    [{ priceMin: 3, priceTypical: 2, priceMax: 4 }, 'catalog.workItems.priceOrder'],
    [{ priceMin: 1, priceTypical: 5, priceMax: 4 }, 'catalog.workItems.priceOrder'],
  ])('checks the price range %j', (values, expected) => {
    expect(priceProblem(values)).toBe(expected)
  })

  it('writes the range, or one price when the range is a single value', () => {
    expect(priceRangeText({ priceMin: 2500, priceTypical: 3500, priceMax: 5000 }, 'en')).toBe('2,500 – 5,000 ֏')
    expect(priceRangeText({ priceMin: 5000, priceTypical: 5000, priceMax: 5000 }, 'en')).toBe('5,000 ֏')
    expect(priceRangeText({ priceMin: null, priceTypical: null, priceMax: null }, 'en')).toBeNull()
    expect(marketRangeText({ marketMin: 6000, marketTypical: 7000, marketMax: 8000 }, 'en')).toBe('6,000 – 8,000 ֏')
    expect(marketRangeText({ marketTypical: null }, 'en')).toBeNull()
  })

  it('groups subcategories under their main categories and marks hidden ones', () => {
    const groups = subcategoryGroups(CATEGORIES, (c) => c.name.en, 'hidden')
    expect(groups.map((g) => g.label)).toEqual(['Renovation']) // Plumbing has no subcategories
    expect(groups[0].options).toEqual([{ value: 'cat-tiling', label: 'Tiling (hidden)' }])
  })

  it('filters by main category, status and text', () => {
    const lookup = categoryLookup(CATEGORIES, 'en', 'hy')
    const slugs = (filter) => filterWorkItems(WORK_ITEMS, { categoryIds: null, status: 'all', search: '', ...filter }).map((i) => i.slug)

    expect(slugs({ categoryIds: lookup.get('cat-plumbing').ids })).toEqual(['leak-repair'])
    expect(slugs({ status: 'hidden' })).toEqual(['floor-tiling'])
    expect(slugs({ status: 'active' })).toEqual(['wall-plastering', 'leak-repair'])
    expect(slugs({ status: 'unpriced' })).toEqual(['leak-repair'])
    expect(slugs({ search: 'WALL' })).toEqual(['wall-plastering'])
    expect(slugs({ search: 'սալիկ' })).toEqual(['floor-tiling'])
    expect(lookup.get('cat-tiling')).toMatchObject({ name: 'Tiling', main: 'Renovation' })
  })
})
