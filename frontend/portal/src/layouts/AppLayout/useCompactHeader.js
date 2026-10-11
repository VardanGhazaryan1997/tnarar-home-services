import { useLayoutEffect, useState } from 'react'

const FULL = { compact: false, fullWidth: 0 }

/**
 * True while the header bar's content is wider than the bar — long translations (Armenian menus are about
 * 50% longer than English) or a partner's extra sections. The header menu then shows icons only.
 * It goes back to labels once the bar is as wide as the full menu needed. `resetKey` (language, signed-in
 * user) measures the full menu again.
 */
export function useCompactHeader(barRef, resetKey) {
  const [state, setState] = useState({ ...FULL, resetKey })
  if (state.resetKey !== resetKey) setState({ ...FULL, resetKey })
  const { compact, fullWidth } = state

  useLayoutEffect(() => {
    const bar = barRef.current
    if (!bar || typeof ResizeObserver === 'undefined') return undefined

    const check = () => {
      if (!compact) {
        if (bar.scrollWidth > bar.clientWidth + 1) setState({ compact: true, fullWidth: bar.scrollWidth, resetKey })
      } else if (bar.clientWidth >= fullWidth) {
        setState({ ...FULL, resetKey })
      }
    }

    check()
    // The bar for window resizes; its children for web fonts and badges that change their width.
    const observer = new ResizeObserver(check)
    observer.observe(bar)
    for (const child of bar.children) observer.observe(child)
    return () => observer.disconnect()
  }, [barRef, compact, fullWidth, resetKey])

  return compact
}
