import { bem } from '@/shared/bem'
import styles from './Spinner.module.scss'

const b = bem(styles)

/** A loading indicator. With `label` it announces itself; without, it is decorative (e.g. inside a button). */
export default function Spinner({ size = 'md', label, className }) {
  return (
    <span
      className={b('spinner', { size }, className)}
      role={label ? 'status' : undefined}
      aria-label={label}
      aria-hidden={label ? undefined : true}
    />
  )
}
