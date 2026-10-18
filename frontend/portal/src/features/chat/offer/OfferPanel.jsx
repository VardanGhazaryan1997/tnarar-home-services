import { useId, useState } from 'react'
import { useTranslation } from 'react-i18next'
import Button from '@/components/ui/Button/Button'
import Icon from '@/components/ui/Icon/Icon'
import { AcceptOfferModal, RejectOfferModal } from '@/features/offers/OfferDecisions'
import { useLocalizedPath } from '@/i18n/hooks'
import { bem } from '@/shared/bem'
import { formatDate, formatMoney, formatTimeLeft } from '@/shared/format'
import { currentOffer, statusNow, useNow } from './offerTimeline'
import styles from './OfferPanel.module.scss'

const b = bem(styles)
const DAY_MS = 24 * 60 * 60 * 1000

/** Each status's tone and icon; the words always come with them. */
const LOOKS = {
  Sent: { tone: 'info', icon: 'clock' },
  Accepted: { tone: 'success', icon: 'check' },
  Rejected: { tone: 'danger', icon: 'close' },
  Expired: { tone: 'warning', icon: 'clock' },
  Closed: { tone: 'neutral', icon: 'info' },
  Withdrawn: { tone: 'neutral', icon: 'info' },
}

/**
 * The specialist's offer, pinned under the chat's header (T3): price, status and expiry in one compact row, with
 * the summary behind a toggle. The customer accepts or declines it here (accepting keeps them in the chat and
 * links the new order); an expired offer can't be accepted. The partner sees where their offer stands.
 */
export default function OfferPanel({ conversation }) {
  const { t, i18n } = useTranslation()
  const path = useLocalizedPath()
  const now = useNow()
  const moreId = useId()
  const [expanded, setExpanded] = useState(false)
  const [accepting, setAccepting] = useState(null)
  const [rejecting, setRejecting] = useState(null)
  const [createdOrderId, setCreatedOrderId] = useState(null)
  const offer = currentOffer(conversation.offers)
  if (!offer) return null

  const lng = i18n.language
  const customer = conversation.myRole === 'Customer'
  const status = createdOrderId ? 'Accepted' : statusNow(offer, now)
  const orderId = createdOrderId ?? offer.orderId
  const look = LOOKS[status] ?? LOOKS.Closed
  const canDecide = customer && status === 'Sent' && conversation.requestStatus === 'Open'
  const expiringSoon = status === 'Sent' && new Date(offer.expiresAt) - now < DAY_MS
  const name = conversation.otherParty.name ?? t('chat.role.Partner')
  const title = customer ? t('chat.offer.label', { name }) : t('chat.offer.mine')
  const price = offer.kind === 'Visit' && offer.price === 0 ? t('offers.free') : formatMoney(offer.price, lng)
  const detailsLink = customer ? path(`/requests/${conversation.requestId}#offer-${offer.id}`) : path(`/inbox/${conversation.requestId}`)

  const statusText =
    status === 'Sent'
      ? customer
        ? t('chat.offer.expiresIn', { when: formatTimeLeft(offer.expiresAt, lng, now) })
        : t('chat.offer.waiting')
      : t(`offers.status.${status}`)
  const message = {
    Expired: t('chat.offer.expiredText'),
    Accepted: customer ? t('chat.offer.accepted') : t('chat.offer.partnerAccepted'),
    Rejected: customer ? t('chat.offer.rejected') : t('chat.offer.partnerRejected'),
    Closed: t('chat.offer.closed'),
    Withdrawn: t('chat.offer.withdrawn'),
  }[status]

  return (
    <section className={b('offer-panel', { [look.tone]: true, expanded })} aria-label={title}>
      <div className={styles['offer-panel__row']}>
        <span className={styles['offer-panel__icon']} aria-hidden="true">
          <Icon name="money" size={20} />
        </span>
        <div className={styles['offer-panel__main']}>
          <p className={styles['offer-panel__title']}>
            {title} · {t(`offers.kind.${offer.kind}`)}
          </p>
          <p className={styles['offer-panel__price']}>{price}</p>
        </div>
        <p className={b('offer-panel__status', { soon: expiringSoon })}>
          <Icon name={look.icon} size={16} />
          <span>{statusText}</span>
        </p>
        <div className={styles['offer-panel__actions']}>
          {canDecide && (
            <>
              <Button variant="accent" size="sm" icon={<Icon name="check" size={16} />} onClick={() => setAccepting(offer)}>
                {t('offers.accept')}
              </Button>
              <Button variant="secondary" size="sm" onClick={() => setRejecting(offer)}>
                {t('offers.reject')}
              </Button>
            </>
          )}
          {customer && status === 'Expired' && (
            <Button variant="accent" size="sm" disabled aria-describedby={moreId}>
              {t('offers.accept')}
            </Button>
          )}
          {orderId && (
            <Button to={path(`/orders/${orderId}`)} variant="primary" size="sm">
              {t('offers.openOrder')}
            </Button>
          )}
          <span className={styles['offer-panel__details']}>
            <Button to={detailsLink} variant="ghost" size="sm">
              {t('chat.offer.details')}
            </Button>
          </span>
        </div>
        <button
          type="button"
          className={styles['offer-panel__toggle']}
          aria-expanded={expanded}
          aria-controls={moreId}
          aria-label={expanded ? t('chat.offer.showLess') : t('chat.offer.showMore')}
          onClick={() => setExpanded(!expanded)}
        >
          <Icon name={expanded ? 'chevronUp' : 'chevronDown'} size={18} />
        </button>
      </div>
      <div id={moreId} className={styles['offer-panel__more']}>
        {message && <p className={styles['offer-panel__message']}>{message}</p>}
        {expanded && (
          <>
            <p className={styles['offer-panel__summary']}>{offer.summary}</p>
            {status === 'Sent' && <p className={styles['offer-panel__expiry']}>{t('offers.validUntil', { date: formatDate(offer.expiresAt, lng) })}</p>}
          </>
        )}
      </div>

      <AcceptOfferModal
        offer={accepting}
        partnerName={name}
        onClose={() => setAccepting(null)}
        onAccepted={(order) => {
          setAccepting(null)
          setCreatedOrderId(order.id)
        }}
      />
      <RejectOfferModal offer={rejecting} onClose={() => setRejecting(null)} onRejected={() => setRejecting(null)} />
    </section>
  )
}
