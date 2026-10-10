import { useTranslation } from 'react-i18next'
import Button from '@/components/ui/Button/Button'
import Modal from '@/components/ui/Modal/Modal'

/** Asks "are you sure?" before something that can't be undone. */
export default function ConfirmDialog({ open, title, text, confirmLabel, loading, onConfirm, onClose }) {
  const { t } = useTranslation()
  return (
    <Modal
      open={open}
      title={title}
      onClose={onClose}
      footer={
        <>
          <Button variant="ghost" onClick={onClose}>
            {t('common.cancel')}
          </Button>
          <Button variant="danger" loading={loading} onClick={onConfirm}>
            {confirmLabel}
          </Button>
        </>
      }
    >
      <p>{text}</p>
    </Modal>
  )
}
