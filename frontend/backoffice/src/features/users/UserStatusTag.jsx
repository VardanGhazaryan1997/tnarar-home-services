import { Tag } from 'antd'
import { useTranslation } from 'react-i18next'

export default function UserStatusTag({ status }) {
  const { t } = useTranslation()
  return <Tag color={status === 'Blocked' ? 'error' : 'success'}>{t(`users.status.${status}`)}</Tag>
}
