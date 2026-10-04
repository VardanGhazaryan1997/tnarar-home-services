import { useTranslation } from 'react-i18next'
import { useSearchParams } from 'react-router'
import PageHeader from '@/components/PageHeader/PageHeader'
import QueryState from '@/components/QueryState/QueryState'
import EmptyState from '@/components/ui/EmptyState/EmptyState'
import Pagination from '@/components/ui/Pagination/Pagination'
import Segmented from '@/components/ui/Segmented/Segmented'
import Tag from '@/components/ui/Tag/Tag'
import DebtPauseNotice from '@/features/commissions/DebtPauseNotice'
import { useLocalizedPath } from '@/i18n/hooks'
import { formatDate } from '@/shared/format'
import { RecipientStatusTag, RequestCard, RequestList } from './components/RequestParts'
import { placeText } from './place'
import { useGetInboxQuery } from './requestsApi'
import RequestsTabs from './RequestsTabs'
import styles from './requests.module.scss'

const STATUSES = ['', 'New', 'Viewed', 'Responded', 'Declined']

/** Requests the partner received, newest first, filtered by what they did with them. */
export default function InboxPage() {
  const { t, i18n } = useTranslation()
  const path = useLocalizedPath()
  const [params, setParams] = useSearchParams()
  const status = params.get('status') ?? ''
  const page = Number(params.get('page') ?? 1)
  const query = useGetInboxQuery({ status, page }, { refetchOnMountOrArgChange: true })

  const setFilter = (changes) => {
    const next = new URLSearchParams(params)
    Object.entries(changes).forEach(([key, value]) => (value ? next.set(key, value) : next.delete(key)))
    setParams(next, { replace: true })
  }

  return (
    <div className={styles['requests-page']}>
      <PageHeader title={t('inbox.title')} subtitle={t('inbox.subtitle')} />
      <RequestsTabs />
      <DebtPauseNotice />
      <Segmented
        label={t('requests.filterLabel')}
        options={STATUSES.map((value) => ({ value, label: t(value ? `inbox.status.${value}` : 'requests.all') }))}
        value={status}
        onChange={(value) => setFilter({ status: value, page: null })}
      />
      <QueryState query={query}>
        {(data) =>
          data.items.length === 0 ? (
            <EmptyState icon="inbox" title={t('inbox.emptyTitle')} description={t('inbox.emptyText')} />
          ) : (
            <>
              <RequestList>
                {data.items.map((item) => (
                  <RequestCard
                    key={item.id}
                    to={path(`/inbox/${item.id}`)}
                    title={placeText(item.place, t)}
                    excerpt={item.excerpt}
                    highlight={item.myStatus === 'New'}
                    tags={
                      <>
                        {item.kind === 'Direct' && <Tag tone="info">{t('inbox.direct')}</Tag>}
                        {item.requestStatus !== 'Open' && <Tag>{t(`requests.status.${item.requestStatus}`)}</Tag>}
                        <RecipientStatusTag status={item.myStatus} />
                      </>
                    }
                    meta={[
                      formatDate(item.sentAt, i18n.language),
                      item.preferredDate && t('inbox.preferred', { date: formatDate(item.preferredDate, i18n.language) }),
                      item.mediaCount > 0 && t('inbox.photos', { n: item.mediaCount }),
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
