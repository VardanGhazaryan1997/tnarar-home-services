import { useTranslation } from 'react-i18next'
import Tag from '@/components/ui/Tag/Tag'
import { formatDateTime, formatMoney } from '@/shared/format'
import styles from './orders.module.scss'

const TONES = {
  Confirmed: 'info',
  InProgress: 'warning',
  CompletionRequested: 'warning',
  Completed: 'success',
  Cancelled: 'neutral',
}

export function OrderStatusTag({ status }) {
  const { t } = useTranslation()
  return <Tag tone={TONES[status] ?? 'neutral'}>{t(`orders.status.${status}`)}</Tag>
}

/** The planned payments of the order (accepted extra work included), with the total. */
export function PaymentPlan({ stages, total }) {
  const { t, i18n } = useTranslation()
  return (
    <div className={styles['payment-plan']}>
      <ol className={styles['payment-plan__list']}>
        {stages.map((stage, index) => (
          <li key={stage.id} className={styles['payment-plan__stage']}>
            <span className={styles['payment-plan__number']}>{index + 1}</span>
            <span className={styles['payment-plan__name']}>
              {stage.title ?? t(`offers.purpose.${stage.purpose}`)}
              {stage.title && <span className={styles['payment-plan__purpose']}>{t(`offers.purpose.${stage.purpose}`)}</span>}
            </span>
            <span className={styles['payment-plan__amount']}>{formatMoney(stage.amount, i18n.language)}</span>
          </li>
        ))}
      </ol>
      <p className={styles['payment-plan__total']}>
        <span>{t('orders.total')}</span>
        <span>{formatMoney(total, i18n.language)}</span>
      </p>
    </div>
  )
}

/** The status history as a timeline: what happened, who did it, when, and the reason given. */
export function OrderHistory({ history }) {
  const { t, i18n } = useTranslation()
  return (
    <ol className={styles['order-history']}>
      {history
        .map((change, index) => ({
          ...change,
          event: change.status === 'InProgress' && history[index - 1]?.status === 'CompletionRequested' ? 'Reopened' : change.status,
        }))
        .reverse()
        .map((change, index) => (
          <li key={`${change.status}-${change.changedAt}-${index}`} className={styles['order-history__event']}>
            <span className={styles['order-history__dot']} aria-hidden="true" />
            <div className={styles['order-history__body']}>
              <p className={styles['order-history__title']}>{t(`orders.event.${change.event}`)}</p>
              <p className={styles['order-history__meta']}>
                <time dateTime={change.changedAt}>{formatDateTime(change.changedAt, i18n.language)}</time>
                {change.by && ` · ${t(`orders.party.${change.by}`)}`}
              </p>
              {change.note && <p className={styles['order-history__note']}>{change.note}</p>}
            </div>
          </li>
        ))}
    </ol>
  )
}
