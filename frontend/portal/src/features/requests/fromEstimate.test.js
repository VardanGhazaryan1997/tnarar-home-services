import { mainCategoryOf } from './fromEstimate'

describe('a request from an estimate', () => {
  it('starts from the service most of the work belongs to', () => {
    const categories = [
      { id: 'renovation', children: [{ id: 'tiling' }, { id: 'painting' }] },
      { id: 'plumbing', children: [{ id: 'toilets' }] },
    ]
    const workItems = [
      { id: 'tiles', categoryId: 'tiling' },
      { id: 'paint', categoryId: 'painting' },
      { id: 'toilet', categoryId: 'toilets' },
    ]
    const estimate = { rooms: [{ lines: [{ workItemId: 'tiles' }, { workItemId: 'toilet' }] }, { lines: [{ workItemId: 'paint' }, { workItemId: 'gone' }] }] }

    expect(mainCategoryOf(estimate, workItems, categories)).toBe('renovation')
    expect(mainCategoryOf({ rooms: [] }, workItems, categories)).toBeNull()
  })
})
