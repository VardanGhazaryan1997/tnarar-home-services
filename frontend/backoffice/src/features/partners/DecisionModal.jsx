import { Form, Input, Modal, Typography } from 'antd'
import { useTranslation } from 'react-i18next'
import { fieldErrors } from '@/api/errors'
import { DECISIONS } from './partnersApi'

const COMMENT_MAX = 2000

/**
 * Confirms a decision on a partner profile. Sending back, rejecting and suspending need a comment,
 * which the partner sees. `onConfirm(comment)` returns a promise; when it rejects with validation errors they show on the form.
 */
export default function DecisionModal({ decision, partnerName, onConfirm, onCancel }) {
  const { t } = useTranslation()
  const [form] = Form.useForm()
  const needsComment = decision ? DECISIONS[decision].needsComment : false

  const submit = async ({ comment }) => {
    try {
      await onConfirm(comment?.trim())
      form.resetFields()
    } catch (failure) {
      form.setFields(fieldErrors(t, failure))
    }
  }

  return (
    <Modal
      open={Boolean(decision)}
      title={decision ? t(`partners.decisions.${decision}.title`, { name: partnerName }) : ''}
      okText={decision ? t(`partners.decisions.${decision}.confirm`) : ''}
      okButtonProps={{ danger: decision === 'reject' || decision === 'suspend', htmlType: 'submit', form: 'partner-decision' }}
      cancelText={t('catalog.form.cancel')}
      onCancel={() => {
        form.resetFields()
        onCancel()
      }}
      destroyOnHidden
    >
      {decision && <Typography.Paragraph>{t(`partners.decisions.${decision}.text`)}</Typography.Paragraph>}
      <Form id="partner-decision" form={form} layout="vertical" onFinish={submit} requiredMark={needsComment ? true : false}>
        {needsComment && (
          <Form.Item
            name="comment"
            label={t('partners.decisions.comment')}
            extra={t('partners.decisions.commentHelp')}
            rules={[
              { required: true, whitespace: true, message: t('errors.comment.required') },
              { max: COMMENT_MAX, message: t('errors.comment.too_long') },
            ]}
          >
            <Input.TextArea rows={4} maxLength={COMMENT_MAX} showCount />
          </Form.Item>
        )}
      </Form>
    </Modal>
  )
}
