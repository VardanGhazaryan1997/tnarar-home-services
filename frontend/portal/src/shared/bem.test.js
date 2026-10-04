import { bem } from './bem'

describe('bem', () => {
  const b = bem({ card: 'x-card', 'card--open': 'x-card--open', 'card__title': 'x-title' })

  it('maps names through the module and adds modifiers', () => {
    expect(b('card')).toBe('x-card')
    expect(b('card', { open: true, closed: false, tone: '' })).toBe('x-card x-card--open')
    expect(b('card__title', null, 'extra')).toBe('x-title extra')
  })

  it('uses string modifier values and keeps unknown names', () => {
    expect(b('card', { state: 'open' })).toBe('x-card x-card--open')
    expect(b('button', { variant: 'primary' })).toBe('button button--primary')
  })
})
