import { Tabs, Typography } from 'antd'
import { useTranslation } from 'react-i18next'
import { Outlet, useLocation, useNavigate } from 'react-router'

const TABS = ['pages', 'faqs']

/** Website content: static pages and FAQs, one tab each (/content/pages, /content/faqs). */
export default function ContentSection() {
  const { t } = useTranslation()
  const navigate = useNavigate()
  const { pathname } = useLocation()
  const active = TABS.find((tab) => pathname.endsWith(`/${tab}`)) ?? TABS[0]

  return (
    <>
      <Typography.Title level={1}>{t('nav.content')}</Typography.Title>
      <Tabs
        activeKey={active}
        onChange={(key) => navigate(`/content/${key}`)}
        items={TABS.map((key) => ({ key, label: t(`content.tabs.${key}`) }))}
      />
      <Outlet />
    </>
  )
}
