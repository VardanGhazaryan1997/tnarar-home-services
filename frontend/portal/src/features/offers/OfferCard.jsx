import { useTranslation } from 'react-i18next'
import Icon from '@/components/ui/Icon/Icon'
import Tag from '@/components/ui/Tag/Tag'
import { bem } from '@/shared/bem'
import { formatDate, formatDateTime, formatMoney } from '@/shared/format'
import styles from './offers.module.scss'

const b = bem(styles)

const STATUS_TONES = { Sent: 'info', Accepted: 'success', Rejected: 'danger', Withdrawn: 'neutral', Expired: 'neutral', Closed: 'neutral' }

export function OfferStatusTag({ status }) {
  const { t } = useTranslation()
  return <Tag tone={STATUS_TONES[status] ?? 'neutral'}>{t(`offers.status.${status}`)}</Tag>
}

/** The terms of an offer (or of an order made from one): scope, price, dates and payment plan. */
export function OfferTerms({ terms, kind }) {
  const { t, i18n } = useTranslation()
  const included = terms.lines.filter((line) => line.included)
  const excluded = terms.lines.filter((line) => !line.included)

  return (
    <div className={styles['offer-terms']}>
      <p className={styles['offer-terms__summary']}>{terms.summary}</p>
      {included.length > 0 && (
        <ul className={styles['offer-terms__lines']} aria-label={t('offers.included')}>
          {included.map((line, index) => (
            <li key={`${index}-${line.title}`} className={styles['offer-terms__line']}>
              <Icon name="check" size={16} className={styles['offer-terms__icon']} />
              {line.title}
            </li>
          ))}
        </ul>
      )}
      {excluded.length > 0 && (
        <ul className={styles['offer-terms__lines']} aria-label={t('offers.excluded')}>
          {excluded.map((line, index) => (
            <li key={`${index}-${line.title}`} className={b('offer-terms__line', { excluded: true })}>
              <Icon name="close" size={16} className={styles['offer-terms__icon']} />
              {line.title}
            </li>
          ))}
        </ul>
      )}
      <dl className={styles['offer-terms__facts']}>
        {kind === 'Visit' ? (
          <div>
            <dt>{t('offers.fields.visitAt')}</dt>
            <dd>{formatDateTime(terms.visitAt, i18n.language)}</dd>
          </div>
        ) : (
          <>
            <div>
              <dt>{t('offers.fields.start')}</dt>
              <dd>{terms.startDate ? formatDate(terms.startDate, i18n.language) : t('offers.startToAgree')}</dd>
            </div>
            <div>
              <dt>{t('offers.fields.duration')}</dt>
              <dd>{terms.durationDays ? t('offers.days', { n: terms.durationDays }) : '—'}</dd>
            </div>
            <div>
              <dt>{t('offers.fields.materials')}</dt>
              <dd>{terms.materialsIncluded ? t('offers.materialsIncluded') : t('offers.materialsNotIncluded')}</dd>
            </div>
          </>
        )}
      </dl>
      {terms.materialsNote && <p className={styles['offer-terms__note']}>{terms.materialsNote}</p>}
      {terms.stages.length > 1 && (
        <ol className={styles['offer-terms__stages']} aria-label={t('offers.paymentPlan')}>
          {terms.stages.map((stage, index) => (
            <li key={`${stage.purpose}-${index}`} className={styles['offer-terms__stage']}>
              <span>{stage.title ?? t(`offers.purpose.${stage.purpose}`)}</span>
              <span className={styles['offer-terms__amount']}>{formatMoney(stage.amount, i18n.language)}</span>
            </li>
          ))}
        </ol>
      )}
    </div>
  )
}

/** An offer for comparison: who, the price, the terms, its status and the actions passed in. */
export default function OfferCard({ offer, showPartner = true, actions, footer }) {
  const { t, i18n } = useTranslation()
  const free = offer.kind === 'Visit' && offer.price === 0

  return (
    <article className={b('offer-card', { open: offer.status === 'Sent', accepted: offer.status === 'Accepted' })} aria-label={offer.partner.displayName}>
      <header className={styles['offer-card__header']}>
        <div className={styles['offer-card__who']}>
          {showPartner && <h3 className={styles['offer-card__partner']}>{offer.partner.displayName}</h3>}
          <span className={styles['offer-card__tags']}>
            <Tag tone={offer.kind === 'Visit' ? 'warning' : 'neutral'}>{t(`offers.kind.${offer.kind}`)}</Tag>
            {offer.status !== 'Sent' && <OfferStatusTag status={offer.status} />}
          </span>
        </div>
        <p className={styles['offer-card__price']}>{free ? t('offers.free') : formatMoney(offer.price, i18n.language)}</p>
      </header>
      <OfferTerms terms={offer} kind={offer.kind} />
      {offer.status === 'Sent' && (
        <p className={styles['offer-card__expiry']}>
          <Icon name="clock" size={16} />
          {t('offers.validUntil', { date: formatDate(offer.expiresAt, i18n.language) })}
        </p>
      )}
      {offer.rejectReason && <p className={styles['offer-card__expiry']}>{t('offers.rejectReasonShown', { reason: offer.rejectReason })}</p>}
      {actions && <div className={styles['offer-card__actions']}>{actions}</div>}
      {footer}
    </article>
  )
}
