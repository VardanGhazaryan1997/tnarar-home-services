import { useTranslation } from 'react-i18next'
import Alert from '@/components/ui/Alert/Alert'
import { MEDIA_TYPES } from '@/features/files/useFileUploads'
import MediaManager from '../MediaManager'
import { DOCUMENT_TYPES, LIMITS } from '../profileForm'
import styles from '../partner.module.scss'

/** Step 5: photos and videos of finished work (public), and documents only the Tnarar team sees. */
export default function MediaStep({ profile, showErrors }) {
  const { t } = useTranslation()
  return (
    <div className={styles['partner-wizard__fields']}>
      <MediaManager
        kind="WorkExample"
        items={profile.workExamples}
        max={LIMITS.workExamples}
        types={MEDIA_TYPES}
        title={t('partner.fields.work')}
        hint={t('partner.workHint')}
      />
      {showErrors && profile.workExamples.length === 0 && <Alert tone="danger" title={t('partner.errors.work')} />}
      <MediaManager
        kind="Document"
        items={profile.documents}
        max={LIMITS.documents}
        types={DOCUMENT_TYPES}
        title={t('partner.fields.documents')}
        hint={t('partner.documentsHint')}
      />
    </div>
  )
}
