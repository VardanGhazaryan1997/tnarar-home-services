import { useTranslation } from 'react-i18next'
import { useParams } from 'react-router'
import PageHeader from '@/components/PageHeader/PageHeader'
import QueryState from '@/components/QueryState/QueryState'
import Button from '@/components/ui/Button/Button'
import Card from '@/components/ui/Card/Card'
import EmptyState from '@/components/ui/EmptyState/EmptyState'
import { intlLocale } from '@/i18n/languages'
import { useLocalizedPath } from '@/i18n/hooks'
import { formatDate, formatMoney } from '@/shared/format'
import { useSharedEstimate } from './estimatorApi'
import styles from './estimator.module.scss'

/** An estimate someone shared: rooms, work and prices, read-only, with a way to make one's own. */
export default function SharedEstimatePage() {
  const { t, i18n } = useTranslation()
  const path = useLocalizedPath()
  const { token } = useParams()
  const query = useSharedEstimate(token)
  const lng = i18n.language
  const number = (value) => new Intl.NumberFormat(intlLocale(lng), { maximumFractionDigits: 2 }).format(value)
  const range = (from, to) => `${formatMoney(from, lng)} – ${formatMoney(to, lng)}`

  return (
    <div className={styles['estimator-page']}>
      <QueryState
        query={query}
        notFound={
          <EmptyState
            icon="file"
            title={t('estimator.shared.notFound')}
            action={<Button to={path('/estimates/new')}>{t('estimator.shared.makeYourOwn')}</Button>}
          />
        }
      >
        {(estimate) => {
          const { measurement } = estimate
          return (
            <>
              <PageHeader
                title={estimate.title}
                subtitle={t('estimator.shared.subtitle', { date: formatDate(estimate.updatedAt, lng) })}
                actions={<Button to={path('/estimates/new')}>{t('estimator.shared.makeYourOwn')}</Button>}
              />
              <Card>
                <div className={styles['estimate-summary']}>
                  <p className={styles['quick-estimate__label']}>{t('estimator.quick.resultLabel')}</p>
                  <p className={styles['quick-estimate__range']}>{range(measurement.totalMin, measurement.totalMax)}</p>
                  <p className={styles['quick-estimate__typical']}>{t('estimator.quick.usually', { amount: formatMoney(measurement.totalTypical, lng) })}</p>
                  {estimate.oldBuilding && <p className={styles['estimate-summary__note']}>{t('estimator.shared.oldBuilding')}</p>}
                  <p className={styles['quick-estimate__note']}>{t('estimator.quick.note')}</p>
                </div>
              </Card>
              {estimate.rooms.map((room, index) => {
                const measured = measurement.rooms[index]
                return (
                  <Card key={room.id} title={room.name} aria-label={room.name}>
                    <p className={styles['room__geometry']}>
                      {t(`estimator.roomTypes.${room.type}`)} ·{' '}
                      {t('estimator.room.geometry', { floor: number(measured.floorArea), walls: number(measured.wallArea), perimeter: number(measured.perimeter) })}
                    </p>
                    <ul className={styles['quick-estimate__lines']}>
                      {measured.lines.map((line) => (
                        <li key={line.workItemId} className={styles['quick-estimate__line']}>
                          <span>
                            {line.name}
                            {line.quantity != null && (
                              <span className={styles['quick-estimate__quantity']}>
                                {' · '}
                                {number(line.quantity)} {t(`estimator.units.${line.unit}`)}
                              </span>
                            )}
                          </span>
                          <span className={styles['quick-estimate__line-price']}>
                            {line.priceTypical != null ? range(line.priceMin, line.priceMax) : t('estimator.room.noPrice')}
                          </span>
                        </li>
                      ))}
                    </ul>
                    {measured.totalTypical > 0 && (
                      <p className={styles['room__total']}>{t('estimator.room.total', { range: range(measured.totalMin, measured.totalMax) })}</p>
                    )}
                  </Card>
                )
              })}
            </>
          )
        }}
      </QueryState>
    </div>
  )
}
