import { App, DatePicker, Descriptions, Form, Input, InputNumber, Modal, Select } from 'antd'
import dayjs from 'dayjs'
import { useTranslation } from 'react-i18next'
import { errorMessage, fieldErrors } from '@/api/errors'
import { formatMoney } from '@/i18n/format'
import { useOpenCount } from '@/shared/useOpenCount'
import { useRecordSettlementMutation } from './commissionsApi'
import { SETTLEMENT_METHODS, weekText } from './commissionParts'

const REFERENCE_MAX = 200

/**
 * Records money a partner paid toward a statement. `statement`: { id, outstanding, periodStart, periodEnd } with
 * `partnerName`; null keeps it closed. The amount starts at what is still owed.
 */
export default function SettlementModal({ statement, partnerName, onClose }) {
  const { t, i18n } = useTranslation()
  const [record, { isLoading }] = useRecordSettlementMutation()
  const openCount = useOpenCount(Boolean(statement))

  return (
    <Modal
      open={Boolean(statement)}
      title={t('commissions.settle.title')}
      okText={t('commissions.settle.confirm')}
      okButtonProps={{ htmlType: 'submit', form: 'settlement-form', loading: isLoading }}
      cancelText={t('common.cancel')}
      onCancel={onClose}
      destroyOnHidden
    >
      {statement && (
        <>
          <Descriptions column={1} size="small" style={{ marginBottom: 16 }}>
            <Descriptions.Item label={t('commissions.columns.partner')}>{partnerName}</Descriptions.Item>
            <Descriptions.Item label={t('commissions.columns.week')}>{weekText(statement, i18n.language)}</Descriptions.Item>
            <Descriptions.Item label={t('commissions.columns.outstanding')}>{formatMoney(statement.outstanding, i18n.language)}</Descriptions.Item>
          </Descriptions>
          <SettlementForm key={openCount} statement={statement} record={record} onClose={onClose} />
        </>
      )}
    </Modal>
  )
}

// A fresh form each time the dialog opens, starting from what is owed now.
function SettlementForm({ statement, record, onClose }) {
  const { t, i18n } = useTranslation()
  const { message } = App.useApp()
  const [form] = Form.useForm()

  const submit = async ({ amount, method, paidOn, reference }) => {
    try {
      await record({ statementId: statement.id, amount, method, paidOn: paidOn.format('YYYY-MM-DD'), reference: reference?.trim() || null }).unwrap()
      message.success(t('commissions.settle.done'))
    } catch (failure) {
      if (failure?.status === 400) {
        form.setFields(fieldErrors(t, failure))
        return
      }
      message.error(errorMessage(t, failure))
    }
    onClose()
  }

  return (
    <Form
      id="settlement-form"
      form={form}
      layout="vertical"
      onFinish={submit}
      initialValues={{ amount: statement.outstanding, method: 'BankTransfer', paidOn: dayjs() }}
    >
      <Form.Item
        name="amount"
        label={t('commissions.settle.amount')}
        rules={[{ required: true, type: 'number', min: 1, max: statement.outstanding, message: t('commissions.settle.amountRange', { max: formatMoney(statement.outstanding, i18n.language) }) }]}
      >
        <InputNumber min={1} max={statement.outstanding} precision={0} style={{ width: '100%' }} suffix="֏" />
      </Form.Item>
      <Form.Item name="method" label={t('commissions.settle.method')} rules={[{ required: true }]}>
        <Select options={SETTLEMENT_METHODS.map((value) => ({ value, label: t(`commissions.method.${value}`) }))} />
      </Form.Item>
      <Form.Item name="paidOn" label={t('commissions.settle.paidOn')} rules={[{ required: true, message: t('commissions.settle.paidOnRequired') }]}>
        <DatePicker style={{ width: '100%' }} disabledDate={(day) => day.isAfter(dayjs(), 'day')} allowClear={false} />
      </Form.Item>
      <Form.Item name="reference" label={t('commissions.settle.reference')} rules={[{ max: REFERENCE_MAX, message: t('common.tooLong', { max: REFERENCE_MAX }) }]}>
        <Input maxLength={REFERENCE_MAX} placeholder={t('commissions.settle.referenceHint')} />
      </Form.Item>
    </Form>
  )
}
