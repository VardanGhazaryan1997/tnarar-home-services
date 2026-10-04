// jsdom has no matchMedia; Ant Design's Grid needs it to pick breakpoints.
// setViewportWidth() lets a test switch between phone and desktop layouts.
let viewportWidth = 1280
const listeners = new Set()

const evaluate = (query) => {
  const min = /min-width:\s*(\d+)px/.exec(query)
  const max = /max-width:\s*(\d+)px/.exec(query)
  return (!min || viewportWidth >= Number(min[1])) && (!max || viewportWidth <= Number(max[1]))
}

export function installMatchMedia() {
  window.matchMedia = (query) => {
    const mql = {
      media: query,
      get matches() {
        return evaluate(query)
      },
      onchange: null,
      addListener: (fn) => listeners.add({ mql, fn }),
      removeListener: (fn) => listeners.forEach((l) => l.fn === fn && listeners.delete(l)),
      addEventListener: (_, fn) => listeners.add({ mql, fn }),
      removeEventListener: (_, fn) => listeners.forEach((l) => l.fn === fn && listeners.delete(l)),
      dispatchEvent: () => true,
    }
    return mql
  }
}

export function setViewportWidth(width) {
  viewportWidth = width
  listeners.forEach(({ mql, fn }) => fn({ matches: mql.matches, media: mql.media }))
}
