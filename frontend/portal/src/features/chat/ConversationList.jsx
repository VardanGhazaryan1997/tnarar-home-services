import { useTranslation } from 'react-i18next'
import { Link } from 'react-router'
import QueryState from '@/components/QueryState/QueryState'
import EmptyState from '@/components/ui/EmptyState/EmptyState'
import { placeText } from '@/features/requests/place'
import { useLocalizedPath } from '@/i18n/hooks'
import { bem } from '@/shared/bem'
import { formatDate } from '@/shared/format'
import { useGetConversationsQuery } from './chatApi'
import { initials } from './initials'
import styles from './chat.module.scss'

const b = bem(styles)

/** The user's conversations, newest first, with the last message and unread counts. */
export default function ConversationList({ activeId }) {
  const { t, i18n } = useTranslation()
  const path = useLocalizedPath()
  const query = useGetConversationsQuery(undefined, { refetchOnMountOrArgChange: true })

  return (
    <QueryState query={query}>
      {(data) =>
        data.items.length === 0 ? (
          <EmptyState icon="messages" title={t('chat.emptyTitle')} description={t('chat.emptyText')} />
        ) : (
          <ul className={styles['conversation-list']}>
            {data.items.map((conversation) => {
              const last = conversation.lastMessage
              const mine = last?.senderRole === conversation.myRole
              const preview = last ? `${mine ? `${t('chat.you')}: ` : ''}${last.excerpt ?? t('chat.files', { n: last.attachmentCount })}` : t('chat.noMessages')
              const name = conversation.otherParty.name ?? t(`chat.role.${conversation.myRole === 'Customer' ? 'Partner' : 'Customer'}`)
              return (
                <li key={conversation.id}>
                  <Link
                    to={path(`/messages/${conversation.id}`)}
                    className={b('conversation-list__item', { active: conversation.id === activeId, unread: conversation.unreadCount > 0 })}
                    aria-current={conversation.id === activeId ? 'page' : undefined}
                  >
                    <span className={styles['conversation-list__avatar']} aria-hidden="true">
                      {initials(name)}
                    </span>
                    <span className={styles['conversation-list__text']}>
                      <span className={styles['conversation-list__top']}>
                        <span className={styles['conversation-list__name']}>{name}</span>
                        {last && <span className={styles['conversation-list__time']}>{formatDate(last.sentAt, i18n.language, { day: 'numeric', month: 'short' })}</span>}
                      </span>
                      <span className={styles['conversation-list__place']}>{placeText(conversation.place, t)}</span>
                      <span className={styles['conversation-list__preview']}>{preview}</span>
                    </span>
                    {conversation.unreadCount > 0 && (
                      <span className={styles['conversation-list__badge']} aria-label={t('chat.unread', { n: conversation.unreadCount })}>
                        {conversation.unreadCount}
                      </span>
                    )}
                  </Link>
                </li>
              )
            })}
          </ul>
        )
      }
    </QueryState>
  )
}
