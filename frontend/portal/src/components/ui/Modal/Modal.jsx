import { useEffect, useId, useRef } from 'react'
import { createPortal } from 'react-dom'
import { useTranslation } from 'react-i18next'
import Icon from '../Icon/Icon'
import styles from './Modal.module.scss'

/**
 * A dialog over the page (a bottom sheet on phones). Closes with Escape, the close button or a click
 * outside. Focus moves into it when it opens and back to where it was when it closes.
 */
export default function Modal({ open, title, onClose, footer, children }) {
  const { t } = useTranslation()
  const titleId = useId()
  const panelRef = useRef(null)
  // The latest onClose, so a parent re-render (typing in a field inside) doesn't re-run the focus effect.
  const onCloseRef = useRef(onClose)
  useEffect(() => {
    onCloseRef.current = onClose
  })

  useEffect(() => {
    if (!open) return undefined
    const previous = document.activeElement
    panelRef.current?.focus()
    const onKey = (event) => {
      if (event.key === 'Escape') onCloseRef.current()
    }
    document.addEventListener('keydown', onKey)
    document.body.style.overflow = 'hidden'
    return () => {
      document.removeEventListener('keydown', onKey)
      document.body.style.overflow = ''
      previous?.focus?.()
    }
  }, [open])

  if (!open) return null

  return createPortal(
    <div className={styles.modal} onMouseDown={(event) => event.target === event.currentTarget && onClose()}>
      <div ref={panelRef} className={styles['modal__panel']} role="dialog" aria-modal="true" aria-labelledby={titleId} tabIndex={-1}>
        <header className={styles['modal__header']}>
          <h2 id={titleId} className={styles['modal__title']}>
            {title}
          </h2>
          <button type="button" className={styles['modal__close']} onClick={onClose} aria-label={t('common.close')}>
            <Icon name="close" />
          </button>
        </header>
        <div className={styles['modal__body']}>{children}</div>
        {footer && <footer className={styles['modal__footer']}>{footer}</footer>}
      </div>
    </div>,
    document.body,
  )
}
