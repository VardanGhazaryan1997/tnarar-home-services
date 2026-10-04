import { useTranslation } from 'react-i18next'
import { Link } from 'react-router'
import { useLocalizedPath } from '@/i18n/hooks'
import styles from './NotFoundPage.module.scss'

export default function NotFoundPage() {
  const { t } = useTranslation()
  const localizedPath = useLocalizedPath()

  return (
    <section className={styles['not-found-page']}>
      <h1 className={styles['not-found-page__title']}>{t('notFound.title')}</h1>
      <Link className={styles['not-found-page__link']} to={localizedPath('/')}>
        {t('notFound.backHome')}
      </Link>
    </section>
  )
}
