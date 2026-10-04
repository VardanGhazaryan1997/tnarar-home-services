import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { errorMessage } from '@/api/errors'
import Alert from '@/components/ui/Alert/Alert'
import Button from '@/components/ui/Button/Button'
import Card from '@/components/ui/Card/Card'
import Icon from '@/components/ui/Icon/Icon'
import Tag from '@/components/ui/Tag/Tag'
import { formatDate, formatDateTime, formatMoney } from '@/shared/format'
import { useAcceptChangeMutation, useRejectChangeMutation, useWithdrawChangeMutation } from './ordersApi'
import ReasonModal from './ReasonModal'
import styles from './orders.module.scss'

const STATUS_TONES = {
  Pending: 'warning',
  Accepted: 'success',
  Rejected: 'danger',
  Withdrawn: 'neutral',
  Closed: 'neutral',
}

/** What a change proposes, in words: the extra work and its price, or the new dates. */
export function ChangeDetails({ change }) {
  const { t, i18n } = useTranslation()
  return (
    <div className={styles['order-change__details']}>
      {change.kind === 'ExtraWork' ? (
        <p className={styles['order-change__what']}>
          <span>{change.title}</span>
          <strong>+{formatMoney(change.amount, i18n.language)}</strong>
        </p>
      ) : (
        <ul className={styles['order-change__facts']}>
          {change.newVisitAt && (
            <li>
              {t('orders.change.newVisitAt', {
                date: formatDateTime(change.newVisitAt, i18n.language),
              })}
            </li>
          )}
          {change.newStartDate && (
            <li>
              {t('orders.change.newStartDate', {
                date: formatDate(change.newStartDate, i18n.language),
              })}
            </li>
          )}
          {change.newDurationDays && <li>{t('orders.change.newDuration', { n: change.newDurationDays })}</li>}
        </ul>
      )}
      {change.description && <p className={styles['order-change__description']}>{change.description}</p>}
    </div>
  )
}

/**
 * The change waiting for an answer. The other side accepts or turns it down (optionally saying why);
 * whoever proposed it can withdraw it.
 */
export function PendingChange({ order, change }) {
  const { t, i18n } = useTranslation()
  const [accept, accepting] = useAcceptChangeMutation()
  const [reject] = useRejectChangeMutation()
  const [withdraw, withdrawing] = useWithdrawChangeMutation()
  const [declining, setDeclining] = useState(false)
  const [error, setError] = useState(null)
  const canAnswer = order.actions.includes('answerChange') && !change.mine
  const canWithdraw = order.actions.includes('withdrawChange') && change.mine

  const run = async (mutation) => {
    setError(null)
    const result = await mutation({ id: order.id, changeId: change.id })
    if (result.error) setError(errorMessage(t, result.error))
  }

  return (
    <Card
      className={styles['order-change']}
      title={t(`orders.change.kind.${change.kind}`)}
      actions={<Tag tone="warning">{change.mine ? t('orders.change.waitingForThem') : t('orders.change.waitingForYou')}</Tag>}
    >
      <p className={styles['order-change__who']}>
        {t('orders.change.proposedBy', {
          who: t(`orders.party.${change.proposedBy}`),
          date: formatDateTime(change.proposedAt, i18n.language),
        })}
      </p>
      <ChangeDetails change={change} />
      {change.kind === 'ExtraWork' && canAnswer && (
        <p className={styles['order-change__hint']}>
          {t('orders.change.extraWorkHint', {
            total: formatMoney(order.price + change.amount, i18n.language),
          })}
        </p>
      )}
      {error && <Alert tone="danger" title={error} />}
      {(canAnswer || canWithdraw) && (
        <div className={styles['order-change__actions']}>
          {canAnswer && (
            <>
              <Button variant="secondary" onClick={() => setDeclining(true)}>
                {t('orders.change.decline')}
              </Button>
              <Button variant="accent" icon={<Icon name="check" />} loading={accepting.isLoading} onClick={() => run(accept)}>
                {t('orders.change.accept')}
              </Button>
            </>
          )}
          {canWithdraw && (
            <Button variant="ghost" icon={<Icon name="close" />} loading={withdrawing.isLoading} onClick={() => run(withdraw)}>
              {t('orders.change.withdraw')}
            </Button>
          )}
        </div>
      )}
      <ReasonModal
        open={declining}
        title={t('orders.change.declineTitle')}
        label={t('orders.change.declineNote')}
        confirmLabel={t('orders.change.decline')}
        required={false}
        onSubmit={(note) => reject({ id: order.id, changeId: change.id, note })}
        onClose={() => setDeclining(false)}
      />
    </Card>
  )
}

/** Changes that were answered, withdrawn or closed, newest first. */
export function PastChanges({ changes }) {
  const { t, i18n } = useTranslation()
  return (
    <ul className={styles['order-changes']}>
      {changes.map((change) => (
        <li key={change.id} className={styles['order-changes__item']}>
          <div className={styles['order-changes__head']}>
            <span className={styles['order-changes__kind']}>{t(`orders.change.kind.${change.kind}`)}</span>
            <Tag tone={STATUS_TONES[change.status]}>{t(`orders.change.status.${change.status}`)}</Tag>
          </div>
          <ChangeDetails change={change} />
          <p className={styles['order-changes__meta']}>
            {t('orders.change.proposedBy', {
              who: t(`orders.party.${change.proposedBy}`),
              date: formatDate(change.proposedAt, i18n.language),
            })}
            {change.responseNote && ` · ${t('orders.change.answer', { note: change.responseNote })}`}
          </p>
        </li>
      ))}
    </ul>
  )
}
