import { useTranslation } from 'react-i18next'
import { NavLink } from 'react-router'
import Icon from '@/components/ui/Icon/Icon'
import { useLocalizedPath } from '@/i18n/hooks'
import { bem } from '@/shared/bem'
import { useGetUnreadNotificationsQuery } from './notificationsApi'
import styles from './notifications.module.scss'

const b = bem(styles)

/** The header bell: opens the notification list, with the unread count as a badge. */
export default function NotificationBell() {
  const { t } = useTranslation()
  const path = useLocalizedPath()
  const unread = useGetUnreadNotificationsQuery().data?.count ?? 0
  const label = unread > 0 ? t('notifications.bellUnread', { n: unread }) : t('notifications.title')

  return (
    <NavLink to={path('/notifications')} className={({ isActive }) => b('bell', { active: isActive })} aria-label={label} title={label}>
      <Icon name="bell" size={22} />
      {unread > 0 && (
        <span className={styles['bell__badge']} aria-hidden="true">
          {unread > 99 ? '99+' : unread}
        </span>
      )}
    </NavLink>
  )
}
