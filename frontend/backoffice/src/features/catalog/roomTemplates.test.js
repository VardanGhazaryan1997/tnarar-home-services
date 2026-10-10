import { canMeasure, lineProblem, moveLine, newLine, sameLines, toInputs, toLines } from './roomTemplates'

describe('room templates', () => {
  it.each([
    ['SquareMeter', 'None', true],
    ['SquareMeter', 'Wall', true],
    ['RunningMeter', 'Floor', true],
    ['RunningMeter', 'None', false],
    ['Fixed', 'None', true],
    ['Piece', 'Wall', false],
    ['Point', 'None', false],
    ['Hour', 'None', false],
    ['CubicMeter', 'Floor', false],
  ])('%s on %s can be measured: %s', (unit, surface, expected) => expect(canMeasure({ unit, surface })).toBe(expected))

  it('turns template items into lines and back', () => {
    const items = [
      { workItemId: 'a', quantity: null, quantityPerSquareMeter: null },
      { workItemId: 'b', quantity: 2, quantityPerSquareMeter: null },
      { workItemId: 'c', quantity: null, quantityPerSquareMeter: 0.25 },
    ]
    const lines = toLines(items)

    expect(lines.map((line) => line.mode)).toEqual(['measured', 'count', 'perSquareMeter'])
    expect(toInputs(lines)).toEqual(items)
    expect(sameLines(lines, toLines(items))).toBe(true)
    expect(sameLines(lines, moveLine(lines, 0, 1))).toBe(false)
    expect(moveLine(lines, 0, -1)).toBe(lines)
  })

  it('starts new lines measured when it can', () => {
    expect(newLine({ id: 'a', unit: 'SquareMeter', surface: 'Wall' })).toEqual({ workItemId: 'a', mode: 'measured', value: null })
    expect(newLine({ id: 'b', unit: 'Piece', surface: 'None' })).toEqual({ workItemId: 'b', mode: 'count', value: 1 })
  })

  it('checks counts', () => {
    const piece = { unit: 'Piece', surface: 'None' }
    expect(lineProblem({ mode: 'measured' }, piece)).toBe('catalog.roomTemplates.needsCount')
    expect(lineProblem({ mode: 'count', value: 0 }, piece)).toBe('catalog.roomTemplates.countInvalid')
    expect(lineProblem({ mode: 'count', value: 1001 }, piece)).toBe('catalog.roomTemplates.countInvalid')
    expect(lineProblem({ mode: 'perSquareMeter', value: null }, piece)).toBe('catalog.roomTemplates.perSquareMeterInvalid')
    expect(lineProblem({ mode: 'perSquareMeter', value: 0.25 }, piece)).toBeNull()
    expect(lineProblem({ mode: 'measured' }, { unit: 'SquareMeter', surface: 'Floor' })).toBeNull()
  })
})
