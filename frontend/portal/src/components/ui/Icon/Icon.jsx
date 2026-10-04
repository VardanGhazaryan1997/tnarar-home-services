import { ICON_PATHS } from './icons'

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
    >
      <path d={ICON_PATHS[name]} />
    </svg>
  )
}
