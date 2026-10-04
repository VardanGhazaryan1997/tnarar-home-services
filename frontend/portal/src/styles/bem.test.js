import { readdirSync } from 'node:fs'
import { join, relative } from 'node:path'
import { compile } from 'sass'

// block, block__element, block--modifier, block__element--modifier (kebab-case)
const BEM_CLASS = /^[a-z][a-z0-9]*(-[a-z0-9]+)*(__[a-z0-9]+(-[a-z0-9]+)*)?(--[a-z0-9]+(-[a-z0-9]+)*)?$/

const SRC = join(import.meta.dirname, '..')

const scssFiles = (dir) =>
  readdirSync(dir, { withFileTypes: true }).flatMap((entry) => {
    const path = join(dir, entry.name)
    if (entry.isDirectory()) return scssFiles(path)
    return entry.name.endsWith('.scss') && !entry.name.startsWith('_') ? [path] : []
  })

// Class names in the compiled CSS selectors (declaration blocks removed first,
// so values such as 0.5rem or url(a.png) are never mistaken for classes).
const classNamesIn = (css) => {
  const selectors = css.replace(/\{[^{}]*\}/g, '{}')
  return [...new Set([...selectors.matchAll(/\.(-?[_a-zA-Z][\w-]*)/g)].map((m) => m[1]))]
}

describe('BEM class names', () => {
  it('validates names correctly', () => {
    expect(['offer-card', 'offer-card__title', 'offer-card--accepted', 'offer-card__price--old'].every((c) => BEM_CLASS.test(c))).toBe(true)
    expect(['offerCard', 'offer-card__title__text', 'offer_card', 'Offer-card'].some((c) => BEM_CLASS.test(c))).toBe(false)
  })

  it('extracts class names from selectors only', () => {
    expect(classNamesIn('.a__b .c--d{margin:0.5rem;background:url(x.png)}')).toEqual(['a__b', 'c--d'])
  })

  it.each(scssFiles(SRC).map((f) => [relative(SRC, f), f]))('%s uses only BEM class names', (_, file) => {
    const { css } = compile(file, { loadPaths: [SRC] })
    const invalid = classNamesIn(css).filter((name) => !BEM_CLASS.test(name))
    expect(invalid).toEqual([])
  })
})
