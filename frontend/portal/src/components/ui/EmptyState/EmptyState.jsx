import Icon from '../Icon/Icon'
import styles from './EmptyState.module.scss'

/** What a list shows when it has nothing yet: an icon, a title, a line of help and an optional action. */
export default function EmptyState({ icon = 'inbox', title, description, action }) {
  return (
    <div className={styles['empty-state']}>
      <span className={styles['empty-state__icon']}>
        <Icon name={icon} size={28} />
      </span>
      <p className={styles['empty-state__title']}>{title}</p>
      {description && <p className={styles['empty-state__description']}>{description}</p>}
      {action && <div className={styles['empty-state__action']}>{action}</div>}
    </div>
  )
}
