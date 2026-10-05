import { ICON_PATHS } from './icons'

/** Icons that point back or forward; they are mirrored in right-to-left languages (see global.scss). */
const DIRECTIONAL = new Set(['chevronLeft', 'chevronRight', 'send', 'logout'])

// Line icons (24×24, 2px stroke) drawn for the Portal. Decorative by default; pass `label` when an
// icon is the only content of a control.

export default function Icon({ name, size = 20, label, className }) {
  return (
    <svg
      className={className}
      width={size}
      height={size}
      viewBox="0 0 24 24"
      fill="none"
      stroke="currentColor"
      strokeWidth="2"
      strokeLinecap="round"
      strokeLinejoin="round"
      focusable="false"
      role={label ? 'img' : undefined}
      aria-label={label}
      aria-hidden={label ? undefined : true}
      data-directional={DIRECTIONAL.has(name) || undefined}
    >
      <path d={ICON_PATHS[name]} />
    </svg>
  )
}
