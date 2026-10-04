import { Form, Modal } from 'antd'
import { useTranslation } from 'react-i18next'
import { fieldErrors } from '@/api/errors'
import { useOpenCount } from '@/shared/useOpenCount'
import { percentRules } from './commissionParts'
import PercentInput from './PercentInput'

/**
 * Sets a category's own rate. `category`: { categoryId, name, percent, effectivePercent }; null keeps it closed.
 * `onSave(percent)` returns a promise; validation errors from the API show on the field.
 */
export default function RateModal({ category, onSave, onClose }) {
  const { t } = useTranslation()
  const openCount = useOpenCount(Boolean(category))

  return (
    <Modal
      open={Boolean(category)}
      title={category && t('commissions.rates.editTitle', { name: category.name })}
      okText={t('commissions.rates.save')}
      okButtonProps={{ htmlType: 'submit', form: 'rate-form' }}
      cancelText={t('common.cancel')}
      onCancel={onClose}
      destroyOnHidden
    >
      {category && <RateForm key={openCount} category={category} onSave={onSave} />}
    </Modal>
  )
}

// A fresh form each time the dialog opens, starting from the category's rate.
function RateForm({ category, onSave }) {
  const { t } = useTranslation()
  const [form] = Form.useForm()

  const submit = async ({ percent }) => {
    try {
      await onSave(percent)
    } catch (failure) {
      form.setFields(fieldErrors(t, failure))
    }
  }

  return (
    <Form id="rate-form" form={form} layout="vertical" onFinish={submit} initialValues={{ percent: category.percent ?? category.effectivePercent }}>
      <Form.Item name="percent" label={t('commissions.rates.own')} extra={t('commissions.rates.ownHelp')} rules={percentRules(t)}>
        <PercentInput aria-label={t('commissions.rates.own')} />
      </Form.Item>
    </Form>
  )
}
