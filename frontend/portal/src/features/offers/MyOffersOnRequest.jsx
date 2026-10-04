import { useTranslation } from 'react-i18next'
import { errorMessage } from '@/api/errors'
import Alert from '@/components/ui/Alert/Alert'
import Button from '@/components/ui/Button/Button'
import Card from '@/components/ui/Card/Card'
import { useLocalizedPath } from '@/i18n/hooks'
import OfferCard from './OfferCard'
import { useGetRequestOffersQuery, useWithdrawOfferMutation } from './offersApi'
import styles from './offers.module.scss'

/** On a received request: the partner's own offers, with withdrawing a waiting one. Nothing until they offer. */
export default function MyOffersOnRequest({ requestId }) {
  const { t } = useTranslation()
  const path = useLocalizedPath()
  const query = useGetRequestOffersQuery(requestId, { refetchOnMountOrArgChange: true })
  const [withdraw, withdrawing] = useWithdrawOfferMutation()
  const offers = query.data ?? []
  if (!offers.length) return null

  return (
    <Card title={t('offers.yours')}>
      {withdrawing.error && <Alert tone="danger" title={errorMessage(t, withdrawing.error)} />}
      <div className={styles['offer-list']}>
        {offers.map((offer) => (
          <OfferCard
            key={offer.id}
            offer={offer}
            showPartner={false}
            actions={
              (offer.status === 'Sent' && (
                <Button variant="secondary" loading={withdrawing.isLoading && withdrawing.originalArgs === offer.id} onClick={() => withdraw(offer.id)}>
                  {t('offers.withdraw')}
                </Button>
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
    </Card>
  )
}
