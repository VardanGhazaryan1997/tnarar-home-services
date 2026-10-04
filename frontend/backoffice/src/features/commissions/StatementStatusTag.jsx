import { Tag } from 'antd'
import { useTranslation } from 'react-i18next'

/** Paid, overdue or open. */
export default function StatementStatusTag({ statement }) {
  const { t } = useTranslation()
  if (statement.status === 'Paid') return <Tag color="success">{t('commissions.status.Paid')}</Tag>
  if (statement.overdue) return <Tag color="error">{t('commissions.status.Overdue')}</Tag>
  return <Tag color="processing">{t('commissions.status.Open')}</Tag>
}
