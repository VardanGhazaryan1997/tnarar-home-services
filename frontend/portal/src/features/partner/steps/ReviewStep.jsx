import { useTranslation } from 'react-i18next'
import Alert from '@/components/ui/Alert/Alert'
import Icon from '@/components/ui/Icon/Icon'
import ProfileSummary from '../ProfileSummary'
import { STEP_OF_MISSING } from '../profileForm'
import styles from '../partner.module.scss'

/** Step 6: everything at a glance, what still blocks sending it for review, and the send button below. */
export default function ReviewStep({ profile, categories, cities, regions = [], goTo }) {
  const { t } = useTranslation()
  const approved = profile.status === 'Approved'
  return (
    <div className={styles['partner-wizard__fields']}>
      {!approved && profile.missingForSubmit.length > 0 && (
        <Alert tone="warning" title={t('partner.missingTitle')}>
          <ul className={styles['partner-wizard__missing']}>
            {profile.missingForSubmit.map((item) => (
              <li key={item}>
                <button type="button" className={styles['partner-wizard__missing-link']} onClick={() => goTo(STEP_OF_MISSING[item] ?? 'type')}>
                  {t(`partner.missing.${item}`, { defaultValue: item })}
                  <Icon name="chevronRight" size={16} />
                </button>
              </li>
            ))}
          </ul>
        </Alert>
      )}
      {!approved && profile.canSubmit && <p className={styles['partner-wizard__intro']}>{t('partner.readyText')}</p>}
      {approved && <p className={styles['partner-wizard__intro']}>{t('partner.liveEditText')}</p>}
      <ProfileSummary profile={profile} categories={categories} cities={cities} regions={regions} onEdit={goTo} />
    </div>
  )
}
