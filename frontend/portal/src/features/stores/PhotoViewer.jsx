import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import Icon from '@/components/ui/Icon/Icon'
import Modal from '@/components/ui/Modal/Modal'
import styles from './MaterialPrices.module.scss'

/**
 * A product's photo as a small picture that opens large in a dialog. Without a photo it is a plain placeholder
 * (nothing to open).
 */
export default function PhotoViewer({ product }) {
  const { t } = useTranslation()
  const [open, setOpen] = useState(false)

  if (!product.photoUrl) {
    return (
      <span className={styles['offer__photo']} aria-hidden="true">
        <Icon name="image" size={28} />
      </span>
    )
  }

  const caption = [product.brand, product.packLabel].filter(Boolean).join(' · ')
  return (
    <>
      <button type="button" className={styles['offer__photo-button']} onClick={() => setOpen(true)} aria-label={t('materialPrices.photo.open', { name: product.name })}>
        <img className={styles['offer__photo']} src={product.photoUrl} alt="" loading="lazy" width="72" height="72" />
      </button>
      <Modal open={open} title={product.name} onClose={() => setOpen(false)}>
        <figure className={styles['offer__figure']}>
          <img className={styles['offer__large']} src={product.photoLargeUrl ?? product.photoUrl} alt={product.name} />
          {caption && <figcaption className={styles['material-prices__muted']}>{caption}</figcaption>}
        </figure>
      </Modal>
    </>
  )
}
