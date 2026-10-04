import { Tag } from 'antd'
import { useTranslation } from 'react-i18next'
import { STATUS_COLORS } from './statuses'

/** A partner profile's status as a colored tag. */
export default function PartnerStatusTag({ status }) {
  const { t } = useTranslation()
  return <Tag color={STATUS_COLORS[status]}>{t(`partners.status.${status}`)}</Tag>
}
