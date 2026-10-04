import { Tag } from 'antd'
import { useTranslation } from 'react-i18next'

export default function PublishedTag({ published }) {
  const { t } = useTranslation()
  return published ? <Tag color="success">{t('content.published')}</Tag> : <Tag>{t('content.draft')}</Tag>
}
