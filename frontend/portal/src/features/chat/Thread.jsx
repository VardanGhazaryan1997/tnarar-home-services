import { useEffect, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { useDispatch } from 'react-redux'
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
import { formatDate, formatMoney } from '@/shared/format'
import { conversationAlertsCleared } from './alerts/alertsSlice'
import { watchOpenConversation } from './alerts/focus'
import { useGetConversationQuery, useGetMessagesQuery, useGetOlderMessagesMutation, useMarkReadMutation } from './chatApi'
import Composer from './Composer'
import { useOutbox, useOutboxSender } from './outbox'
import { useThreadScroll } from './useThreadScroll'
import OfferPanel from './offer/OfferPanel'
import { offerEvents, useNow } from './offer/offerTimeline'
import { initials } from './initials'
import styles from './chat.module.scss'

const b = bem(styles)
const dayOf = (iso) => iso.slice(0, 10)

/**
 * One message bubble with its files and time. `afterCancel`: sent after the request was cancelled (shown lighter);
 * the first such message also says so in words.
 */
function Bubble({ message, mine, seen, afterCancel = false, firstAfterCancel = false }) {
  const { t, i18n } = useTranslation()
  return (
    <li id={`message-${message.id}`} className={b('thread__message', { mine, 'after-cancel': afterCancel })}>
      {firstAfterCancel && <span className={styles['thread__after-label']}>{t('chat.cancel.after')}</span>}
      <div className={b('thread__bubble', { mine, 'after-cancel': afterCancel })}>
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
  const [unreadAtOpen, setUnreadAtOpen] = useState(null)
  const latest = messages.data?.items ?? []
  const all = [...older.items, ...latest]
  const hasMore = older.hasMore ?? messages.data?.hasMore ?? false
  const unread = conversation.data?.unreadCount ?? 0
  const dispatch = useDispatch()
  const now = useNow()

  // While this conversation is on screen, its new messages need no alert; the ones already shown are done.
  useEffect(() => {
    dispatch(conversationAlertsCleared(id))
    return watchOpenConversation(id)
  }, [id, dispatch])

  useEffect(() => {
    if (unread > 0) markRead(id)
  }, [unread, id, markRead])

  // How many were unread when the conversation opened (marking it read sets the count to 0 right after).
  const loadedUnread = conversation.data?.unreadCount
  if (unreadAtOpen === null && loadedUnread !== undefined) setUnreadAtOpen(loadedUnread)

  const scroll = useThreadScroll(latest, conversation.data?.myRole, unreadAtOpen)
  // Messages on their way: the ones the server already has (same client id) show as normal messages.
  const sentIds = new Set(all.map((message) => message.clientId).filter(Boolean))
  const pending = useOutbox(id).filter((item) => !sentIds.has(item.clientId))
  const sender = useOutboxSender(id)

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
        // Offer events between the messages, in time order (none before the oldest loaded message).
        const events = offerEvents(chat.offers, now, hasMore ? all[0]?.sentAt : null)
        // The cancellation, where it happened in the conversation (unless that part isn't loaded yet).
        const cancelledAt = chat.requestCancelledAt
        const showCancel = cancelledAt && !(hasMore && all[0] && new Date(cancelledAt) < new Date(all[0].sentAt))
        const timeline = [
          ...all.map((message) => ({ key: message.id, at: message.sentAt, message })),
          ...events.map((event) => ({ key: event.id, at: event.at, event })),
          ...(showCancel ? [{ key: 'cancelled', at: cancelledAt, cancelled: true }] : []),
        ].sort((a, b) => new Date(a.at) - new Date(b.at))
        const isAfterCancel = (message) => Boolean(cancelledAt) && new Date(message.sentAt) > new Date(cancelledAt)
        const firstAfterCancel = all.find(isAfterCancel)
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

            <OfferPanel conversation={chat} />

            <div className={styles['thread__scroll']} ref={scroll.scrollRef} onScroll={scroll.onScroll}>
              {hasMore && (
                <div className={styles['thread__older']}>
                  <Button variant="ghost" size="sm" loading={loadingOlder.isLoading} onClick={showOlder}>
                    {t('chat.older')}
                  </Button>
                </div>
              )}
              {messages.isError && <Alert tone="danger" title={errorMessage(t, messages.error)} />}
              {messages.isSuccess && timeline.length === 0 && <p className={styles['thread__hint']}>{t('chat.startHint')}</p>}
              <ol className={styles['thread__messages']} aria-label={t('chat.messagesLabel')}>
                {timeline.map((item, index) => (
                  <ItemWithDay key={item.key} at={item.at} newDay={index === 0 || dayOf(timeline[index - 1].at) !== dayOf(item.at)} lng={i18n.language}>
                    {item.message?.id === scroll.dividerId && (
                      <li className={styles['thread__new-divider']}>
                        <span>{t('chat.newMessages')}</span>
                      </li>
                    )}
                    {item.message && (
                      <Bubble
                        message={item.message}
                        mine={item.message.senderRole === chat.myRole}
                        seen={item.message === lastMine && chat.otherReadAt != null && new Date(chat.otherReadAt) >= new Date(item.message.sentAt)}
                        afterCancel={isAfterCancel(item.message)}
                        firstAfterCancel={item.message === firstAfterCancel}
                      />
                    )}
                    {item.event && <OfferEvent event={item.event} lng={i18n.language} />}
                    {item.cancelled && <CancelMarker at={item.at} reason={chat.requestCancelReason} lng={i18n.language} />}
                  </ItemWithDay>
                ))}
              </ol>
              {pending.length > 0 && (
                <ol className={styles['thread__messages']} aria-label={t('chat.outbox.label')}>
                  {pending.map((item) => (
                    <PendingBubble key={item.clientId} item={item} onRetry={() => sender.retry(item)} onDiscard={() => sender.discard(item)} />
                  ))}
                </ol>
              )}
              <div ref={scroll.endRef} />
              {scroll.showLatest && (
                <div className={styles['thread__latest']}>
                  <Button variant="primary" size="sm" icon={<Icon name="chevronDown" size={16} />} onClick={scroll.scrollToEnd}>
                    {scroll.missed > 0 ? t('chat.latestCount', { n: scroll.missed }) : t('chat.latest')}
                  </Button>
                </div>
              )}
            </div>

            {chat.canSend && chat.canSendUntil && (
              <p className={styles['thread__cancel-note']}>
                <Icon name="info" size={16} />
                {t('chat.cancel.openUntil', { date: formatDate(chat.canSendUntil, i18n.language, { day: 'numeric', month: 'long', hour: '2-digit', minute: '2-digit' }) })}
              </p>
            )}
            {chat.canSend ? (
              <Composer conversationId={chat.id} onSent={() => requestAnimationFrame(scroll.scrollToEnd)} />
            ) : (
              <Alert tone="info" title={chat.canSendUntil ? t('chat.cancel.closed') : t('chat.closed')} />
            )}
          </div>
        )
      }}
    </QueryState>
  )
}

