import { useTranslation } from 'react-i18next'
import { useSelector } from 'react-redux'
import { useSearchParams } from 'react-router'
import PageHeader from '@/components/PageHeader/PageHeader'
import QueryState from '@/components/QueryState/QueryState'
import EmptyState from '@/components/ui/EmptyState/EmptyState'
import Pagination from '@/components/ui/Pagination/Pagination'
import Segmented from '@/components/ui/Segmented/Segmented'
import Tag from '@/components/ui/Tag/Tag'
import { selectIsPartner } from '@/features/auth/authSlice'
import { RequestCard, RequestList } from '@/features/requests/components/RequestParts'
import { placeText } from '@/features/requests/place'
import { useLocalizedPath } from '@/i18n/hooks'
import { formatDate, formatMoney } from '@/shared/format'
import { OrderStatusTag } from './OrderParts'
import { useGetOrdersQuery } from './ordersApi'
import styles from '@/features/requests/requests.module.scss'

/** The user's orders, newest first; partners can show only one side. */
export default function OrdersPage() {
  const { t, i18n } = useTranslation()
  const path = useLocalizedPath()
  const isPartner = useSelector(selectIsPartner)
  const [params, setParams] = useSearchParams()
  const as = params.get('as') ?? ''
  const page = Number(params.get('page') ?? 1)
  const query = useGetOrdersQuery({ as, page }, { refetchOnMountOrArgChange: true })

  const setFilter = (changes) => {
    const next = new URLSearchParams(params)
    Object.entries(changes).forEach(([name, value]) => (value ? next.set(name, value) : next.delete(name)))
    setParams(next, { replace: true })
  }

  return (
    <div className={styles['requests-page']}>
      <PageHeader title={t('orders.title')} subtitle={t('orders.subtitle')} />
      {isPartner && (
        <Segmented
          label={t('orders.sideLabel')}
          options={['', 'Customer', 'Partner'].map((value) => ({ value, label: t(value ? `orders.as.${value}` : 'requests.all') }))}
          value={as}
          onChange={(value) => setFilter({ as: value, page: null })}
        />
      )}
      <QueryState query={query}>
        {(data) =>
          data.items.length === 0 ? (
            <EmptyState icon="orders" title={t('orders.emptyTitle')} description={t('orders.emptyText')} />
          ) : (
            <>
              <RequestList>
                {data.items.map((order) => (
                  <RequestCard
                    key={order.id}
                    to={path(`/orders/${order.id}`)}
                    title={placeText(order.place, t)}
                    excerpt={order.summary}
                    tags={
                      <>
                        {order.kind === 'Visit' && <Tag tone="warning">{t('offers.kind.Visit')}</Tag>}
                        {isPartner && <Tag>{t(`orders.as.${order.myRole}`)}</Tag>}
                        <OrderStatusTag status={order.status} />
                      </>
                    }
                    meta={[
                      order.otherParty,
                      order.kind === 'Visit' && order.price === 0 ? t('offers.free') : formatMoney(order.price, i18n.language),
                      formatDate(order.visitAt ?? order.startDate ?? order.createdAt, i18n.language),
                    ]
                      .filter(Boolean)
                      .join(' · ')}
                  />
                ))}
              </RequestList>
              <Pagination page={data.page} pageSize={data.pageSize} totalCount={data.totalCount} onChange={(next) => setFilter({ page: String(next) })} />
            </>
          )
        }
      </QueryState>
    </div>
  )
}
