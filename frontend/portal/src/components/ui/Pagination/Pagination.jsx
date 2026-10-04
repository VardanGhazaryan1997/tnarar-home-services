import { useTranslation } from 'react-i18next'
import Button from '../Button/Button'
import Icon from '../Icon/Icon'
import styles from './Pagination.module.scss'

/** Previous / next for a paged list. Hidden when everything fits on one page. */
export default function Pagination({ page, pageSize, totalCount, onChange }) {
  const { t } = useTranslation()
  const pages = Math.max(1, Math.ceil(totalCount / pageSize))
  if (pages <= 1) return null

  return (
    <nav className={styles.pagination} aria-label={t('pagination.label')}>
      <Button variant="secondary" size="sm" icon={<Icon name="chevronLeft" />} disabled={page <= 1} onClick={() => onChange(page - 1)}>
        {t('pagination.previous')}
      </Button>
      <span className={styles['pagination__status']}>{t('pagination.status', { page, pages })}</span>
      <Button variant="secondary" size="sm" disabled={page >= pages} onClick={() => onChange(page + 1)}>
        {t('pagination.next')}
      </Button>
    </nav>
  )
}
