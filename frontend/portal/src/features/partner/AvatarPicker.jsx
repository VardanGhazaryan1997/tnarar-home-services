import { useEffect, useRef } from 'react'
import { useTranslation } from 'react-i18next'
import Avatar from '@/components/Avatar/Avatar'
import Button from '@/components/ui/Button/Button'
import Icon from '@/components/ui/Icon/Icon'
import { useFileUploads } from '@/features/files/useFileUploads'
import { IMAGE_TYPES } from './profileForm'
import styles from './partner.module.scss'

/** The profile photo or logo: shows the current one, uploads a new one, or removes it. Reports `onChange(fileId, previewUrl)`. */
export default function AvatarPicker({ name, url, onChange }) {
  const { t } = useTranslation()
  const inputRef = useRef(null)
  const uploads = useFileUploads({ max: 1, types: IMAGE_TYPES })
  const item = uploads.items[0]
  const onChangeRef = useRef(onChange)
  useEffect(() => {
    onChangeRef.current = onChange
  })

  useEffect(() => {
    if (item?.status === 'done') {
      onChangeRef.current(item.fileId, item.previewUrl)
      uploads.clear()
    }
  }, [item, uploads])

  return (
    <div className={styles['avatar-picker']}>
      <Avatar name={name} src={url} size="xl" />
      <div className={styles['avatar-picker__text']}>
        <span className={styles['avatar-picker__label']}>{t('partner.fields.avatar')}</span>
        <span className={styles['avatar-picker__hint']}>{t('partner.avatarHint')}</span>
        {item?.status === 'error' && (
          <span className={styles['avatar-picker__error']} role="alert">
            {t(`files.${item.error}`)}
          </span>
        )}
        <div className={styles['avatar-picker__actions']}>
          <Button
            variant="secondary"
            size="sm"
            icon={<Icon name="camera" />}
            loading={item?.status === 'uploading'}
            onClick={() => {
              uploads.clear()
              inputRef.current.click()
            }}
          >
            {url ? t('partner.avatarChange') : t('partner.avatarAdd')}
          </Button>
          {url && (
            <Button variant="ghost" size="sm" icon={<Icon name="trash" />} onClick={() => onChange(null, null)}>
              {t('partner.avatarRemove')}
            </Button>
          )}
        </div>
      </div>
      <input
        ref={inputRef}
        type="file"
        tabIndex={-1}
        aria-label={t('partner.fields.avatar')}
        accept={Object.keys(IMAGE_TYPES).join(',')}
        className={styles['avatar-picker__input']}
        onChange={(event) => {
          uploads.add(event.target.files)
          event.target.value = ''
        }}
      />
    </div>
  )
}
