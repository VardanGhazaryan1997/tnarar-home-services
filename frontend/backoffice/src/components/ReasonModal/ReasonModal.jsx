import { Form, Input, Modal, Typography } from 'antd'
import { useTranslation } from 'react-i18next'
import { fieldErrors } from '@/api/errors'

/**
 * Asks for a reason (or a note) before an action. `required`: it can't be empty. `onConfirm(text)` returns a
 * promise; when it rejects with validation errors from the API they show on the field.
 */
export default function ReasonModal({ open, title, text, label, okText, danger = false, required = true, max = 1000, onConfirm, onCancel, children }) {
  const { t } = useTranslation()
  const [form] = Form.useForm()

  const submit = async (values) => {
    try {
      await onConfirm(values.reason?.trim() || null, values)
      form.resetFields()
    } catch (failure) {
      const errors = fieldErrors(t, failure).map((error) => ({ ...error, name: error.name === 'note' ? 'reason' : error.name }))
      // Other failures are the caller's to report (it closes the dialog or shows a message).
      form.setFields(errors)
    }
  }

  return (
    <Modal
      open={open}
      title={title}
      okText={okText}
      okButtonProps={{ danger, htmlType: 'submit', form: 'reason-form' }}
      cancelText={t('common.cancel')}
      onCancel={() => {
        form.resetFields()
        onCancel()
      }}
      destroyOnHidden
    >
      {text && <Typography.Paragraph>{text}</Typography.Paragraph>}
      <Form id="reason-form" form={form} layout="vertical" onFinish={submit} requiredMark={required}>
        {children}
        <Form.Item
          name="reason"
          label={label}
          rules={[
            ...(required ? [{ required: true, whitespace: true, message: t('common.reasonRequired') }] : []),
            { max, message: t('common.tooLong', { max }) },
          ]}
        >
          <Input.TextArea rows={4} maxLength={max} showCount />
        </Form.Item>
      </Form>
    </Modal>
  )
}
