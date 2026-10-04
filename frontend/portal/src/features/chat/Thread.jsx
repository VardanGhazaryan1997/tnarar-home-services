import { useEffect, useRef, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Link } from 'react-router'
import { errorMessage } from '@/api/errors'
import QueryState from '@/components/QueryState/QueryState'
import Alert from '@/components/ui/Alert/Alert'
import Button from '@/components/ui/Button/Button'
import EmptyState from '@/components/ui/EmptyState/EmptyState'
import Icon from '@/components/ui/Icon/Icon'
import { placeText } from '@/features/requests/place'
import { useLocalizedPath } from '@/i18n/hooks'
import { bem } from '@/shared/bem'
import { formatDate } from '@/shared/format'
import { useGetConversationQuery, useGetMessagesQuery, useGetOlderMessagesMutation, useMarkReadMutation } from './chatApi'
import Composer from './Composer'
import { initials } from './initials'
import styles from './chat.module.scss'

const b = bem(styles)
const dayOf = (iso) => iso.slice(0, 10)

/** One message bubble with its files and time. */
function Bubble({ message, mine, seen }) {
  const { t, i18n } = useTranslation()
  return (
    <li className={b('thread__message', { mine })}>
      <div className={b('thread__bubble', { mine })}>
        {message.body && <p className={styles['thread__body']}>{message.body}</p>}
        {message.attachments.length > 0 && (
          <ul className={styles['thread__files']}>
            {message.attachments.map((file) => (
              <li key={file.id}>
                <a href={file.url} target="_blank" rel="noreferrer" className={styles['thread__file']}>
                  {file.kind === 'Image' && file.thumbnailUrl ? (
                    <img src={file.thumbnailUrl} alt={file.fileName} className={styles['thread__image']} />
                  ) : (
                    <>
                      <Icon name={file.kind === 'Video' ? 'camera' : 'requests'} size={16} />
                      {file.fileName}
                    </>
                  )}
                </a>
              </li>
            ))}
          </ul>
        )}
        <span className={styles['thread__meta']}>
          <time dateTime={message.sentAt}>{formatDate(message.sentAt, i18n.language, { hour: '2-digit', minute: '2-digit' })}</time>
          {seen && <span className={styles['thread__seen']}>{t('chat.seen')}</span>}
        </span>
      </div>
    </li>
  )
}

/**
 * An open conversation: who and what it's about, the messages (older ones on request), and the composer.
 * Opening it, and new messages arriving while it's open, mark it read.
 */
export default function Thread({ id }) {
  const { t, i18n } = useTranslation()
  const path = useLocalizedPath()
  const conversation = useGetConversationQuery(id)
  const messages = useGetMessagesQuery(id)
  const [loadOlder, loadingOlder] = useGetOlderMessagesMutation()
  const [markRead] = useMarkReadMutation()
  const [older, setOlder] = useState({ items: [], hasMore: null })
  const endRef = useRef(null)
  const latest = messages.data?.items ?? []
  const all = [...older.items, ...latest]
  const hasMore = older.hasMore ?? messages.data?.hasMore ?? false
  const unread = conversation.data?.unreadCount ?? 0

  useEffect(() => {
    if (unread > 0) markRead(id)
  }, [unread, id, markRead])

  useEffect(() => {
    endRef.current?.scrollIntoView?.({ block: 'end' })
  }, [latest.length])

  const showOlder = async () => {
    const result = await loadOlder({ id, before: all[0].sentAt })
    if (result.data) setOlder({ items: [...result.data.items, ...older.items], hasMore: result.data.hasMore })
  }

  return (
    <QueryState query={conversation} notFound={<EmptyState icon="messages" title={t('chat.notFound')} />}>
      {(chat) => {
        const name = chat.otherParty.name ?? t(`chat.role.${chat.myRole === 'Customer' ? 'Partner' : 'Customer'}`)
        const requestLink = chat.myRole === 'Customer' ? path(`/requests/${chat.requestId}`) : path(`/inbox/${chat.requestId}`)
        const lastMine = [...all].reverse().find((message) => message.senderRole === chat.myRole)
        return (
          <div className={styles.thread}>
            <header className={styles['thread__header']}>
              <Link to={path('/messages')} className={styles['thread__back']} aria-label={t('chat.backToList')}>
                <Icon name="chevronLeft" />
              </Link>
              <span className={styles['conversation-list__avatar']} aria-hidden="true">
                {initials(name)}
              </span>
              <div className={styles['thread__who']}>
                <h2 className={styles['thread__name']}>{name}</h2>
                <Link to={requestLink} className={styles['thread__about']}>
                  {placeText(chat.place, t)}
                </Link>
              </div>
              {chat.orderId && (
                <Button to={path(`/orders/${chat.orderId}`)} variant="secondary" size="sm">
                  {t('chat.openOrder')}
                </Button>
              )}
            </header>

            <div className={styles['thread__scroll']}>
              {hasMore && (
                <div className={styles['thread__older']}>
                  <Button variant="ghost" size="sm" loading={loadingOlder.isLoading} onClick={showOlder}>
                    {t('chat.older')}
                  </Button>
                </div>
              )}
              {messages.isError && <Alert tone="danger" title={errorMessage(t, messages.error)} />}
              {messages.isSuccess && all.length === 0 && <p className={styles['thread__hint']}>{t('chat.startHint')}</p>}
              <ol className={styles['thread__messages']} aria-label={t('chat.messagesLabel')}>
                {all.map((message, index) => (
                  <MessageWithDay
                    key={message.id}
                    message={message}
                    newDay={index === 0 || dayOf(all[index - 1].sentAt) !== dayOf(message.sentAt)}
                    lng={i18n.language}
                    mine={message.senderRole === chat.myRole}
                    seen={message === lastMine && chat.otherReadAt != null && new Date(chat.otherReadAt) >= new Date(message.sentAt)}
                  />
                ))}
              </ol>
              <div ref={endRef} />
            </div>

            {chat.canSend ? <Composer conversationId={chat.id} /> : <Alert tone="info" title={t('chat.closed')} />}
          </div>
        )
      }}
    </QueryState>
  )
}

function MessageWithDay({ message, newDay, lng, mine, seen }) {
  return (
    <>
      {newDay && (
        <li className={styles['thread__day']} aria-hidden="true">
          {formatDate(message.sentAt, lng, { weekday: 'short', day: 'numeric', month: 'long' })}
        </li>
      )}
      <Bubble message={message} mine={mine} seen={seen} />
    </>
  )
}
