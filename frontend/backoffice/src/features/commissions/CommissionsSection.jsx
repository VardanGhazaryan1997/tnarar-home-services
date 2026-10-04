import { Tabs, Typography } from 'antd'
import { useTranslation } from 'react-i18next'
import { Outlet, useLocation, useNavigate } from 'react-router'

const TABS = ['statements', 'rates']

/** Commissions: partners' weekly statements and the rates, one tab each (/commissions/statements, /commissions/rates). */
export default function CommissionsSection() {
  const { t } = useTranslation()
  const navigate = useNavigate()
  const { pathname } = useLocation()
  const active = TABS.find((tab) => pathname.endsWith(`/${tab}`)) ?? TABS[0]

  return (
    <>
      <Typography.Title level={1}>{t('nav.commissions')}</Typography.Title>
      <Tabs
        activeKey={active}
        onChange={(key) => navigate(`/commissions/${key}`)}
        items={TABS.map((key) => ({ key, label: t(`commissions.tabs.${key}`) }))}
      />
      <Outlet />
    </>
  )
}
