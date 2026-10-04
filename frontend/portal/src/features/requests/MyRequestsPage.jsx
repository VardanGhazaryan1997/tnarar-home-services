import { useTranslation } from 'react-i18next'
import { useSearchParams } from 'react-router'
import PageHeader from '@/components/PageHeader/PageHeader'
import QueryState from '@/components/QueryState/QueryState'
import Button from '@/components/ui/Button/Button'
import EmptyState from '@/components/ui/EmptyState/EmptyState'
import Icon from '@/components/ui/Icon/Icon'
import Pagination from '@/components/ui/Pagination/Pagination'
import Segmented from '@/components/ui/Segmented/Segmented'
import Tag from '@/components/ui/Tag/Tag'
import { useLocalizedPath } from '@/i18n/hooks'
import { formatDate } from '@/shared/format'
import { RequestCard, RequestList, RequestStatusTag } from './components/RequestParts'
import { placeText } from './place'
import { useGetMyRequestsQuery } from './requestsApi'
import RequestsTabs from './RequestsTabs'
import styles from './requests.module.scss'

const STATUSES = ['', 'Open', 'Closed', 'Cancelled']

/** The customer's requests, newest first, filtered by status (kept in the URL). */
export default function MyRequestsPage() {
  const { t, i18n } = useTranslation()
  const path = useLocalizedPath()
  const [params, setParams] = useSearchParams()
  const status = params.get('status') ?? ''
  const page = Number(params.get('page') ?? 1)
  const query = useGetMyRequestsQuery({ status, page }, { refetchOnMountOrArgChange: true })

  const setFilter = (changes) => {
    const next = new URLSearchParams(params)
    Object.entries(changes).forEach(([key, value]) => (value ? next.set(key, value) : next.delete(key)))
    setParams(next, { replace: true })
  }

  const newButton = (
    <Button to={path('/requests/new')} variant="accent" icon={<Icon name="plus" />}>
      {t('requests.new')}
    </Button>
  )

  return (
    <div className={styles['requests-page']}>
      <PageHeader title={t('requests.mineTitle')} subtitle={t('requests.mineSubtitle')} actions={newButton} />
      <RequestsTabs />
      <Segmented
        label={t('requests.filterLabel')}
        options={STATUSES.map((value) => ({ value, label: t(value ? `requests.status.${value}` : 'requests.all') }))}
        value={status}
        onChange={(value) => setFilter({ status: value, page: null })}
      />
      <QueryState query={query}>
        {(data) =>
          data.items.length === 0 ? (
            <EmptyState icon="requests" title={t('requests.emptyTitle')} description={t('requests.emptyText')} action={!status && newButton} />
          ) : (
            <>
              <RequestList>
                {data.items.map((item) => (
                  <RequestCard
                    key={item.id}
                    to={path(`/requests/${item.id}`)}
                    title={placeText(item.place, t)}
                    excerpt={item.excerpt}
                    highlight={item.status === 'Open' && item.responded > 0}
                    tags={
                      <>
                        {item.kind === 'Direct' && <Tag tone="info">{t('requests.kind.Direct')}</Tag>}
                        <RequestStatusTag status={item.status} />
                      </>
                    }
                    meta={[
                      formatDate(item.createdAt, i18n.language),
                      item.findingPartners
                        ? t('requests.findingPartners')
                        : t('requests.sentSummary', { sent: item.sentTo, responded: item.responded }),
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
