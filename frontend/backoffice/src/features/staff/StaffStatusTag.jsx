import { Tag } from 'antd'
import { useTranslation } from 'react-i18next'
import { STAFF_STATUS_COLORS } from './statuses'

export default function StaffStatusTag({ status }) {
  const { t } = useTranslation()
  return <Tag color={STAFF_STATUS_COLORS[status]}>{t(`staff.status.${status}`)}</Tag>
}
