import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { useNavigate } from 'react-router'
import { errorMessage } from '@/api/errors'
import Alert from '@/components/ui/Alert/Alert'
import Button from '@/components/ui/Button/Button'
import Card from '@/components/ui/Card/Card'
import { TextField } from '@/components/ui/Field/Field'
import Modal from '@/components/ui/Modal/Modal'
import Spinner from '@/components/ui/Spinner/Spinner'
import StartChatButton from '@/features/chat/StartChatButton'
import { useLocalizedPath } from '@/i18n/hooks'
import { formatMoney } from '@/shared/format'
import OfferCard from './OfferCard'
import { useAcceptOfferMutation, useGetRequestOffersQuery, useRejectOfferMutation } from './offersApi'
import styles from './offers.module.scss'

/**
 * The offers on the customer's request, for comparison: waiting ones first, cheapest first. Accepting
 * one creates the order (and, for work, closes the request); rejecting tells the partner, optionally why.
 */
export default function RequestOffers({ request }) {
  const { t, i18n } = useTranslation()
  const navigate = useNavigate()
  const path = useLocalizedPath()
  const query = useGetRequestOffersQuery(request.id, { refetchOnMountOrArgChange: true })
  const [accepting, setAccepting] = useState(null)
  const [rejecting, setRejecting] = useState(null)
  const [reason, setReason] = useState('')
  const [accept, acceptState] = useAcceptOfferMutation()
  const [reject, rejectState] = useRejectOfferMutation()
  const offers = query.data ?? []
  const canDecide = request.status === 'Open'

  const confirmAccept = async () => {
    const result = await accept(accepting.id)
    if (result.data) navigate(path(`/orders/${result.data.id}`), { state: { created: true } })
  }

  const confirmReject = async () => {
    const result = await reject({ id: rejecting.id, reason: reason.trim() })
    if (result.data) {
      setRejecting(null)
      setReason('')
    }
  }

  return (
    <Card title={offers.length ? t('offers.titleCount', { n: offers.length }) : t('offers.title')}>
      {query.isLoading && <Spinner label={t('common.loading')} />}
      {query.isError && <Alert tone="danger" title={errorMessage(t, query.error)} />}
      {query.isSuccess && offers.length === 0 && <p className={styles['offer-card__expiry']}>{t('offers.none')}</p>}
      {offers.length > 0 && (
        <div className={styles['offer-list']}>
          {offers.map((offer) => (
            <OfferCard
              key={offer.id}
              offer={offer}
              actions={
                (offer.status === 'Sent' && canDecide && (
                  <>
                    <Button variant="accent" onClick={() => setAccepting(offer)}>
                      {t('offers.accept')}
                    </Button>
                    <Button variant="secondary" onClick={() => setRejecting(offer)}>
                      {t('offers.reject')}
                    </Button>
                    <StartChatButton requestId={request.id} partnerId={offer.partner.partnerId} variant="ghost" label={t('chat.ask')} />
                  </>
                )) ||
                (offer.orderId && (
                  <Button to={path(`/orders/${offer.orderId}`)} variant="secondary">
                    {t('offers.openOrder')}
                  </Button>
                ))
              }
            />
          ))}
        </div>
      )}

      <Modal
        open={Boolean(accepting)}
        title={t('offers.acceptTitle')}
        onClose={() => setAccepting(null)}
        footer={
          <>
            <Button variant="secondary" onClick={() => setAccepting(null)}>
              {t('common.back')}
            </Button>
            <Button variant="accent" loading={acceptState.isLoading} onClick={confirmAccept}>
              {t('offers.acceptConfirm')}
            </Button>
          </>
        }
      >
        {accepting && (
          <>
            <p>
              {t('offers.acceptText', {
                name: accepting.partner.displayName,
                price: accepting.kind === 'Visit' && accepting.price === 0 ? t('offers.free') : formatMoney(accepting.price, i18n.language),
              })}
            </p>
            <Alert tone="info" title={accepting.kind === 'Visit' ? t('offers.acceptVisitNote') : t('offers.acceptWorkNote')} />
            {acceptState.error && <Alert tone="danger" title={errorMessage(t, acceptState.error)} />}
          </>
        )}
      </Modal>

      <Modal
        open={Boolean(rejecting)}
        title={t('offers.rejectTitle')}
        onClose={() => setRejecting(null)}
        footer={
          <>
            <Button variant="secondary" onClick={() => setRejecting(null)}>
              {t('common.back')}
            </Button>
            <Button variant="danger" loading={rejectState.isLoading} onClick={confirmReject}>
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
        {rejectState.error && <Alert tone="danger" title={errorMessage(t, rejectState.error)} />}
      </Modal>
    </Card>
  )
}
