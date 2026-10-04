import { Tabs, Typography } from 'antd'
import { useTranslation } from 'react-i18next'
import { Outlet, useLocation, useNavigate } from 'react-router'

const TABS = ['categories', 'cities']

/** Catalog section: categories and cities, one tab each (/catalog/categories, /catalog/cities). */
export default function CatalogPage() {
  const { t } = useTranslation()
  const navigate = useNavigate()
  const { pathname } = useLocation()
  const active = TABS.find((tab) => pathname.endsWith(`/${tab}`)) ?? TABS[0]

  return (
    <>
      <Typography.Title level={1}>{t('nav.catalog')}</Typography.Title>
      <Tabs
        activeKey={active}
        onChange={(key) => navigate(`/catalog/${key}`)}
        items={TABS.map((key) => ({ key, label: t(`catalog.tabs.${key}`) }))}
      />
      <Outlet />
    </>
  )
}
