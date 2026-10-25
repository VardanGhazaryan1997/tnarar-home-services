import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { errorMessage } from '@/api/errors'
import Alert from '@/components/ui/Alert/Alert'
import Button from '@/components/ui/Button/Button'
import { TextField } from '@/components/ui/Field/Field'
import Modal from '@/components/ui/Modal/Modal'
import { formatMoney } from '@/shared/format'
import { useAcceptOfferMutation, useRejectOfferMutation, useWithdrawOfferMutation } from './offersApi'

/**
 * Confirms accepting an offer, which creates an order and can't be undone. `offer`: { id, kind, price } (null:
 * closed); `partnerName`: who sent it. `onAccepted(order)` runs once the order exists.
 */
export function AcceptOfferModal({ offer, partnerName, onClose, onAccepted }) {
  const { t, i18n } = useTranslation()
  const [accept, state] = useAcceptOfferMutation()

  const confirm = async () => {
    const result = await accept(offer.id)
    if (result.data) onAccepted(result.data)
  }

  return (
    <Modal
      open={Boolean(offer)}
      title={t('offers.acceptTitle')}
      onClose={onClose}
      footer={
        <>
          <Button variant="secondary" onClick={onClose}>
            {t('common.back')}
          </Button>
          <Button variant="accent" loading={state.isLoading} onClick={confirm}>
            {t('offers.acceptConfirm')}
          </Button>
        </>
      }
    >
      {offer && (
        <>
          <p>
            {t('offers.acceptText', {
              name: partnerName,
              price: offer.kind === 'Visit' && offer.price === 0 ? t('offers.free') : formatMoney(offer.price, i18n.language),
            })}
          </p>
          <Alert tone="info" title={offer.kind === 'Visit' ? t('offers.acceptVisitNote') : t('offers.acceptWorkNote')} />
          {state.error && <Alert tone="danger" title={errorMessage(t, state.error)} />}
        </>
      )}
    </Modal>
  )
}

/** Turns an offer down, optionally saying why (the partner sees it). `onRejected(offer)` runs once it's done. */
export function RejectOfferModal({ offer, onClose, onRejected }) {
  const { t } = useTranslation()
  const [reject, state] = useRejectOfferMutation()
  const [reason, setReason] = useState('')

  const close = () => {
    setReason('')
    onClose()
  }

  const confirm = async () => {
    const result = await reject({ id: offer.id, reason: reason.trim() })
    if (result.data) {
      setReason('')
      onRejected(result.data)
    }
  }

  return (
    <Modal
      open={Boolean(offer)}
      title={t('offers.rejectTitle')}
      onClose={close}
      footer={
        <>
          <Button variant="secondary" onClick={close}>
            {t('common.back')}
          </Button>
          <Button variant="danger" loading={state.isLoading} onClick={confirm}>
            {t('offers.rejectConfirm')}
          </Button>
        </>
      }
    >
      <TextField
        label={t('offers.rejectReason')}
        hint={t('offers.rejectReasonHint')}
        optionalText={t('common.optional')}
        multiline
        maxLength={500}
        value={reason}
        onChange={(event) => setReason(event.target.value)}
      />
      {state.error && <Alert tone="danger" title={errorMessage(t, state.error)} />}
    </Modal>
  )
}

/** The partner takes back their waiting offer (the customer can no longer accept it). `onWithdrawn(offer)` runs once it's done. */
export function WithdrawOfferModal({ offer, onClose, onWithdrawn }) {
  const { t } = useTranslation()
  const [withdraw, state] = useWithdrawOfferMutation()

  const confirm = async () => {
    const result = await withdraw(offer.id)
    if (result.data) onWithdrawn(result.data)
  }

  return (
    <Modal
      open={Boolean(offer)}
      title={t('offers.withdrawTitle')}
      onClose={onClose}
      footer={
        <>
          <Button variant="secondary" onClick={onClose}>
            {t('common.back')}
          </Button>
          <Button variant="danger" loading={state.isLoading} onClick={confirm}>
            {t('offers.withdrawConfirm')}
          </Button>
        </>
      }
    >
      <p>{t('offers.withdrawText')}</p>
      {state.error && <Alert tone="danger" title={errorMessage(t, state.error)} />}
    </Modal>
  )
}
