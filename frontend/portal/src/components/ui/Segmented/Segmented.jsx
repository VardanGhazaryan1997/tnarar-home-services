import { bem } from '@/shared/bem'
import styles from './Segmented.module.scss'

const b = bem(styles)

/** A row of mutually exclusive choices (filters, kinds). `options`: [{ value, label }]. */
export default function Segmented({ label, options, value, onChange, className }) {
  return (
    <div className={b('segmented', null, className)} role="radiogroup" aria-label={label}>
      {options.map((option) => {
        const selected = option.value === value
        return (
          <button
            key={option.value}
            type="button"
            role="radio"
            aria-checked={selected}
            className={b('segmented__option', { selected })}
            onClick={() => onChange(option.value)}
          >
            {option.label}
          </button>
        )
      })}
    </div>
  )
}
