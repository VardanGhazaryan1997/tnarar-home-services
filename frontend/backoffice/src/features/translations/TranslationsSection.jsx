import { Tabs, Typography } from 'antd'
import { useTranslation } from 'react-i18next'
import { Outlet, useLocation, useNavigate } from 'react-router'

const TABS = ['texts', 'languages']

/** Interface texts and languages, one tab each (/translations/texts, /translations/languages). */
export default function TranslationsSection() {
  const { t } = useTranslation()
  const navigate = useNavigate()
  const { pathname } = useLocation()
  const active = TABS.find((tab) => pathname.endsWith(`/${tab}`)) ?? TABS[0]

  return (
    <>
      <Typography.Title level={1}>{t('nav.translations')}</Typography.Title>
      <Tabs
        activeKey={active}
        onChange={(key) => navigate(`/translations/${key}`)}
        items={TABS.map((key) => ({ key, label: t(`translations.tabs.${key}`) }))}
      />
      <Outlet />
    </>
  )
}
