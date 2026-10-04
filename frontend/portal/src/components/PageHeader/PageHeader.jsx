import { Link } from 'react-router'
import Icon from '@/components/ui/Icon/Icon'
import styles from './PageHeader.module.scss'

/** A page's title row: an optional back link above, a subtitle below and actions on the right. */
export default function PageHeader({ title, subtitle, back, actions }) {
  return (
    <header className={styles['page-header']}>
      {back && (
        <Link to={back.to} className={styles['page-header__back']}>
          <Icon name="chevronLeft" size={18} />
          {back.label}
        </Link>
      )}
      <div className={styles['page-header__row']}>
        <div className={styles['page-header__text']}>
          <h1 className={styles['page-header__title']}>{title}</h1>
          {subtitle && <p className={styles['page-header__subtitle']}>{subtitle}</p>}
        </div>
        {actions && <div className={styles['page-header__actions']}>{actions}</div>}
      </div>
    </header>
  )
}
