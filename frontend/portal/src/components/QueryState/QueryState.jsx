import { useTranslation } from 'react-i18next'
import { errorMessage } from '@/api/errors'
import Alert from '@/components/ui/Alert/Alert'
import Button from '@/components/ui/Button/Button'
import Spinner from '@/components/ui/Spinner/Spinner'
import styles from './QueryState.module.scss'

/**
 * Renders `children` once a query has data; until then a spinner, or the error with a retry button.
 * `notFound` replaces the error for 404s.
 */
export default function QueryState({ query, notFound, children }) {
  const { t } = useTranslation()

  if (query.isLoading) {
    return (
      <div className={styles['query-state']}>
        <Spinner size="lg" label={t('common.loading')} />
      </div>
    )
  }

  if (query.isError && !query.data) {
    if (query.error?.status === 404 && notFound) return notFound
    return (
      <div className={styles['query-state']}>
        <Alert tone="danger" title={errorMessage(t, query.error)}>
          <Button variant="secondary" size="sm" onClick={query.refetch}>
            {t('common.retry')}
          </Button>
        </Alert>
      </div>
    )
  }

  return children(query.data)
}
