import { useEffect, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { useLocation, useNavigate } from 'react-router'
import { errorMessage } from '@/api/errors'
import Alert from '@/components/ui/Alert/Alert'
import Button from '@/components/ui/Button/Button'
import Card from '@/components/ui/Card/Card'
import Spinner from '@/components/ui/Spinner/Spinner'
import StartChatButton from '@/features/chat/StartChatButton'
import { useLocalizedPath } from '@/i18n/hooks'
import OfferCard from './OfferCard'
import OfferComparison from './OfferComparison.jsx'
import { AcceptOfferModal, RejectOfferModal } from './OfferDecisions'
import { useGetRequestOffersQuery } from './offersApi'
import styles from './offers.module.scss'

/**
 * The offers on the customer's request, for comparison: waiting ones first, cheapest first. Accepting
 * one creates the order (and, for work, closes the request); rejecting tells the partner, optionally why.
 * A link ending in #offer-<id> (from the chat) scrolls to that offer.
 */
export default function RequestOffers({ request }) {
  const { t } = useTranslation()
  const navigate = useNavigate()
  const location = useLocation()
  const path = useLocalizedPath()
  const query = useGetRequestOffersQuery(request.id, { refetchOnMountOrArgChange: true })
  const [accepting, setAccepting] = useState(null)
  const [rejecting, setRejecting] = useState(null)
  const offers = query.data ?? []
  const canDecide = request.status === 'Open'
  const loaded = query.isSuccess

  useEffect(() => {
    if (!loaded || !location.hash.startsWith('#offer-')) return
    document.getElementById(location.hash.slice(1))?.scrollIntoView?.({ block: 'start' })
  }, [loaded, location.hash])

  return (
    <Card title={offers.length ? t('offers.titleCount', { n: offers.length }) : t('offers.title')}>
      {query.isLoading && <Spinner label={t('common.loading')} />}
      {query.isError && <Alert tone="danger" title={errorMessage(t, query.error)} />}
      {query.isSuccess && offers.length === 0 && <p className={styles['offer-card__expiry']}>{t('offers.none')}</p>}
      {offers.length > 0 && request.lines?.length > 0 && <OfferComparison request={request} offers={offers} />}
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

      <AcceptOfferModal
        offer={accepting}
        partnerName={accepting?.partner.displayName}
        onClose={() => setAccepting(null)}
        onAccepted={(order) => navigate(path(`/orders/${order.id}`), { state: { created: true } })}
      />
      <RejectOfferModal offer={rejecting} onClose={() => setRejecting(null)} onRejected={() => setRejecting(null)} />
    </Card>
  )
}
