import { useEffect } from 'react'
import { useTranslation } from 'react-i18next'
import { useDispatch, useSelector } from 'react-redux'
import { NavLink, Outlet } from 'react-router'
import BrandLogo from '@/components/BrandLogo/BrandLogo'
import LanguageSwitcher from '@/components/LanguageSwitcher/LanguageSwitcher'
import Button from '@/components/ui/Button/Button'
import Icon from '@/components/ui/Icon/Icon'
import { bootstrapSession } from '@/features/auth/authApi'
import { useGetUnreadQuery } from '@/features/chat/chatApi'
import { useChatConnection } from '@/features/chat/useChatConnection'
import NotificationBell from '@/features/notifications/NotificationBell'
import { selectAuthStatus, selectUser } from '@/features/auth/authSlice'
import { useLocalizedPath } from '@/i18n/hooks'
import { bem } from '@/shared/bem'
import { desktopItems, mobileItems } from './navigation'
import SiteFooter from './SiteFooter'
import styles from './AppLayout.module.scss'

const b = bem(styles)

/** The page frame: header (desktop navigation), bottom tab bar (phones) and footer. Restores the session on start. */
export default function AppLayout() {
  const { t } = useTranslation()
  const dispatch = useDispatch()
  const status = useSelector(selectAuthStatus)
  const user = useSelector(selectUser)
  const path = useLocalizedPath()
  const signedIn = status === 'authenticated'
  const unread = useGetUnreadQuery(undefined, { skip: !signedIn }).data?.messages ?? 0
  useChatConnection(signedIn)

  const badge = (item) =>
    item.badge && unread > 0 ? (
      <span className={styles['app-layout__badge']} aria-label={t('chat.unread', { n: unread })}>
        {unread > 99 ? '99+' : unread}
      </span>
    ) : null

  useEffect(() => {
    dispatch(bootstrapSession())
  }, [dispatch])

  const linkClass = (block) => ({ isActive }) => b(block, { active: isActive })

  return (
    <div className={styles['app-layout']}>
      <a className={styles['app-layout__skip']} href="#main">
        {t('layout.skipToContent')}
      </a>
      <header className={styles['app-layout__header']}>
        <div className={styles['app-layout__bar']}>
          <NavLink to={path('/')} className={styles['app-layout__logo']} aria-label={t('layout.home')}>
            <BrandLogo size={30} />
          </NavLink>
          <nav className={styles['app-layout__nav']} aria-label={t('layout.mainNavigation')}>
            {desktopItems(user).map((item) => (
              <NavLink key={item.key} to={path(item.path)} end={item.end} className={linkClass('app-layout__nav-link')}>
                {t(`nav.${item.key}`)}
                {badge(item)}
              </NavLink>
            ))}
          </nav>
          <div className={styles['app-layout__tools']}>
            <LanguageSwitcher />
            {status === 'authenticated' && <NotificationBell />}
            {status === 'authenticated' && (
              <NavLink to={path('/account')} className={linkClass('app-layout__account')}>
                <Icon name="account" />
                <span className={styles['app-layout__account-name']}>{user.fullName ?? t('nav.account')}</span>
              </NavLink>
            )}
            {status === 'anonymous' && (
              <Button to={path('/sign-in')} size="sm">
                {t('nav.signIn')}
              </Button>
            )}
          </div>
        </div>
      </header>

      <main id="main" className={b('app-layout__main', { 'with-tabs': true })}>
        <Outlet />
      </main>

      <SiteFooter />

      <nav className={styles['app-layout__tabs']} aria-label={t('layout.tabNavigation')}>
        {mobileItems(user).map((item) => (
          <NavLink key={item.key} to={path(item.path)} end={item.end} className={linkClass('app-layout__tab')}>
            <span className={styles['app-layout__tab-icon']}>
              <Icon name={item.icon} size={22} />
              {badge(item)}
            </span>
            <span className={styles['app-layout__tab-label']}>{t(`nav.tab.${item.key}`)}</span>
          </NavLink>
        ))}
        {status === 'anonymous' && (
          <NavLink to={path('/sign-in')} className={linkClass('app-layout__tab')}>
            <Icon name="account" size={22} />
            <span className={styles['app-layout__tab-label']}>{t('nav.signIn')}</span>
          </NavLink>
        )}
      </nav>
    </div>
  )
}
