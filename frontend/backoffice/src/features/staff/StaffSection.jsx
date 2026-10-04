import { Tabs, Typography } from 'antd'
import { useTranslation } from 'react-i18next'
import { Outlet, useLocation, useNavigate } from 'react-router'

const TABS = ['members', 'roles']

/** Staff section: accounts and roles, one tab each (/staff/members, /staff/roles). */
export default function StaffSection() {
  const { t } = useTranslation()
  const navigate = useNavigate()
  const { pathname } = useLocation()
  const active = TABS.find((tab) => pathname.endsWith(`/${tab}`)) ?? TABS[0]

  return (
    <>
      <Typography.Title level={1}>{t('nav.staff')}</Typography.Title>
      <Tabs
        activeKey={active}
        onChange={(key) => navigate(`/staff/${key}`)}
        items={TABS.map((key) => ({ key, label: t(`staff.tabs.${key}`) }))}
      />
      <Outlet />
    </>
  )
}
