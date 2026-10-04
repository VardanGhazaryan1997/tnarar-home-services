import { useId, useRef, useState } from 'react'
import { useTranslation } from 'react-i18next'
import Icon from '@/components/ui/Icon/Icon'
import Spinner from '@/components/ui/Spinner/Spinner'
import { bem } from '@/shared/bem'
import { MEDIA_TYPES } from './useFileUploads'
import styles from './PhotoPicker.module.scss'

const b = bem(styles)

/**
 * Photos and videos for a request: an "add" tile (opens the camera or gallery on phones) and a grid of
 * what was added, each uploading on its own. `uploads` comes from useFileUploads().
 */
export default function PhotoPicker({ uploads, label, hint, types = MEDIA_TYPES }) {
  const { t } = useTranslation()
  const inputId = useId()
  const inputRef = useRef(null)
  const [skipped, setSkipped] = useState(0)

  const onChange = (event) => {
    setSkipped(uploads.add(event.target.files))
    event.target.value = ''
  }

  return (
    <div className={styles['photo-picker']}>
      <label htmlFor={inputId} className={styles['photo-picker__label']}>
        {label}
      </label>
      {hint && <p className={styles['photo-picker__hint']}>{hint}</p>}
      <ul className={styles['photo-picker__grid']}>
        {uploads.items.map((item) => (
          <li key={item.key} className={b('photo-picker__item', { error: item.status === 'error' })}>
            {item.previewUrl ? (
              <img src={item.previewUrl} alt={item.name} className={styles['photo-picker__preview']} />
            ) : (
              <span className={styles['photo-picker__placeholder']}>
                <Icon name="camera" size={24} />
                <span className={styles['photo-picker__name']}>{item.name}</span>
              </span>
            )}
            {item.status === 'uploading' && (
              <span className={styles['photo-picker__overlay']}>
                <Spinner label={t('files.uploading', { name: item.name })} />
              </span>
            )}
            {item.status === 'error' && (
              <span className={styles['photo-picker__error']} role="alert">
                {t(`files.${item.error}`)}
              </span>
            )}
            <button
              type="button"
              className={styles['photo-picker__remove']}
              onClick={() => uploads.remove(item.key)}
              aria-label={t('files.remove', { name: item.name })}
            >
              <Icon name="close" size={16} />
            </button>
          </li>
        ))}
        {!uploads.full && (
          <li>
            <button type="button" className={styles['photo-picker__add']} onClick={() => inputRef.current.click()}>
              <Icon name="plus" size={24} />
              <span>{t('files.add')}</span>
            </button>
          </li>
        )}
      </ul>
      {skipped > 0 && <p className={styles['photo-picker__hint']}>{t('files.skipped', { n: skipped })}</p>}
      <input
        ref={inputRef}
        id={inputId}
        type="file"
        tabIndex={-1}
        multiple
        accept={Object.keys(types).join(',')}
        className={styles['photo-picker__input']}
        onChange={onChange}
      />
    </div>
  )
}
