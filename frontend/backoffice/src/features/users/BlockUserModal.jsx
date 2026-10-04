import { Form, Input, Modal, Typography } from 'antd'
import { useTranslation } from 'react-i18next'
import { errorMessage, fieldErrors } from '@/api/errors'
import { useBlockUserMutation } from './usersApi'

const REASON_MAX = 500

/** Asks why a user is being blocked (staff see the reason later), then blocks them. */
export default function BlockUserModal({ user, open, onDone, onCancel }) {
  const { t } = useTranslation()
  const [form] = Form.useForm()
  const [block, { isLoading }] = useBlockUserMutation()

  const submit = async ({ reason }) => {
    try {
      await block({ id: user.id, reason: reason.trim() }).unwrap()
      onDone()
    } catch (failure) {
      const fields = fieldErrors(t, failure)
      form.setFields(fields.length > 0 ? fields : [{ name: 'reason', errors: [errorMessage(t, failure)] }])
    }
  }

  return (
    <Modal
      open={open}
      title={t('users.block.title', { name: user.fullName ?? user.phone })}
      okText={t('users.block.confirm')}
      okButtonProps={{ danger: true, htmlType: 'submit', form: 'block-user', loading: isLoading }}
      cancelText={t('catalog.form.cancel')}
      onCancel={onCancel}
      destroyOnHidden
    >
      <Typography.Paragraph>{t('users.block.text')}</Typography.Paragraph>
      <Form id="block-user" form={form} layout="vertical" requiredMark={false} onFinish={submit}>
        <Form.Item
          name="reason"
          label={t('users.block.reason')}
          extra={t('users.block.reasonHelp')}
          rules={[
            { required: true, whitespace: true, message: t('errors.reason.required') },
            { max: REASON_MAX, message: t('errors.reason.too_long') },
          ]}
        >
          <Input.TextArea rows={3} maxLength={REASON_MAX} showCount />
        </Form.Item>
      </Form>
    </Modal>
  )
}
