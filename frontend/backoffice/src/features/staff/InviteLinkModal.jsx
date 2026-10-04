import { Alert, Button, Modal, Typography } from 'antd'
import { useTranslation } from 'react-i18next'
import { formatDate } from '@/i18n/format'
import { inviteLink } from './inviteLink'

/**
 * Shows an invitation link once, right after it is created. Emails aren't sent yet, so the
 * manager copies the link and sends it to the person.
 */
export default function InviteLinkModal({ invite, onClose }) {
  const { t, i18n } = useTranslation()

  return (
    <Modal
      open={Boolean(invite)}
      title={t('staff.inviteLink.title')}
      onCancel={onClose}
      footer={
        <Button type="primary" onClick={onClose}>
          {t('staff.inviteLink.done')}
        </Button>
      }
      destroyOnHidden
    >
      {invite && (
        <>
          <Typography.Paragraph>
            {t('staff.inviteLink.text', { name: invite.member.fullName, email: invite.member.email })}
          </Typography.Paragraph>
          <Typography.Paragraph copyable={{ text: inviteLink(invite.inviteToken) }} code style={{ wordBreak: 'break-all' }}>
            {inviteLink(invite.inviteToken)}
          </Typography.Paragraph>
          <Alert
            type="warning"
            showIcon
            title={t('staff.inviteLink.once', { date: formatDate(invite.expiresAt, i18n.language, { withTime: true }) })}
          />
        </>
      )}
    </Modal>
  )
}