function ItemWithDay({ at, newDay, lng, children }) {
  return (
    <>
      {newDay && (
        <li className={styles['thread__day']} aria-hidden="true">
          {formatDate(at, lng, { weekday: 'short', day: 'numeric', month: 'long' })}
        </li>
      )}
      {children}
    </>
  )
}

/** A message of mine on its way: "Sending…", or "Not sent" with the reason, Retry and Discard (its text is kept). */
function PendingBubble({ item, onRetry, onDiscard }) {
  const { t } = useTranslation()
  const failed = item.status === 'failed'
  return (
    <li className={b('thread__message', { mine: true })}>
      <div className={b('thread__bubble', { mine: true, failed })}>
        {item.body && <p className={styles['thread__body']}>{item.body}</p>}
        {item.files?.length > 0 && <p className={styles['thread__pending-files']}>{t('chat.files', { n: item.files.length })}</p>}
        {failed ? (
          <div className={styles['thread__failed']} role="alert">
            <span className={styles['thread__failed-text']}>
              <Icon name="info" size={16} />
              {t('chat.outbox.failed')} {t(item.error ? `errors.${item.error}` : 'chat.outbox.failedHint', { defaultValue: t('chat.outbox.failedHint') })}
            </span>
            <span className={styles['thread__failed-actions']}>
              <Button variant="secondary" size="sm" onClick={onRetry}>
                {t('chat.outbox.retry')}
              </Button>
              <Button variant="ghost" size="sm" onClick={onDiscard}>
                {t('chat.outbox.discard')}
              </Button>
            </span>
          </div>
        ) : (
          <span className={styles['thread__meta']}>
            <Icon name="clock" size={14} />
            {t('chat.outbox.sending')}
          </span>
        )}
      </div>
    </li>
  )
}

/**
 * Where the request was cancelled in the conversation: a pill across the thread with the date, opening the
 * customer's reason. The messages after it are shown lighter.
 */
function CancelMarker({ at, reason, lng }) {
  const { t } = useTranslation()
  const [open, setOpen] = useState(false)
  const id = `cancel-reason-${at}`
  return (
    <li className={styles['thread__cancelled']}>
      <button
        type="button"
        className={styles['thread__cancelled-pill']}
        aria-expanded={open}
        aria-controls={id}
        title={open ? t('chat.cancel.hideReason') : t('chat.cancel.showReason')}
        onClick={() => setOpen(!open)}
      >
        <Icon name="close" size={16} />
        {t('chat.cancel.marker', { date: formatDate(at, lng, { day: 'numeric', month: 'short', hour: '2-digit', minute: '2-digit' }) })}
        <Icon name={open ? 'chevronUp' : 'chevronDown'} size={14} />
      </button>
      {open && (
        <p id={id} className={styles['thread__cancelled-reason']}>
          {reason ? t('chat.cancel.reason', { reason }) : t('chat.cancel.noReason')}
        </p>
      )}
    </li>
  )
}

const EVENT_ICONS = { sent: 'send', accepted: 'check', rejected: 'close', withdrawn: 'info', expired: 'clock' }

/** "Offer sent · 450 000 ֏", "Offer accepted"…: what happened to an offer, between the messages. */
function OfferEvent({ event, lng }) {
  const { t } = useTranslation()
  const { offer } = event
  const price = offer.kind === 'Visit' && offer.price === 0 ? t('offers.free') : formatMoney(offer.price, lng)
  return (
    <li className={b('thread__event', { [event.kind]: true })}>
      <Icon name={EVENT_ICONS[event.kind]} size={16} />
      <span>{t(`chat.offer.events.${event.kind}`, { price })}</span>
      <time dateTime={event.at} className={styles['thread__event-time']}>
        {formatDate(event.at, lng, { hour: '2-digit', minute: '2-digit' })}
      </time>
    </li>
  )
}
