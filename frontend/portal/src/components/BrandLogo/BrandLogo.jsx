import styles from './BrandLogo.module.scss'

/** The house mark: roof, a tuff-brick wall and a sparkle (construction + cleaning). Same drawing as the Back Office. */
export function BrandMark({ size = 32 }) {
  return (
    <svg className={styles['brand-logo__mark']} width={size} height={size} viewBox="0 0 64 64" aria-hidden="true" focusable="false">
      <path d="M8 30 32 9l24 21" fill="none" stroke="#c8643b" strokeWidth="6" strokeLinecap="round" strokeLinejoin="round" />
      <rect x="16" y="26" width="32" height="28" rx="4" fill="#1d3346" />
      <path d="M39 30q1.1 5.9 7 7-5.9 1.1-7 7-1.1-5.9-7-7 5.9-1.1 7-7Z" fill="#fff" />
      <path d="M44 43q.5 2.5 3 3-2.5.5-3 3-.5-2.5-3-3 2.5-.5 3-3Z" fill="#fff" />
      <rect x="20" y="44" width="6" height="3" rx="1" fill="#c8643b" />
      <rect x="27" y="44" width="6" height="3" rx="1" fill="#c8643b" />
      <rect x="23.5" y="48.5" width="6" height="3" rx="1" fill="#c8643b" />
    </svg>
  )
}

/** The Tnarar logo: the mark and the wordmark "Tnarar" ("Tna" in Ararat Night, "rar" in Tuff Stone). */
export default function BrandLogo({ size = 32 }) {
  return (
    <span className={styles['brand-logo']} role="img" aria-label="Tnarar">
      <BrandMark size={size} />
      <span className={styles['brand-logo__word']} aria-hidden="true">
        Tna<span className={styles['brand-logo__accent']}>rar</span>
      </span>
    </span>
  )
}
