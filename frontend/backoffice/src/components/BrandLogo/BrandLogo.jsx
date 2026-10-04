import { BRAND } from '@/theme/theme'

/** The house mark: roof, a tuff-brick wall and a sparkle (cleaning + construction). */
export function BrandMark({ size = 32, tone = 'light' }) {
  const onDark = tone === 'dark'
  const wall = onDark ? BRAND.cleanMist : BRAND.araratNight
  const sparkle = onDark ? BRAND.araratNight : '#ffffff'
  const accent = onDark ? BRAND.tuffLight : BRAND.tuffStone

  return (
    <svg width={size} height={size} viewBox="0 0 64 64" aria-hidden="true" focusable="false">
      <path d="M8 30 32 9l24 21" fill="none" stroke={accent} strokeWidth="6" strokeLinecap="round" strokeLinejoin="round" />
      <rect x="16" y="26" width="32" height="28" rx="4" fill={wall} />
      <path d="M39 30q1.1 5.9 7 7-5.9 1.1-7 7-1.1-5.9-7-7 5.9-1.1 7-7Z" fill={sparkle} />
      <path d="M44 43q.5 2.5 3 3-2.5.5-3 3-.5-2.5-3-3 2.5-.5 3-3Z" fill={sparkle} />
      <rect x="20" y="44" width="6" height="3" rx="1" fill={accent} />
      <rect x="27" y="44" width="6" height="3" rx="1" fill={accent} />
      <rect x="23.5" y="48.5" width="6" height="3" rx="1" fill={accent} />
    </svg>
  )
}

/**
 * The Tnarar logo: the mark and the wordmark "Tnarar" ("Tna" in Ararat Night, "rar" in Tuff Stone).
 * `tone="dark"` is for dark backgrounds; `compact` shows the mark only.
 */
export default function BrandLogo({ size = 32, tone = 'light', compact = false }) {
  const onDark = tone === 'dark'

  return (
    <span role="img" aria-label="Tnarar" style={{ display: 'inline-flex', alignItems: 'center', gap: size / 4, lineHeight: 1 }}>
      <BrandMark size={size} tone={tone} />
      {!compact && (
        <span aria-hidden="true" style={{ fontFamily: "'Noto Sans Armenian', sans-serif", fontWeight: 700, fontSize: size * 0.7, letterSpacing: 0.2 }}>
          <span style={{ color: onDark ? '#ffffff' : BRAND.araratNight }}>Tna</span>
          <span style={{ color: onDark ? BRAND.tuffLight : BRAND.tuffStone }}>rar</span>
        </span>
      )}
    </span>
  )
}
