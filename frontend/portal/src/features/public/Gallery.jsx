import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import Button from '@/components/ui/Button/Button'
import Icon from '@/components/ui/Icon/Icon'
import Modal from '@/components/ui/Modal/Modal'
import styles from './Gallery.module.scss'

/** Work examples as a grid of thumbnails; one opens large in a dialog, with previous / next. */
export default function Gallery({ items, label }) {
  const { t } = useTranslation()
  const [open, setOpen] = useState(null)
  const current = open === null ? null : items[open]
  const step = (delta) => setOpen((index) => (index + delta + items.length) % items.length)

  return (
    <>
      <ul className={styles.gallery} aria-label={label}>
        {items.map((item, index) => (
          <li key={item.url}>
            <button type="button" className={styles['gallery__thumb']} onClick={() => setOpen(index)} aria-label={t('public.profile.openWork', { n: index + 1, caption: item.caption ?? '' })}>
              {item.kind === 'Image' || item.thumbnailUrl ? (
                <img src={item.thumbnailUrl ?? item.url} alt="" loading="lazy" className={styles['gallery__image']} />
              ) : (
                <span className={styles['gallery__video']}>
                  <Icon name="camera" size={28} />
                </span>
              )}
              {item.kind === 'Video' && <span className={styles['gallery__badge']}>{t('public.profile.video')}</span>}
            </button>
          </li>
        ))}
      </ul>
      <Modal
        open={current !== null}
        title={t('public.profile.workOf', { n: (open ?? 0) + 1, total: items.length })}
        onClose={() => setOpen(null)}
        footer={
          items.length > 1 && (
            <div className={styles['gallery__nav']}>
              <Button variant="secondary" icon={<Icon name="chevronLeft" />} onClick={() => step(-1)}>
                {t('public.profile.previous')}
              </Button>
              <Button variant="secondary" onClick={() => step(1)}>
                {t('public.profile.next')}
              </Button>
            </div>
          )
        }
      >
        {current && (
          <figure className={styles['gallery__figure']}>
            {current.kind === 'Video' ? (
              <video src={current.url} controls className={styles['gallery__large']} />
            ) : (
              <img src={current.url} alt={current.caption ?? ''} className={styles['gallery__large']} />
            )}
            {current.caption && <figcaption className={styles['gallery__caption']}>{current.caption}</figcaption>}
          </figure>
        )}
      </Modal>
    </>
  )
}
