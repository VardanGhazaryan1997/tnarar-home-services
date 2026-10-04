import { useTranslation } from 'react-i18next'
import Alert from '@/components/ui/Alert/Alert'
import Button from '@/components/ui/Button/Button'
import Card from '@/components/ui/Card/Card'
import Icon from '@/components/ui/Icon/Icon'
import { useLocalizedPath } from '@/i18n/hooks'
import { bem } from '@/shared/bem'
import { formatDate } from '@/shared/format'
import ProfileSummary from './ProfileSummary'
import styles from './partner.module.scss'

const b = bem(styles)
const LOOK = {
  UnderReview: { icon: 'clock', tone: 'info' },
  Approved: { icon: 'shield', tone: 'success' },
  Rejected: { icon: 'close', tone: 'danger' },
  Suspended: { icon: 'info', tone: 'warning' },
}

/** Where a submitted profile stands: under review, live (with Edit), rejected or suspended, and what it contains. */
export default function ProfileStatus({ profile, categories, cities, onEdit, notice }) {
  const { t, i18n } = useTranslation()
  const path = useLocalizedPath()
  const look = LOOK[profile.status]

  return (
    <div className={styles['profile-status']}>
      {notice && <Alert tone="success" title={t(`partner.notice.${notice}`)} />}
      <section className={b('profile-status__banner', { tone: look.tone })}>
        <span className={styles['profile-status__icon']}>
          <Icon name={look.icon} size={28} />
        </span>
        <div className={styles['profile-status__text']}>
          <h2 className={styles['profile-status__title']}>{t(`partner.status.${profile.status}.title`)}</h2>
          <p>{t(`partner.status.${profile.status}.text`)}</p>
          {profile.status === 'UnderReview' && profile.submittedAt && (
            <p className={styles['profile-status__muted']}>{t('partner.submittedAt', { date: formatDate(profile.submittedAt, i18n.language) })}</p>
          )}
          {profile.reviewComment && profile.status !== 'Approved' && (
            <blockquote className={styles['profile-status__comment']}>{profile.reviewComment}</blockquote>
          )}
        </div>
        {profile.status === 'Approved' && (
          <div className={styles['profile-status__actions']}>
            <Button to={path(`/partners/${profile.slug}`)} variant="primary" icon={<Icon name="account" />}>
              {t('partner.viewPublic')}
            </Button>
            <Button variant="secondary" icon={<Icon name="edit" />} onClick={onEdit}>
              {t('partner.edit')}
            </Button>
            <Button to={path('/inbox')} variant="ghost" icon={<Icon name="inbox" />}>
              {t('partner.openInbox')}
            </Button>
          </div>
        )}
      </section>
      <Card title={t('partner.yourProfile')}>
        <ProfileSummary profile={profile} categories={categories} cities={cities} />
      </Card>
    </div>
  )
}
