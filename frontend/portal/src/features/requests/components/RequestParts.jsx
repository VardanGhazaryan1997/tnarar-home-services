import { useTranslation } from 'react-i18next'
import { Link } from 'react-router'
import Icon from '@/components/ui/Icon/Icon'
import Tag from '@/components/ui/Tag/Tag'
import LiveNote from '@/features/live/LiveNote'
import { LIVE_KINDS, useLiveChange } from '@/features/live/useLiveChange'
import { bem } from '@/shared/bem'
import { formatBudget, formatDate } from '@/shared/format'
import styles from './RequestParts.module.scss'

const b = bem(styles)

const RECIPIENT_TONES = { New: 'accent', Viewed: 'neutral', Responded: 'success', Declined: 'neutral' }

/** A request's status in words with an icon: "Accepted" only for one closed by an accepted offer (it has an order). */
const STATUS_LOOKS = {
  Open: { tone: 'info', icon: 'clock', label: 'Open' },
  Accepted: { tone: 'success', icon: 'check', label: 'Accepted' },
  Closed: { tone: 'neutral', icon: 'info', label: 'Ended' },
  Cancelled: { tone: 'neutral', icon: 'close', label: 'Cancelled' },
}

export function RequestStatusTag({ status, orderId = null }) {
  const { t } = useTranslation()
  const look = STATUS_LOOKS[status === 'Closed' && orderId ? 'Accepted' : status] ?? { tone: 'neutral', icon: 'info', label: status }
  return (
    <Tag tone={look.tone}>
      <Icon name={look.icon} size={14} />
      {t(`requests.status.${look.label}`)}
    </Tag>
  )
}

/** What the partner did with a received request. */
export function RecipientStatusTag({ status }) {
  const { t } = useTranslation()
  return <Tag tone={RECIPIENT_TONES[status] ?? 'neutral'}>{t(`inbox.status.${status}`)}</Tag>
}

/**
 * A request in a list: a whole-card link with title, excerpt, tags and a meta line. `liveFor`: { id, side } to point
 * out a change that just happened to that request, with a fading tint, an edge in its tone and a note under the link.
 */
export function RequestCard({ to, title, excerpt, tags, meta, highlight = false, liveFor = null }) {
  const change = useLiveChange(liveFor?.id, liveFor?.side)
  const tone = change ? LIVE_KINDS[change.kind].tone : null
  return (
    <li className={b('request-card', { highlight: highlight && !tone, [`live-${tone}`]: Boolean(tone) })}>
      <Link to={to} className={styles['request-card__link']}>
        <span className={styles['request-card__top']}>
          <span className={styles['request-card__title']}>{title}</span>
          <span className={styles['request-card__tags']}>{tags}</span>
        </span>
        <span className={styles['request-card__excerpt']}>{excerpt}</span>
        <span className={styles['request-card__meta']}>{meta}</span>
      </Link>
      {tone && <LiveNote change={change} side={liveFor.side} className={styles['request-card__live']} />}
    </li>
  )
}

export function RequestList({ children }) {
  return <ul className={styles['request-list']}>{children}</ul>
}

/** Where, when and (for the customer only) the budget, as a definition list. */
export function RequestFacts({ request, showBudget = false }) {
  const { t, i18n } = useTranslation()
  const budget = showBudget ? formatBudget(request.budgetMin, request.budgetMax, i18n.language, t) : null
  const when = [formatDate(request.preferredDate, i18n.language), request.timeNote].filter(Boolean).join(', ')

  return (
    <dl className={styles['request-facts']}>
      <div className={styles['request-facts__row']}>
        <dt>
          <Icon name="requests" size={18} />
          {t('requests.fields.category')}
        </dt>
        <dd>{request.place.categoryName}</dd>
      </div>
      <div className={styles['request-facts__row']}>
        <dt>
          <Icon name="pin" size={18} />
          {t('requests.fields.place')}
        </dt>
        <dd>{request.place.districtName ? `${request.place.cityName}, ${request.place.districtName}` : request.place.cityName}</dd>
      </div>
      <div className={styles['request-facts__row']}>
        <dt>
          <Icon name="calendar" size={18} />
          {t('requests.fields.when')}
        </dt>
        <dd>{when || t('requests.flexible')}</dd>
      </div>
      {showBudget && (
        <div className={styles['request-facts__row']}>
          <dt>
            <Icon name="money" size={18} />
            {t('requests.fields.budget')}
          </dt>
          <dd>{budget ?? t('requests.noBudget')}</dd>
        </div>
      )}
    </dl>
  )
}

/** The request's photos (thumbnails linking to the full image) and videos. */
export function MediaGallery({ files }) {
  const { t } = useTranslation()
  if (!files.length) return null

  return (
    <ul className={styles['media-gallery']} aria-label={t('requests.photos')}>
      {files.map((file) => (
        <li key={file.id} className={styles['media-gallery__item']}>
          <a href={file.url} target="_blank" rel="noreferrer" className={styles['media-gallery__link']}>
            {file.kind === 'Video' ? (
              <span className={styles['media-gallery__video']}>
                <Icon name="camera" size={28} label={file.fileName} />
              </span>
            ) : (
              <img src={file.thumbnailUrl ?? file.url} alt={file.fileName} className={styles['media-gallery__image']} loading="lazy" />
            )}
          </a>
        </li>
      ))}
    </ul>
  )
}
