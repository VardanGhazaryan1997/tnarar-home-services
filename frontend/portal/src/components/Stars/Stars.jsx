import { useId } from 'react'
import { useTranslation } from 'react-i18next'
import { ICON_PATHS } from '@/components/ui/Icon/icons'
import { bem } from '@/shared/bem'
import styles from './Stars.module.scss'

const b = bem(styles)
const STARS = [1, 2, 3, 4, 5]

function Star({ filled, size }) {
  return (
    <svg width={size} height={size} viewBox="0 0 24 24" className={b('stars__star', { filled })} aria-hidden="true" focusable="false">
      <path d={ICON_PATHS.star} strokeWidth="2" strokeLinejoin="round" />
    </svg>
  )
}

/** A rating as five stars (rounded to the nearest whole star), read out as "4.5 out of 5". */
export default function Stars({ value, size = 16, className }) {
  const { t, i18n } = useTranslation()
  const rounded = Math.round(value)
  const label = t('reviews.ratingLabel', { rating: value.toLocaleString(i18n.language, { maximumFractionDigits: 1 }) })
  return (
    <span className={[styles.stars, className].filter(Boolean).join(' ')} role="img" aria-label={label}>
      {STARS.map((star) => (
        <Star key={star} filled={star <= rounded} size={size} />
      ))}
    </span>
  )
}

/** Picking 1–5 stars: a radio group, so it works with the keyboard and screen readers. */
export function StarInput({ label, value, onChange, error }) {
  const { t } = useTranslation()
  const name = useId()
  return (
    <fieldset className={styles['star-input']}>
      <legend className={styles['star-input__label']}>{label}</legend>
      <div className={styles['star-input__stars']}>
        {STARS.map((star) => (
          <label key={star} className={styles['star-input__option']}>
            <input
              type="radio"
              name={name}
              value={star}
              checked={value === star}
              onChange={() => onChange(star)}
              className={styles['star-input__radio']}
              aria-label={t('reviews.stars', { n: star })}
            />
            <Star filled={star <= value} size={32} />
          </label>
        ))}
      </div>
      {error && (
        <p className={styles['star-input__error']} role="alert">
          {error}
        </p>
      )}
    </fieldset>
  )
}
