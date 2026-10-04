import { useTranslation } from 'react-i18next'
import { Link } from 'react-router'
import Avatar from '@/components/Avatar/Avatar'
import Stars from '@/components/Stars/Stars'
import Icon from '@/components/ui/Icon/Icon'
import Tag from '@/components/ui/Tag/Tag'
import { useLocalizedPath } from '@/i18n/hooks'
import styles from './PartnerCard.module.scss'

/** "★★★★★ 4.8 (12 reviews)": the average rating and how many reviews it is based on. */
export function Rating({ rating, count, className }) {
  const { t, i18n } = useTranslation()
  return (
    <p className={[styles['partner-card__rating'], className].filter(Boolean).join(' ')}>
      <Stars value={rating} />
      <strong>{rating.toLocaleString(i18n.language, { minimumFractionDigits: 1, maximumFractionDigits: 1 })}</strong>
      <span>{t('reviews.count', { n: count })}</span>
    </p>
  )
}

/** A partner in search results: the first work example as a cover, who they are, what and where they work. */
export default function PartnerCard({ partner }) {
  const { t } = useTranslation()
  const path = useLocalizedPath()
  const cover = partner.cover?.thumbnailUrl ?? (partner.cover?.kind === 'Image' ? partner.cover.url : null)

  return (
    <article className={styles['partner-card']}>
      <div className={styles['partner-card__cover']}>
        {cover ? <img src={cover} alt="" loading="lazy" className={styles['partner-card__cover-image']} /> : <Icon name="image" size={32} />}
      </div>
      <div className={styles['partner-card__body']}>
        <Avatar name={partner.displayName} src={partner.avatar?.thumbnailUrl ?? partner.avatar?.url} size="lg" className={styles['partner-card__avatar']} />
        <h3 className={styles['partner-card__name']}>
          <Link to={path(`/partners/${partner.slug}`)} className={styles['partner-card__link']}>
            {partner.displayName}
          </Link>
        </h3>
        <p className={styles['partner-card__meta']}>
          <Tag tone={partner.type === 'Company' ? 'info' : 'neutral'}>{t(`public.partnerType.${partner.type}`)}</Tag>
          {partner.yearsOfExperience != null && <span>{t('public.years', { n: partner.yearsOfExperience })}</span>}
        </p>
        {partner.reviewCount > 0 && <Rating rating={partner.rating} count={partner.reviewCount} />}
        {partner.categories.length > 0 && (
          <p className={styles['partner-card__services']}>{partner.categories.map((category) => category.name).join(' · ')}</p>
        )}
        {partner.aboutExcerpt && <p className={styles['partner-card__about']}>{partner.aboutExcerpt}</p>}
        <p className={styles['partner-card__footer']}>
          {partner.cities.length > 0 && (
            <span className={styles['partner-card__fact']}>
              <Icon name="pin" size={16} />
              {partner.cities.join(', ')}
            </span>
          )}
          {partner.workExampleCount > 0 && (
            <span className={styles['partner-card__fact']}>
              <Icon name="image" size={16} />
              {t('public.workCount', { n: partner.workExampleCount })}
            </span>
          )}
        </p>
      </div>
    </article>
  )
}
