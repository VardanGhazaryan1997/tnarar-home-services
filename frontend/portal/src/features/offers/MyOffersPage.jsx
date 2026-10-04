import { useTranslation } from 'react-i18next'
import { useSearchParams } from 'react-router'
import PageHeader from '@/components/PageHeader/PageHeader'
import QueryState from '@/components/QueryState/QueryState'
import EmptyState from '@/components/ui/EmptyState/EmptyState'
import Pagination from '@/components/ui/Pagination/Pagination'
import Segmented from '@/components/ui/Segmented/Segmented'
import Tag from '@/components/ui/Tag/Tag'
import { RequestCard, RequestList } from '@/features/requests/components/RequestParts'
import { placeText } from '@/features/requests/place'
import RequestsTabs from '@/features/requests/RequestsTabs'
import { useLocalizedPath } from '@/i18n/hooks'
import { formatDate, formatMoney } from '@/shared/format'
import { OfferStatusTag } from './OfferCard'
import { useGetMyOffersQuery } from './offersApi'
import styles from '@/features/requests/requests.module.scss'

const STATUSES = ['', 'Sent', 'Accepted', 'Rejected', 'Expired']

/** Offers the partner sent, newest first, by status. Each opens its request. */
export default function MyOffersPage() {
  const { t, i18n } = useTranslation()
  const path = useLocalizedPath()
  const [params, setParams] = useSearchParams()
  const status = params.get('status') ?? ''
  const page = Number(params.get('page') ?? 1)
  const query = useGetMyOffersQuery({ status, page }, { refetchOnMountOrArgChange: true })

  const setFilter = (changes) => {
    const next = new URLSearchParams(params)
    Object.entries(changes).forEach(([name, value]) => (value ? next.set(name, value) : next.delete(name)))
    setParams(next, { replace: true })
  }

  return (
    <div className={styles['requests-page']}>
      <PageHeader title={t('offers.mineTitle')} subtitle={t('offers.mineSubtitle')} />
      <RequestsTabs />
      <Segmented
        label={t('requests.filterLabel')}
        options={STATUSES.map((value) => ({ value, label: t(value ? `offers.status.${value}` : 'requests.all') }))}
        value={status}
        onChange={(value) => setFilter({ status: value, page: null })}
      />
      <QueryState query={query}>
        {(data) =>
          data.items.length === 0 ? (
            <EmptyState icon="send" title={t('offers.mineEmpty')} description={t('offers.mineEmptyText')} />
          ) : (
            <>
              <RequestList>
                {data.items.map((offer) => (
                  <RequestCard
                    key={offer.id}
                    to={offer.orderId ? path(`/orders/${offer.orderId}`) : path(`/inbox/${offer.requestId}`)}
                    title={placeText(offer.place, t)}
                    excerpt={offer.requestExcerpt}
                    highlight={offer.status === 'Accepted'}
                    tags={
                      <>
                        <Tag tone={offer.kind === 'Visit' ? 'warning' : 'neutral'}>{t(`offers.kind.${offer.kind}`)}</Tag>
                        <OfferStatusTag status={offer.status} />
                      </>
                    }
                    meta={[
                      offer.kind === 'Visit' && offer.price === 0 ? t('offers.free') : formatMoney(offer.price, i18n.language),
                      t('offers.sentOn', { date: formatDate(offer.sentAt, i18n.language) }),
                    ].join(' · ')}
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
