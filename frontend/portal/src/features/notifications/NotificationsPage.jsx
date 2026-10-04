import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { useNavigate } from 'react-router'
import PageHeader from '@/components/PageHeader/PageHeader'
import QueryState from '@/components/QueryState/QueryState'
import Button from '@/components/ui/Button/Button'
import Card from '@/components/ui/Card/Card'
import EmptyState from '@/components/ui/EmptyState/EmptyState'
import Icon from '@/components/ui/Icon/Icon'
import Pagination from '@/components/ui/Pagination/Pagination'
import Segmented from '@/components/ui/Segmented/Segmented'
import { useLocalizedPath } from '@/i18n/hooks'
import { bem } from '@/shared/bem'
import { formatDateTime } from '@/shared/format'
import { notificationIcon, notificationText } from './notificationText'
import { useGetNotificationsQuery, useMarkAllNotificationsReadMutation, useMarkNotificationReadMutation, useGetUnreadNotificationsQuery } from './notificationsApi'
import styles from './notifications.module.scss'

const b = bem(styles)

/** Everything that happened on the user's requests, offers, orders and profile, newest first. Opening one marks it read. */
export default function NotificationsPage() {
  const { t, i18n } = useTranslation()
  const path = useLocalizedPath()
  const navigate = useNavigate()
  const [page, setPage] = useState(1)
  const [show, setShow] = useState('all')
  const query = useGetNotificationsQuery({ page, unreadOnly: show === 'unread' })
  const unread = useGetUnreadNotificationsQuery().data?.count ?? 0
  const [markRead] = useMarkNotificationReadMutation()
  const [markAll, markingAll] = useMarkAllNotificationsReadMutation()

  const open = (notification) => {
    if (!notification.readAt) markRead(notification.id)
    navigate(path(notification.link))
  }

  return (
    <>
      <title>{t('notifications.documentTitle')}</title>
      <PageHeader
        title={t('notifications.title')}
        subtitle={unread > 0 ? t('notifications.unreadCount', { n: unread }) : t('notifications.allRead')}
        actions={
          unread > 0 && (
            <Button variant="secondary" size="sm" icon={<Icon name="check" />} loading={markingAll.isLoading} onClick={() => markAll()}>
              {t('notifications.markAll')}
            </Button>
          )
        }
      />
      <div className={styles['notifications']}>
        <Segmented
          label={t('notifications.show')}
          options={['all', 'unread'].map((value) => ({ value, label: t(`notifications.filter.${value}`) }))}
          value={show}
          onChange={(value) => {
            setShow(value)
            setPage(1)
          }}
        />
        <QueryState query={query}>
          {(result) =>
            result.items.length === 0 ? (
              <EmptyState icon="bell" title={t('notifications.emptyTitle')} description={t('notifications.emptyText')} />
            ) : (
              <>
                <Card flush>
                  <ul className={styles['notifications__list']}>
                    {result.items.map((notification) => (
                      <li key={notification.id}>
                        <button type="button" className={b('notification', { unread: !notification.readAt })} onClick={() => open(notification)}>
                          <span className={styles['notification__icon']}>
                            <Icon name={notificationIcon(notification.type)} />
                          </span>
                          <span className={styles['notification__body']}>
                            <span className={styles['notification__text']}>{notificationText(t, notification, i18n.language)}</span>
                            <time className={styles['notification__time']} dateTime={notification.createdAt}>
                              {formatDateTime(notification.createdAt, i18n.language)}
                            </time>
                          </span>
                          {!notification.readAt && <span className={styles['notification__dot']} aria-label={t('notifications.new')} />}
                        </button>
                      </li>
                    ))}
                  </ul>
                </Card>
                <Pagination page={page} pageSize={result.pageSize} totalCount={result.totalCount} onChange={setPage} />
              </>
            )
          }
        </QueryState>
      </div>
    </>
  )
}
