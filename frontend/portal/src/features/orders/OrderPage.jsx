import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { useLocation, useParams } from 'react-router'
import PageHeader from '@/components/PageHeader/PageHeader'
import QueryState from '@/components/QueryState/QueryState'
import Alert from '@/components/ui/Alert/Alert'
import Button from '@/components/ui/Button/Button'
import Card from '@/components/ui/Card/Card'
import EmptyState from '@/components/ui/EmptyState/EmptyState'
import Icon from '@/components/ui/Icon/Icon'
import Tag from '@/components/ui/Tag/Tag'
import StartChatButton from '@/features/chat/StartChatButton'
import { OfferTerms } from '@/features/offers/OfferCard'
import { placeText } from '@/features/requests/place'
import { useLocalizedPath } from '@/i18n/hooks'
import { formatMoney } from '@/shared/format'
import ChangeModal from './ChangeModal'
import { PastChanges, PendingChange } from './ChangeParts'
import { OrderHistory, OrderStatusTag } from './OrderParts'
import OrderStatusPanel from './OrderStatusPanel'
import PaymentsCard from './PaymentsCard'
import { useCancelOrderMutation, useGetOrderQuery } from './ordersApi'
import ReasonModal from './ReasonModal'
import ReviewCard from './ReviewCard'
import styles from './orders.module.scss'

/**
 * An order as either party sees it: where it stands and the viewer's next step, a change waiting for an
 * answer, the terms as they are now (accepted changes included), the payment plan, both parties' contacts
 * and the history. Proposing a change and cancelling sit in the side column.
 */
export default function OrderPage() {
  const { t, i18n } = useTranslation()
  const { id } = useParams()
  const location = useLocation()
  const path = useLocalizedPath()
  const query = useGetOrderQuery(id, { refetchOnMountOrArgChange: true })
  const [cancel] = useCancelOrderMutation()
  const [changeOpen, setChangeOpen] = useState(false)
  const [cancelOpen, setCancelOpen] = useState(false)
  const back = { to: path('/orders'), label: t('orders.title') }

  return (
    <QueryState
      query={query}
      notFound={
        <>
          <PageHeader title={t('orders.notFound')} back={back} />
          <EmptyState icon="orders" title={t('orders.notFound')} description={t('orders.notFoundText')} />
        </>
      }
    >
      {(order) => {
        const isCustomer = order.myRole === 'Customer'
        const other = isCustomer
          ? {
              title: t('orders.yourSpecialist'),
              name: order.partner.displayName,
              phone: order.partner.phone,
            }
          : {
              title: t('orders.yourCustomer'),
              name: order.customer.fullName ?? t('inbox.customerNoName'),
              phone: order.customer.phone,
            }
        const pending = order.changeRequests.find((change) => change.status === 'Pending')
        const past = order.changeRequests.filter((change) => change.status !== 'Pending')
        const free = order.kind === 'Visit' && order.price === 0
        const current = {
          ...order.terms,
          startDate: order.startDate,
          durationDays: order.durationDays,
          visitAt: order.visitAt,
          stages: [],
        }
        const canChange = order.actions.includes('proposeChange')
        const canCancel = order.actions.includes('cancel')

        return (
          <>
            <title>{t('orders.documentTitle', { place: placeText(order.place, t) })}</title>
            <PageHeader
              title={order.kind === 'Visit' ? t('orders.visitTitle') : t('orders.workTitle')}
              subtitle={placeText(order.place, t)}
              back={back}
              actions={
                <>
                  {order.kind === 'Visit' && <Tag tone="warning">{t('offers.kind.Visit')}</Tag>}
                  <OrderStatusTag status={order.status} />
                </>
              }
            />
            <div className={styles['order-page']}>
              <div className={styles['order-page__main']}>
                {location.state?.created && <Alert tone="success" title={order.kind === 'Visit' ? t('orders.createdVisit') : t('orders.created')} />}
                <OrderStatusPanel order={order} />
                {pending && <PendingChange order={order} change={pending} />}
                <Card
                  title={t('orders.terms')}
                  actions={<span className={styles['order-page__price']}>{free ? t('offers.free') : formatMoney(order.price, i18n.language)}</span>}
                >
                  {order.price !== order.terms.price && (
                    <p className={styles['order-page__note']}>
                      <Icon name="info" size={16} />
                      {t('orders.priceChanged', {
                        agreed: formatMoney(order.terms.price, i18n.language),
                      })}
                    </p>
                  )}
                  <OfferTerms terms={current} kind={order.kind} />
                </Card>
                <ReviewCard order={order} />
                <PaymentsCard order={order} />
                {past.length > 0 && (
                  <Card title={t('orders.changes')}>
                    <PastChanges changes={past} />
                  </Card>
                )}
              </div>

              <aside className={styles['order-page__side']}>
                <Card title={other.title}>
                  <p className={styles['order-page__name']}>{other.name}</p>
                  <a className={styles['order-page__phone']} href={`tel:${other.phone}`}>
                    {other.phone}
                  </a>
                  <StartChatButton requestId={order.requestId} partnerId={isCustomer ? order.partner.partnerId : undefined} block />
                </Card>
                {(canChange || canCancel) && (
                  <Card title={t('orders.manage')}>
                    <div className={styles['order-page__manage']}>
                      {canChange && (
                        <Button variant="secondary" block icon={<Icon name="edit" />} onClick={() => setChangeOpen(true)}>
                          {t('orders.actions.proposeChange')}
                        </Button>
                      )}
                      {canChange || !pending ? null : <p className={styles['order-page__hint']}>{t('orders.change.oneAtATime')}</p>}
                      {canCancel && (
                        <Button variant="ghost" block icon={<Icon name="close" />} className={styles['order-page__cancel']} onClick={() => setCancelOpen(true)}>
                          {t('orders.actions.cancel')}
                        </Button>
                      )}
                    </div>
                  </Card>
                )}
                <Card title={t('orders.history')}>
                  <OrderHistory history={order.history} />
                </Card>
              </aside>
            </div>
            {canChange && <ChangeModal order={order} open={changeOpen} onClose={() => setChangeOpen(false)} />}
            <ReasonModal
              open={cancelOpen}
              title={t('orders.cancel.title')}
              text={order.status === 'Confirmed' ? t('orders.cancel.text') : t('orders.cancel.textStarted')}
              label={t('orders.cancel.label')}
              confirmLabel={t('orders.cancel.confirm')}
              onSubmit={(reason) => cancel({ id: order.id, reason })}
              onClose={() => setCancelOpen(false)}
            />
          </>
        )
      }}
    </QueryState>
  )
}
