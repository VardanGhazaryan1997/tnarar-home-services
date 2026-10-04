import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { errorMessage } from '@/api/errors'
import Alert from '@/components/ui/Alert/Alert'
import Button from '@/components/ui/Button/Button'
import Card from '@/components/ui/Card/Card'
import Icon from '@/components/ui/Icon/Icon'
import { formatDate, formatDateTime } from '@/shared/format'
import OrderProgress from './OrderProgress'
import { useConfirmCompletionMutation, useRejectCompletionMutation, useRequestCompletionMutation, useStartOrderMutation } from './ordersApi'
import ReasonModal from './ReasonModal'
import styles from './orders.module.scss'

/**
 * The order's progress, what happens next for the viewer and the main buttons for their next step
 * (start, mark as done, confirm, "not finished yet"), shown only when the order's `actions` allow them.
 */
export default function OrderStatusPanel({ order }) {
  const { t, i18n } = useTranslation()
  const [start, starting] = useStartOrderMutation()
  const [requestCompletion, requesting] = useRequestCompletionMutation()
  const [confirm, confirming] = useConfirmCompletionMutation()
  const [rejectCompletion] = useRejectCompletionMutation()
  const [rejectOpen, setRejectOpen] = useState(false)
  const [error, setError] = useState(null)
  const can = (name) => order.actions.includes(name)
  const date = order.autoCompleteAt ? formatDate(order.autoCompleteAt, i18n.language) : ''

  const run = async (mutation) => {
    setError(null)
    const result = await mutation(order.id)
    if (result.error) setError(errorMessage(t, result.error))
  }

  if (order.status === 'Cancelled') {
    return (
      <Card className={styles['order-status']}>
        <div className={styles['order-status__cancelled']}>
          <Icon name="close" size={22} />
          <div>
            <p className={styles['order-status__title']}>
              {t('orders.cancelledBy', {
                who: t(`orders.party.${order.cancelledBy}`),
                date: formatDateTime(order.cancelledAt, i18n.language),
              })}
            </p>
            {order.cancelReason && <p className={styles['order-status__quote']}>{order.cancelReason}</p>}
            {order.needsAttentionSince && <p className={styles['order-status__text']}>{t('orders.teamWillHelp')}</p>}
          </div>
        </div>
      </Card>
    )
  }

  return (
    <Card className={styles['order-status']}>
      <OrderProgress status={order.status} />
      <p className={styles['order-status__text']}>{t(`orders.next.${order.myRole}.${order.status}`, { date })}</p>
      {error && <Alert tone="danger" title={error} />}
      {(can('start') || can('requestCompletion') || can('confirmCompletion') || can('rejectCompletion')) && (
        <div className={styles['order-status__actions']}>
          {can('start') && (
            <Button variant="secondary" icon={<Icon name="tools" />} loading={starting.isLoading} onClick={() => run(start)}>
              {t('orders.actions.start')}
            </Button>
          )}
          {can('requestCompletion') && (
            <Button variant="accent" icon={<Icon name="check" />} loading={requesting.isLoading} onClick={() => run(requestCompletion)}>
              {order.kind === 'Visit' ? t('orders.actions.visitDone') : t('orders.actions.requestCompletion')}
            </Button>
          )}
          {can('rejectCompletion') && (
            <Button variant="secondary" onClick={() => setRejectOpen(true)}>
              {t('orders.actions.rejectCompletion')}
            </Button>
          )}
          {can('confirmCompletion') && (
            <Button variant="accent" icon={<Icon name="check" />} loading={confirming.isLoading} onClick={() => run(confirm)}>
              {t('orders.actions.confirmCompletion')}
            </Button>
          )}
        </div>
      )}
      <ReasonModal
        open={rejectOpen}
        title={t('orders.rejectCompletion.title')}
        text={t('orders.rejectCompletion.text')}
        label={t('orders.rejectCompletion.label')}
        confirmLabel={t('orders.rejectCompletion.confirm')}
        tone="primary"
        onSubmit={(reason) => rejectCompletion({ id: order.id, reason })}
        onClose={() => setRejectOpen(false)}
      />
    </Card>
  )
}
