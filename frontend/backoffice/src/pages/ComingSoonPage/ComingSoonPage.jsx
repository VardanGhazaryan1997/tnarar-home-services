import { Result, Typography } from 'antd'
import { useTranslation } from 'react-i18next'

/** Placeholder for menu sections whose feature task hasn't landed yet. */
export default function ComingSoonPage({ titleKey }) {
  const { t } = useTranslation()
  return (
    <>
      <Typography.Title level={1}>{t(titleKey)}</Typography.Title>
      <Result status="info" title={t('comingSoon.text')} />
    </>
  )
}
