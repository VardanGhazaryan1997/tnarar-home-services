import { useTranslation } from 'react-i18next'
import Avatar from '@/components/Avatar/Avatar'
import Button from '@/components/ui/Button/Button'
import Icon from '@/components/ui/Icon/Icon'
import Tag from '@/components/ui/Tag/Tag'
import { allCategories } from '@/features/public/categories'
import { areaNames } from './profileForm'
import styles from './partner.module.scss'

/** The profile as the team will review it, section by section. `onEdit(step)` adds an Edit link to each. */
export default function ProfileSummary({ profile, categories, cities, onEdit }) {
  const { t } = useTranslation()
  const names = new Map(allCategories(categories).map((category) => [category.id, category.name]))

  const section = (step, title, content) => (
    <section className={styles['profile-summary__section']}>
      <div className={styles['profile-summary__head']}>
        <h3 className={styles['profile-summary__title']}>{title}</h3>
        {onEdit && (
          <Button variant="ghost" size="sm" icon={<Icon name="edit" />} onClick={() => onEdit(step)} aria-label={t('partner.editSection', { section: title })}>
            {t('common.edit')}
          </Button>
        )}
      </div>
      {content}
    </section>
  )

  return (
    <div className={styles['profile-summary']}>
      {section(
        'type',
        t('partner.steps.type'),
        <div className={styles['profile-summary__who']}>
          <Avatar name={profile.displayName} src={profile.avatar?.thumbnailUrl ?? profile.avatar?.url} size="lg" />
          <div>
            <p className={styles['profile-summary__name']}>{profile.displayName}</p>
            <p className={styles['profile-summary__muted']}>
              {t(`public.partnerType.${profile.type}`)}
              {profile.yearsOfExperience != null && ` · ${t('public.years', { n: profile.yearsOfExperience })}`}
            </p>
          </div>
        </div>,
      )}
      {section(
        'services',
        t('partner.steps.services'),
        profile.categoryIds.length ? (
          <ul className={styles['profile-summary__tags']}>
            {profile.categoryIds.map((id) => (
              <li key={id}>
                <Tag>{names.get(id) ?? '…'}</Tag>
              </li>
            ))}
          </ul>
        ) : (
          <p className={styles['profile-summary__muted']}>{t('partner.none')}</p>
        ),
      )}
      {section(
        'areas',
        t('partner.steps.areas'),
        profile.areas.length ? (
          <ul className={styles['profile-summary__tags']}>
            {areaNames(profile.areas, cities).map((name) => (
              <li key={name}>
                <Tag>{name}</Tag>
              </li>
            ))}
          </ul>
        ) : (
          <p className={styles['profile-summary__muted']}>{t('partner.none')}</p>
        ),
      )}
      {section('about', t('partner.steps.about'), <p className={styles['profile-summary__about']}>{profile.about || t('partner.none')}</p>)}
      {section(
        'work',
        t('partner.steps.work'),
        <p className={styles['profile-summary__muted']}>
          {t('partner.mediaSummary', { work: profile.workExamples.length, documents: profile.documents.length })}
        </p>,
      )}
    </div>
  )
}
