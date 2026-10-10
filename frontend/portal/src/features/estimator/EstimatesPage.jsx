import { useTranslation } from 'react-i18next'
import { Link, useLocation } from 'react-router'
import PageHeader from '@/components/PageHeader/PageHeader'
import QueryState from '@/components/QueryState/QueryState'
import Button from '@/components/ui/Button/Button'
import Card from '@/components/ui/Card/Card'
import EmptyState from '@/components/ui/EmptyState/EmptyState'
import Icon from '@/components/ui/Icon/Icon'
import Tag from '@/components/ui/Tag/Tag'
import { useLocalizedPath } from '@/i18n/hooks'
import { formatDate, formatMoney } from '@/shared/format'
import { isBlank, loadLocalDraft } from './draft'
import { useGetMyEstimatesQuery } from './estimatorApi'
import { useSignedIn } from './useSignedIn'
import styles from './estimator.module.scss'

/**
 * Estimates: the signed-in user's saved estimates, and an estimate started on this device that isn't saved yet.
 * Visitors can start one without signing in.
 */
export default function EstimatesPage() {
  const { t } = useTranslation()
  const path = useLocalizedPath()
  const location = useLocation()
  const signedIn = useSignedIn()
  const draft = loadLocalDraft()
  const hasDraft = !isBlank(draft)

  return (
    <div className={styles['estimator-page']}>
      <PageHeader
        title={t('estimator.list.title')}
        subtitle={t('estimator.list.subtitle')}
        actions={
          <Button variant="accent" icon={<Icon name="plus" />} to={path('/estimates/new')}>
            {hasDraft ? t('estimator.list.continue') : t('estimator.list.new')}
          </Button>
        }
      />
      {hasDraft && (
        <Card title={t('estimator.list.draftTitle')}>
          <p className={styles['estimates__draft']}>
            <Link to={path('/estimates/new')}>{draft.title || t('estimator.editor.defaultTitle')}</Link>
            <span className={styles['estimates__meta']}>{t('estimator.list.rooms', { number: draft.rooms.length })}</span>
          </p>
          <p className={styles['estimates__meta']}>{signedIn ? t('estimator.list.draftSave') : t('estimator.editor.keptHere')}</p>
        </Card>
      )}
      {signedIn ? (
        <SavedList />
      ) : (
        !hasDraft && (
          <EmptyState
            icon="money"
            title={t('estimator.list.visitorTitle')}
            description={t('estimator.list.visitorText')}
            action={
              <Button variant="secondary" to={path('/sign-in')} state={{ from: location }}>
                {t('estimator.list.signIn')}
              </Button>
            }
          />
        )
      )}
    </div>
  )
}

function SavedList() {
  const { t, i18n } = useTranslation()
  const path = useLocalizedPath()
  const query = useGetMyEstimatesQuery()
  const lng = i18n.language

  return (
    <QueryState query={query}>
      {(estimates) =>
        estimates.length === 0 ? (
          <EmptyState icon="money" title={t('estimator.list.empty')} description={t('estimator.list.emptyText')} />
        ) : (
          <ul className={styles.estimates}>
            {estimates.map((estimate) => (
              <li key={estimate.id}>
                <Link to={path(`/estimates/${estimate.id}`)} className={styles['estimates__item']}>
                  <span className={styles['estimates__title']}>{estimate.title}</span>
                  <span className={styles['estimates__price']}>
                    {estimate.totalTypical > 0 ? `${formatMoney(estimate.totalMin, lng)} – ${formatMoney(estimate.totalMax, lng)}` : t('estimator.list.noPrice')}
                  </span>
                  <span className={styles['estimates__meta']}>
                    {t('estimator.list.rooms', { number: estimate.roomCount })} · {formatDate(estimate.updatedAt ?? estimate.createdAt, lng)}
                    {estimate.isShared && (
                      <>
                        {' '}
                        <Tag tone="info">{t('estimator.list.shared')}</Tag>
                      </>
                    )}
                  </span>
                </Link>
              </li>
            ))}
          </ul>
        )
      }
    </QueryState>
  )
}
