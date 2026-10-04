/**
 * Builds BEM class names from an SCSS Module:
 *
 *   const b = bem(styles)
 *   b('offer-card', { accepted })            → "offer-card offer-card--accepted"
 *   b('button', { variant: 'primary' })      → "button button--primary"
 *   b('offer-card__price', null, className)  → "offer-card__price <className>"
 *
 * A modifier set to true adds `--name`; a string value adds `--value`; false/null/'' add nothing.
 */
export function bem(styles) {
  const cls = (name) => styles[name] ?? name
  return (name, modifiers, extra) => {
    const classes = [cls(name)]
    for (const [key, value] of Object.entries(modifiers ?? {})) {
      if (value === true) classes.push(cls(`${name}--${key}`))
      else if (typeof value === 'string' && value) classes.push(cls(`${name}--${value}`))
    }
    if (extra) classes.push(extra)
    return classes.join(' ')
  }
}
