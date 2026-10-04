import { bem } from '@/shared/bem'
import styles from './Alert.module.scss'

const b = bem(styles)

/** A message box. `tone`: info | success | warning | danger; danger and warning are announced right away. */
export default function Alert({ tone = 'info', title, className, children }) {
  const urgent = tone === 'danger' || tone === 'warning'
  return (
    <div className={b('alert', { tone }, className)} role={urgent ? 'alert' : 'status'}>
      {title && <p className={styles['alert__title']}>{title}</p>}
      {children && <div className={styles['alert__body']}>{children}</div>}
    </div>
  )
}
