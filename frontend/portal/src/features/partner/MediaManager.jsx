import { useEffect, useId, useRef, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { errorMessage } from '@/api/errors'
import Alert from '@/components/ui/Alert/Alert'
import Icon from '@/components/ui/Icon/Icon'
import Spinner from '@/components/ui/Spinner/Spinner'
import { useFileUploads } from '@/features/files/useFileUploads'
import { bem } from '@/shared/bem'
import { useAddPartnerMediaMutation, useRemovePartnerMediaMutation } from './partnerProfileApi'
import styles from './partner.module.scss'

const b = bem(styles)

const preview = (file) => file.thumbnailUrl ?? (file.kind === 'Image' ? file.url : null)

/**
 * Work examples or documents on the profile: what's attached (each removable) and an add tile. A new file
 * uploads, then is attached to the profile right away; the list always shows what the API has.
 */
export default function MediaManager({ kind, items, max, types, title, hint }) {
  const { t } = useTranslation()
  const inputId = useId()
  const inputRef = useRef(null)
  const attaching = useRef(new Set())
  const uploads = useFileUploads({ max: Math.max(0, max - items.length), types })
  const [addMedia] = useAddPartnerMediaMutation()
  const [removeMedia] = useRemovePartnerMediaMutation()
  const [removing, setRemoving] = useState(null)
  const [error, setError] = useState(null)

  useEffect(() => {
    uploads.items
      .filter((item) => item.status === 'done' && !attaching.current.has(item.key))
      .forEach(async (item) => {
        attaching.current.add(item.key)
        const result = await addMedia({ kind, fileId: item.fileId, caption: null })
        if (result.error) setError(errorMessage(t, result.error))
        uploads.remove(item.key)
      })
  }, [uploads, addMedia, kind, t])

  const remove = async (media) => {
    setError(null)
    setRemoving(media.id)
    const result = await removeMedia(media.id)
    setRemoving(null)
    if (result.error) setError(errorMessage(t, result.error))
  }

  // Uploaded files stay in the list until they're attached, so everything here is still on its way.
  const pending = uploads.items
  const full = items.length + pending.length >= max

  return (
    <section className={styles['media-manager']} aria-labelledby={`${inputId}-title`}>
      <div>
        <h3 id={`${inputId}-title`} className={styles['media-manager__title']}>
          {title} <span className={styles['media-manager__count']}>{t('partner.selected', { n: items.length, max })}</span>
        </h3>
        <p className={styles['media-manager__hint']}>{hint}</p>
      </div>
      <ul className={styles['media-manager__grid']}>
        {items.map((media) => (
          <li key={media.id} className={styles['media-manager__item']}>
            {preview(media.file) ? (
              <img src={preview(media.file)} alt={media.file.fileName} className={styles['media-manager__preview']} />
            ) : (
              <span className={styles['media-manager__placeholder']}>
                <Icon name={media.file.kind === 'Video' ? 'camera' : 'file'} size={24} />
                <span className={styles['media-manager__name']}>{media.file.fileName}</span>
              </span>
            )}
            <button
              type="button"
              className={styles['media-manager__remove']}
              onClick={() => remove(media)}
              disabled={removing === media.id}
              aria-label={t('files.remove', { name: media.file.fileName })}
            >
              {removing === media.id ? <Spinner size="sm" /> : <Icon name="close" size={16} />}
            </button>
          </li>
        ))}
        {pending.map((item) => (
          <li key={item.key} className={b('media-manager__item', { error: item.status === 'error' })}>
            {item.previewUrl ? (
              <img src={item.previewUrl} alt={item.name} className={styles['media-manager__preview']} />
            ) : (
              <span className={styles['media-manager__placeholder']}>
                <Icon name="file" size={24} />
                <span className={styles['media-manager__name']}>{item.name}</span>
              </span>
            )}
            {item.status === 'error' ? (
              <>
                <span className={styles['media-manager__error']} role="alert">
                  {t(`files.${item.error}`)}
                </span>
                <button type="button" className={styles['media-manager__remove']} onClick={() => uploads.remove(item.key)} aria-label={t('files.remove', { name: item.name })}>
                  <Icon name="close" size={16} />
                </button>
              </>
            ) : (
              <span className={styles['media-manager__overlay']}>
                <Spinner label={t('files.uploading', { name: item.name })} />
              </span>
            )}
          </li>
        ))}
        {!full && (
          <li>
            <button type="button" className={styles['media-manager__add']} onClick={() => inputRef.current.click()}>
              <Icon name="plus" size={24} />
              <span>{t('files.add')}</span>
            </button>
          </li>
        )}
      </ul>
      {error && <Alert tone="danger" title={error} />}
      <input
        ref={inputRef}
        id={inputId}
        type="file"
        multiple
        tabIndex={-1}
        aria-label={title}
        accept={Object.keys(types).join(',')}
        className={styles['media-manager__input']}
        onChange={(event) => {
          setError(null)
          uploads.add(event.target.files)
          event.target.value = ''
        }}
      />
    </section>
  )
}
