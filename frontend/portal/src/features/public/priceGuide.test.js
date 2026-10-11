import { priceGuide } from './priceGuide'

const CATEGORIES = [
  { id: 'renovation', name: 'Renovation', children: [{ id: 'tiling', name: 'Tiling' }, { id: 'painting', name: 'Painting' }] },
  { id: 'plumbing', name: 'Plumbing', children: [{ id: 'pipes', name: 'Pipes' }] },
]
const ITEMS = [
  { id: 'floor', categoryId: 'tiling', name: 'Floor tiling', priceTypical: 6500 },
  { id: 'wall', categoryId: 'tiling', name: 'Wall tiling', priceTypical: 7000 },
  { id: 'paint', categoryId: 'painting', name: 'Wall painting', priceTypical: 1500 },
  { id: 'new', categoryId: 'painting', name: 'Ceiling painting', priceTypical: null },
  { id: 'pipe', categoryId: 'pipes', name: 'Pipe replacement', priceTypical: 4000 },
]

const names = (guide) => guide.map((main) => [main.name, main.groups.map((group) => [group.name, group.items.map((item) => item.id)])])

describe('the price guide', () => {
  it('groups priced work by service', () => {
    expect(names(priceGuide(CATEGORIES, ITEMS))).toEqual([
      ['Renovation', [['Tiling', ['floor', 'wall']], ['Painting', ['paint']]]],
      ['Plumbing', [['Pipes', ['pipe']]]],
    ])
  })

  it('finds work by its name or its service', () => {
    expect(names(priceGuide(CATEGORIES, ITEMS, { search: 'wall' }))).toEqual([['Renovation', [['Tiling', ['wall']], ['Painting', ['paint']]]]])
    expect(names(priceGuide(CATEGORIES, ITEMS, { search: 'plumb' }))).toEqual([['Plumbing', [['Pipes', ['pipe']]]]])
    expect(priceGuide(CATEGORIES, ITEMS, { search: 'nothing' })).toEqual([])
  })

  it('keeps one service', () => {
    expect(names(priceGuide(CATEGORIES, ITEMS, { category: 'plumbing' }))).toEqual([['Plumbing', [['Pipes', ['pipe']]]]])
  })
})
