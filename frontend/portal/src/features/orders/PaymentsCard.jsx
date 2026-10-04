import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { errorMessage } from '@/api/errors'
import Alert from '@/components/ui/Alert/Alert'
import Button from '@/components/ui/Button/Button'
import Card from '@/components/ui/Card/Card'
import Icon from '@/components/ui/Icon/Icon'
import Tag from '@/components/ui/Tag/Tag'
import { formatDate, formatMoney } from '@/shared/format'
import { PaymentPlan } from './OrderParts'
import { useConfirmPaymentMutation, useDisputePaymentMutation, useWithdrawPaymentMutation } from './ordersApi'
import PaymentModal from './PaymentModal'
import ReasonModal from './ReasonModal'
import styles from './orders.module.scss'

const TONES = { Pending: 'warning', Confirmed: 'success', Disputed: 'danger', Withdrawn: 'neutral', Rejected: 'neutral' }
const COUNTING = ['Pending', 'Confirmed', 'Disputed']

/** One payment record: what, how and when, who recorded it, where it stands, and the viewer's answer. */
function PaymentRow({ order, payment, onDispute }) {
  const { t, i18n } = useTranslation()
  const [confirm, confirming] = useConfirmPaymentMutation()
  const [withdraw, withdrawing] = useWithdrawPaymentMutation()
  const party = order.myRole === 'Customer' || order.myRole === 'Partner'
  const answer = party && payment.status === 'Pending' && !payment.mine
  const canWithdraw = party && payment.status === 'Pending' && payment.mine
  const error = confirming.error ?? withdrawing.error
  const stage = order.stages.find((item) => item.id === payment.stageId)
  const who = payment.mine ? t('orders.pay.byYou') : t('orders.pay.by', { who: t(`orders.party.${payment.recordedBy}`) })

  return (
    <li className={styles['payment-record']}>
      <div className={styles['payment-record__head']}>
        <span className={styles['payment-record__amount']}>{formatMoney(payment.amount, i18n.language)}</span>
        <Tag tone={TONES[payment.status]}>{t(`orders.pay.status.${payment.status}`)}</Tag>
      </div>
      <p className={styles['payment-record__meta']}>
        {[
          t(`orders.pay.methods.${payment.method}`),
          formatDate(payment.paidOn, i18n.language),
          stage && (stage.title ?? t(`offers.purpose.${stage.purpose}`)),
          who,
        ]
          .filter(Boolean)
          .join(' · ')}
      </p>
      {payment.note && <p className={styles['payment-record__note']}>{payment.note}</p>}
      {payment.disputeReason && (
        <p className={styles['payment-record__note']}>
          <strong>{t('orders.pay.disputeReason')}</strong> {payment.disputeReason}
        </p>
      )}
      {payment.status === 'Disputed' && <p className={styles['payment-record__hint']}>{t('orders.pay.withTeam')}</p>}
      {payment.resolutionNote && (
        <p className={styles['payment-record__note']}>
          <strong>{t('orders.pay.teamNote')}</strong> {payment.resolutionNote}
        </p>
      )}
      {(answer || canWithdraw) && (
        <div className={styles['payment-record__actions']}>
          {answer && (
            <>
              <Button size="sm" variant="primary" icon={<Icon name="check" />} loading={confirming.isLoading} onClick={() => confirm({ paymentId: payment.id })}>
                {t(`orders.pay.confirm.${order.myRole}`)}
              </Button>
              <Button size="sm" variant="secondary" onClick={() => onDispute(payment)}>
                {t('orders.pay.dispute')}
              </Button>
            </>
          )}
          {canWithdraw && (
            <Button size="sm" variant="ghost" loading={withdrawing.isLoading} onClick={() => withdraw({ paymentId: payment.id })}>
              {t('orders.pay.withdraw')}
            </Button>
          )}
        </div>
      )}
      {error && <Alert tone="danger" title={errorMessage(t, error)} />}
    </li>
  )
}

/**
 * Payments made directly between the two sides (TnaShen doesn't hold the money): how much is confirmed, the
 * payment plan, and each record. Either side records a payment; the other confirms or disputes it.
 */
export default function PaymentsCard({ order }) {
  const { t, i18n } = useTranslation()
  const [recordOpen, setRecordOpen] = useState(false)
  const [disputing, setDisputing] = useState(null)
  const [dispute] = useDisputePaymentMutation()
  const counted = order.payments.filter((payment) => COUNTING.includes(payment.status)).reduce((sum, payment) => sum + payment.amount, 0)
  const remaining = Math.max(order.price - counted, 0)
  const canRecord = order.actions.includes('recordPayment')
  const share = order.price > 0 ? Math.min(order.paidAmount / order.price, 1) : 0

  if (order.price === 0 && order.payments.length === 0) return null

  return (
    <Card
      title={t('orders.pay.heading')}
      actions={
        canRecord && (
          <Button size="sm" variant="secondary" icon={<Icon name="money" />} onClick={() => setRecordOpen(true)}>
            {t(`orders.pay.record.${order.myRole}`)}
          </Button>
        )
      }
    >
      <div className={styles['payments']}>
        <div>
          <p className={styles['payments__summary']}>
            {t('orders.pay.paidOf', { paid: formatMoney(order.paidAmount, i18n.language), total: formatMoney(order.price, i18n.language) })}
          </p>
          <div
            className={styles['payments__bar']}
            role="progressbar"
            aria-label={t('orders.pay.progress')}
            aria-valuemin={0}
            aria-valuemax={100}
            aria-valuenow={Math.round(share * 100)}
          >
            <span className={styles['payments__fill']} style={{ width: `${share * 100}%` }} />
          </div>
        </div>
        {order.stages.length > 0 && (
          <div>
            <h3 className={styles['payments__subheading']}>{t('orders.payments')}</h3>
            <PaymentPlan stages={order.stages} total={order.price} />
          </div>
        )}
        <div>
          <h3 className={styles['payments__subheading']}>{t('orders.pay.records')}</h3>
          {order.payments.length ? (
            <ul className={styles['payments__list']}>
              {order.payments.map((payment) => (
                <PaymentRow key={payment.id} order={order} payment={payment} onDispute={setDisputing} />
              ))}
            </ul>
          ) : (
            <p className={styles['payments__empty']}>{t(`orders.pay.none.${order.myRole === 'Partner' ? 'Partner' : 'Customer'}`)}</p>
          )}
        </div>
      </div>
      {canRecord && <PaymentModal order={order} remaining={remaining} open={recordOpen} onClose={() => setRecordOpen(false)} />}
      <ReasonModal
        open={Boolean(disputing)}
        title={t('orders.pay.disputeTitle')}
        text={t('orders.pay.disputeText')}
        label={t('orders.pay.disputeLabel')}
        confirmLabel={t('orders.pay.dispute')}
        onSubmit={(reason) => dispute({ paymentId: disputing.id, reason })}
        onClose={() => setDisputing(null)}
      />
    </Card>
  )
}
