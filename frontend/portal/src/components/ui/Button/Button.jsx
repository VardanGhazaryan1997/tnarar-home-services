import { Link } from 'react-router'
import { bem } from '@/shared/bem'
import Spinner from '../Spinner/Spinner'
import styles from './Button.module.scss'

const b = bem(styles)

/**
 * The Portal's button. `variant`: primary | accent | secondary | ghost | danger; `size`: md | sm | lg.
 * With `to` it renders a router link styled as a button. `loading` disables it and shows a spinner.
 */
export default function Button({
  variant = 'primary',
  size = 'md',
  block = false,
  loading = false,
  icon,
  to,
  type = 'button',
  disabled,
  className,
  children,
  ...rest
}) {
  const classes = b('button', { variant, size, block, loading, 'icon-only': !children }, className)
  const content = (
    <>
      {loading ? <Spinner size="sm" /> : icon}
      {children && <span className={styles['button__label']}>{children}</span>}
    </>
  )

  if (to) {
    return (
      <Link to={to} className={classes} {...rest}>
        {content}
      </Link>
    )
  }

  return (
    <button type={type} className={classes} disabled={disabled || loading} aria-busy={loading || undefined} {...rest}>
      {content}
    </button>
  )
}
