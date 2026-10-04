import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { errorMessage, fieldErrors } from '@/api/errors'
import Alert from '@/components/ui/Alert/Alert'
import Button from '@/components/ui/Button/Button'
import { TextField } from '@/components/ui/Field/Field'
import Modal from '@/components/ui/Modal/Modal'

const MAX = 1000

/**
 * Asks why before an action (cancel, "not finished yet", turning a change down). `required`: the reason can't
 * be empty. `onSubmit(reason)` returns the mutation result; the dialog closes when it succeeds.
 */
export default function ReasonModal({ open, title, text, label, confirmLabel, tone = 'danger', required = true, onSubmit, onClose }) {
  const { t } = useTranslation()
  const [reason, setReason] = useState('')
  const [error, setError] = useState(null)
  const [busy, setBusy] = useState(false)
  const [tried, setTried] = useState(false)
  const missing = required && !reason.trim()

  const close = () => {
    setReason('')
    setError(null)
    setTried(false)
    onClose()
  }

  const submit = async () => {
    setTried(true)
    if (missing) return
    setBusy(true)
    const result = await onSubmit(reason.trim() || null)
    setBusy(false)
    if (result.error) {
      setError(Object.values(fieldErrors(t, result.error))[0] ?? errorMessage(t, result.error))
      return
    }
    close()
  }

  return (
    <Modal
      open={open}
      title={title}
      onClose={close}
      footer={
        <>
          <Button variant="secondary" onClick={close}>
            {t('common.back')}
          </Button>
          <Button variant={tone} loading={busy} onClick={submit}>
            {confirmLabel}
          </Button>
        </>
      }
    >
      {text && <p>{text}</p>}
      <TextField
        label={label}
        optionalText={required ? undefined : t('common.optional')}
        required={required}
        multiline
        maxLength={MAX}
        value={reason}
        onChange={(event) => setReason(event.target.value)}
        error={tried && missing ? t('orders.reasonRequired') : undefined}
      />
      {error && <Alert tone="danger" title={error} />}
    </Modal>
  )
}
