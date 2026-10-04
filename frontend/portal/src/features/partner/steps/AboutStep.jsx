import { useTranslation } from 'react-i18next'
import { TextField } from '@/components/ui/Field/Field'
import AvatarPicker from '../AvatarPicker'
import { LIMITS } from '../profileForm'
import styles from '../partner.module.scss'

/** Step 4: a photo or logo and a few words about the work, experience and way of working. */
export default function AboutStep({ form, set, errorFor }) {
  const { t } = useTranslation()
  const length = form.about.trim().length
  return (
    <div className={styles['partner-wizard__fields']}>
      <AvatarPicker
        name={form.displayName}
        url={form.avatarUrl}
        onChange={(fileId, url) => {
          set('avatarFileId', fileId)
          set('avatarUrl', url)
        }}
      />
      <TextField
        label={t('partner.fields.about')}
        hint={
          length < LIMITS.aboutMin
            ? t('partner.aboutHintShort', { n: length, min: LIMITS.aboutMin })
            : t('partner.aboutHint', { n: length, max: LIMITS.aboutMax })
        }
        placeholder={t('partner.aboutPlaceholder')}
        multiline
        rows={8}
        required
        maxLength={LIMITS.aboutMax}
        value={form.about}
        onChange={(event) => set('about', event.target.value)}
        error={errorFor('about')}
      />
    </div>
  )
}
