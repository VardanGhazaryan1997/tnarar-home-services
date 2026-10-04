import { App, Descriptions, Form, Radio } from 'antd'
import { useTranslation } from 'react-i18next'
import { errorMessage } from '@/api/errors'
import ReasonModal from '@/components/ReasonModal/ReasonModal'
import { formatDate, formatMoney } from '@/i18n/format'
import { useResolvePaymentMutation } from './paymentsApi'

/**
 * Decides a disputed payment: it counts as paid (Confirmed) or not (Rejected), with an optional note both
 * sides see. `payment`: { id, amount, method, paidOn, recordedBy, disputeReason }; null keeps it closed.
 */
export default function ResolvePaymentModal({ payment, onClose }) {
  const { t, i18n } = useTranslation()
  const { message } = App.useApp()
  const [resolve] = useResolvePaymentMutation()

  const confirm = async (note, { counts }) => {
    try {
      await resolve({ id: payment.id, counts, note }).unwrap()
      message.success(t(counts ? 'payments.resolve.doneCounts' : 'payments.resolve.doneRejected'))
    } catch (failure) {
      if (failure?.status === 400) throw failure
      message.error(errorMessage(t, failure))
    }
    onClose()
  }

  return (
    <ReasonModal
      open={Boolean(payment)}
      title={t('payments.resolve.title')}
      label={t('payments.resolve.note')}
      okText={t('payments.resolve.confirm')}
      required={false}
      onConfirm={confirm}
      onCancel={onClose}
    >
      {payment && (
        <Descriptions column={1} size="small" style={{ marginBottom: 16 }}>
          <Descriptions.Item label={t('payments.columns.amount')}>{formatMoney(payment.amount, i18n.language)}</Descriptions.Item>
          <Descriptions.Item label={t('payments.columns.how')}>
            {t(`payments.method.${payment.method}`)} · {formatDate(payment.paidOn, i18n.language)} · {t(`payments.recordedBy.${payment.recordedBy}`)}
          </Descriptions.Item>
          <Descriptions.Item label={t('payments.columns.dispute')}>{payment.disputeReason}</Descriptions.Item>
        </Descriptions>
      )}
      <Form.Item name="counts" label={t('payments.resolve.decision')} rules={[{ required: true, message: t('payments.resolve.decisionRequired') }]}>
        <Radio.Group
          options={[
            { value: true, label: t('payments.resolve.counts') },
            { value: false, label: t('payments.resolve.rejected') },
          ]}
        />
      </Form.Item>
    </ReasonModal>
  )
}
