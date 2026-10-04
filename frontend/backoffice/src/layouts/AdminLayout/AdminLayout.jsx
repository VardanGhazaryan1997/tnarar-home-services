import { MenuOutlined } from '@ant-design/icons'
import { Button, Drawer, Flex, Grid, Layout, Menu, Typography } from 'antd'
import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { useSelector } from 'react-redux'
import { Outlet, useLocation, useNavigate } from 'react-router'
import { findNavItem, navItemsFor } from '@/app/navigation'
import BrandLogo from '@/components/BrandLogo/BrandLogo'
import LanguageSwitcher from '@/components/LanguageSwitcher/LanguageSwitcher'
import { selectStaff } from '@/features/auth/authSlice'
import UserMenu from '@/features/auth/UserMenu'

const { Header, Sider, Content } = Layout

function NavMenu({ onNavigate, dark = false }) {
  const { t } = useTranslation()
  const navigate = useNavigate()
  const { pathname } = useLocation()
  const items = navItemsFor(useSelector(selectStaff))
  const selected = findNavItem(pathname)

  const handleClick = ({ key }) => {
    navigate(items.find((item) => item.key === key).path)
    onNavigate?.()
  }

  return (
    <nav aria-label={t('nav.label')}>
      <Menu
        mode="inline"
        theme={dark ? 'dark' : 'light'}
        selectedKeys={selected ? [selected.key] : []}
        items={items.map(({ key, icon }) => ({ key, icon, label: t(`nav.${key}`) }))}
        onClick={handleClick}
      />
    </nav>
  )
}

/**
 * Back Office shell: side menu on desktop (lg and up), a drawer opened from
 * the header on tablets and phones.
 */
export default function AdminLayout() {
  const { t } = useTranslation()
  const screens = Grid.useBreakpoint()
  const isDesktop = Boolean(screens.lg)
  const [drawerOpen, setDrawerOpen] = useState(false)

  return (
    <Layout style={{ minHeight: '100vh' }}>
      {isDesktop && (
        <Sider width={240}>
          <Flex align="center" style={{ height: 64, padding: '0 20px' }}>
            <BrandLogo size={30} tone="dark" />
          </Flex>
          <NavMenu dark />
        </Sider>
      )}
      <Layout>
        <Header role="banner">
          <Flex align="center" gap="middle" style={{ height: '100%' }}>
            {!isDesktop && (
              <Button type="text" icon={<MenuOutlined />} aria-label={t('nav.open')} onClick={() => setDrawerOpen(true)} />
            )}
            {!isDesktop && <BrandLogo size={28} compact />}
            <Typography.Text strong>{t('app.name')}</Typography.Text>
            <Flex flex="auto" justify="flex-end" align="center" gap="small">
              <LanguageSwitcher />
              <UserMenu />
            </Flex>
          </Flex>
        </Header>
        <Content style={{ padding: isDesktop ? 24 : 16 }}>
          <Outlet />
        </Content>
      </Layout>
      {!isDesktop && (
        <Drawer
          placement="left"
          size={280}
          title={<BrandLogo size={28} />}
          open={drawerOpen}
          onClose={() => setDrawerOpen(false)}
          destroyOnHidden
          styles={{ body: { padding: 0 } }}
        >
          <NavMenu onNavigate={() => setDrawerOpen(false)} />
        </Drawer>
      )}
    </Layout>
  )
}
