import {
  clearLocalDraft,
  emptyDraft,
  floorArea,
  fromEstimate,
  isBlank,
  isRoomValid,
  lineQuantity,
  loadLocalDraft,
  measureRequest,
  newOpening,
  newRoom,
  parseNumber,
  roomName,
  roomProblems,
  sameDraft,
  saveLocalDraft,
  toRequest,
} from './draft'

const TEMPLATE = {
  type: 'Bedroom',
  items: [
    { workItemId: 'paint', quantity: null, quantityPerSquareMeter: null },
    { workItemId: 'sockets', quantity: null, quantityPerSquareMeter: 0.25 },
    { workItemId: 'lights', quantity: 1, quantityPerSquareMeter: null },
  ],
}

const bedroom = (patch = {}) => ({ ...newRoom('Bedroom', 'Bedroom', TEMPLATE), length: '4,2', width: '3', ...patch })

describe('estimate drafts', () => {
  it.each([
    ['4,2', 4.2],
    [' 3 ', 3],
    ['', null],
    ['abc', Number.NaN],
    ['-1', Number.NaN],
  ])('reads %j as %s', (text, value) => expect(parseNumber(text)).toBe(value))

  it('starts rooms with the template’s work', () => {
    const room = newRoom('Bedroom', 'Bedroom', TEMPLATE)
    expect(room.lines).toEqual([
      { workItemId: 'paint', quantity: '', perSquareMeter: null },
      { workItemId: 'sockets', quantity: '', perSquareMeter: 0.25 },
      { workItemId: 'lights', quantity: '1', perSquareMeter: null },
    ])
    expect(isRoomValid(room)).toBe(false)
    expect(roomProblems(room)).toEqual({ length: 'estimator.errors.side', width: 'estimator.errors.side' })
  })

  it('works out floor areas and quantities', () => {
    const room = bedroom()
    expect(floorArea(room)).toBe(12.6)
    expect(floorArea({ ...room, sizeMode: 'area', area: '20' })).toBe(20)
    expect(lineQuantity(room.lines[0], room)).toBeNull() // measured on the server
    expect(lineQuantity(room.lines[1], room)).toBe(4) // 12.6 × 0.25 → 3.15 → 4
    expect(lineQuantity(room.lines[2], room)).toBe(1)
    expect(lineQuantity({ ...room.lines[1], quantity: '6' }, room)).toBe(6)
    expect(lineQuantity(room.lines[1], { ...room, length: '' })).toBeNull()
  })

  it('checks heights, openings and quantities', () => {
    const room = bedroom({ height: '1', openings: [{ ...newOpening('Door'), count: '0' }] })
    room.lines[0] = { ...room.lines[0], quantity: '0' }
    const problems = roomProblems(room)
    expect(problems.height).toBe('estimator.errors.height')
    expect(problems.openings).toEqual([{ count: 'estimator.errors.count' }])
    expect(problems.lines).toEqual(['estimator.errors.quantity', null, null])
  })

  it('builds the API request and prices only rooms that can be measured', () => {
    const draft = { ...emptyDraft(' Flat '), oldBuilding: true, rooms: [bedroom({ openings: [newOpening('Window')] }), bedroom({ length: '' })] }

    const request = toRequest(draft)
    expect(request.title).toBe('Flat')
    expect(request.rooms[0]).toEqual({
      name: 'Bedroom',
      type: 'Bedroom',
      length: 4.2,
      width: 3,
      area: null,
      height: null,
      openings: [{ kind: 'Window', width: 1.4, height: 1.5, count: 1 }],
      lines: [
        { workItemId: 'paint', quantity: null },
        { workItemId: 'sockets', quantity: 4 },
        { workItemId: 'lights', quantity: 1 },
      ],
    })

    const measure = measureRequest(draft)
    expect(measure.indexes).toEqual([0])
    expect(measure.body.oldBuilding).toBe(true)
    expect(measure.body.rooms).toHaveLength(1)
    expect(measureRequest(emptyDraft())).toBeNull()
  })

  it('names rooms without repeating', () => {
    expect(roomName('Bedroom', [])).toBe('Bedroom')
    expect(roomName('Bedroom', [{ name: 'Bedroom' }, { name: 'Bedroom 2' }])).toBe('Bedroom 3')
  })

  it('turns a saved estimate into a draft that saves the same', () => {
    const saved = {
      title: 'Flat',
      oldBuilding: false,
      rooms: [
        {
          id: 'r1',
          name: 'Bath',
          type: 'Bathroom',
          length: null,
          width: null,
          area: 4.4,
          height: 2.7,
          openings: [{ kind: 'Door', width: 0.8, height: 2, count: 1 }],
          lines: [{ workItemId: 'toilet', quantity: 1 }, { workItemId: 'tiles', quantity: null }],
        },
      ],
    }
    const draft = fromEstimate(saved)

    expect(draft.rooms[0]).toMatchObject({ key: 'r1', sizeMode: 'area', area: '4.4', height: '' })
    expect(toRequest(draft).rooms[0].lines).toEqual(saved.rooms[0].lines)
    expect(sameDraft(draft, fromEstimate(saved))).toBe(true)
    expect(sameDraft(draft, { ...draft, title: 'Other' })).toBe(false)
  })

  it('keeps a visitor’s draft in the browser', () => {
    expect(loadLocalDraft()).toBeNull()
    const draft = { ...emptyDraft('Flat'), rooms: [bedroom()] }

    saveLocalDraft(draft)
    expect(loadLocalDraft()).toEqual(draft)

    saveLocalDraft(emptyDraft(''))
    expect(loadLocalDraft()).toBeNull()
    saveLocalDraft(draft)
    clearLocalDraft()
    expect(loadLocalDraft()).toBeNull()

    localStorage.setItem('hs_estimate_draft', '{broken')
    expect(loadLocalDraft()).toBeNull()
    expect(isBlank(null)).toBe(true)
  })
})
