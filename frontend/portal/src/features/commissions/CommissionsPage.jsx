import { useTranslation } from 'react-i18next'
import { useSearchParams } from 'react-router'
import PageHeader from '@/components/PageHeader/PageHeader'
import QueryState from '@/components/QueryState/QueryState'
import EmptyState from '@/components/ui/EmptyState/EmptyState'
import Pagination from '@/components/ui/Pagination/Pagination'
import Segmented from '@/components/ui/Segmented/Segmented'
import Tag from '@/components/ui/Tag/Tag'
import { RequestCard, RequestList } from '@/features/requests/components/RequestParts'
import RequestsTabs from '@/features/requests/RequestsTabs'
import { useLocalizedPath } from '@/i18n/hooks'
import { formatDate, formatMoney } from '@/shared/format'
import CommissionSummary from './CommissionSummary'
import { formatPercent, statementState, weekText } from './commissionParts'
import { useGetCommissionSummaryQuery, useGetMyCommissionsQuery, useGetMyStatementsQuery } from './commissionsApi'
import styles from './commissions.module.scss'

const VIEWS = ['statements', 'orders']

/**
 * What the partner owes TnaShen: totals, weekly statements (newest first) and the commission on each completed
 * order. Partners pay offline; the team records payments.
 */
export default function CommissionsPage() {
  const { t } = useTranslation()
  const [params, setParams] = useSearchParams()
  const view = VIEWS.includes(params.get('view')) ? params.get('view') : 'statements'
  const page = Number(params.get('page') ?? 1)
  const summary = useGetCommissionSummaryQuery(undefined, { refetchOnMountOrArgChange: true })

  const setFilter = (changes) => {
    const next = new URLSearchParams(params)
    Object.entries(changes).forEach(([name, value]) => (value ? next.set(name, value) : next.delete(name)))
    setParams(next, { replace: true })
  }

  return (
    <div className={styles['commissions-page']}>
      <PageHeader title={t('commissions.title')} subtitle={t('commissions.subtitle')} />
      <RequestsTabs />
      <QueryState query={summary}>{(data) => <CommissionSummary summary={data} />}</QueryState>
      <p className={styles['commissions-page__how']}>{t('commissions.howToPay')}</p>
      <Segmented
        label={t('commissions.viewLabel')}
        options={VIEWS.map((value) => ({ value, label: t(`commissions.views.${value}`) }))}
        value={view}
        onChange={(value) => setFilter({ view: value === 'statements' ? null : value, page: null })}
      />
      {view === 'statements' ? (
        <Statements page={page} onPage={(next) => setFilter({ page: String(next) })} />
      ) : (
        <Charges page={page} onPage={(next) => setFilter({ page: String(next) })} />
      )}
    </div>
  )
}

function Statements({ page, onPage }) {
  const { t, i18n } = useTranslation()
  const lng = i18n.language
  const path = useLocalizedPath()
  const query = useGetMyStatementsQuery({ page }, { refetchOnMountOrArgChange: true })

  return (
    <QueryState query={query}>
      {(data) =>
        data.items.length === 0 ? (
          <EmptyState icon="money" title={t('commissions.statementsEmpty')} description={t('commissions.statementsEmptyText')} />
        ) : (
          <>
            <RequestList>
              {data.items.map((statement) => {
                const state = statementState(statement)
                return (
                  <RequestCard
                    key={statement.id}
                    to={path(`/commissions/${statement.id}`)}
                    title={weekText(statement, lng)}
                    highlight={state.key === 'Overdue'}
                    tags={<Tag tone={state.tone}>{t(`commissions.status.${state.key}`)}</Tag>}
                    excerpt={t('commissions.statementLine', { total: formatMoney(statement.total, lng), paid: formatMoney(statement.paidAmount, lng) })}
                    meta={
                      statement.status === 'Paid'
                        ? t('commissions.paidOn', { date: formatDate(statement.paidAt, lng) })
                        : t('commissions.dueLine', { amount: formatMoney(statement.outstanding, lng), date: formatDate(statement.dueOn, lng) })
                    }
                  />
                )
              })}
            </RequestList>
            <Pagination page={data.page} pageSize={data.pageSize} totalCount={data.totalCount} onChange={onPage} />
          </>
        )
      }
    </QueryState>
  )
}

function Charges({ page, onPage }) {
  const { t, i18n } = useTranslation()
  const lng = i18n.language
  const path = useLocalizedPath()
  const query = useGetMyCommissionsQuery({ page }, { refetchOnMountOrArgChange: true })

  return (
    <QueryState query={query}>
      {(data) =>
        data.items.length === 0 ? (
          <EmptyState icon="orders" title={t('commissions.chargesEmpty')} description={t('commissions.chargesEmptyText')} />
        ) : (
          <>
            <RequestList>
              {data.items.map((line) => (
                <RequestCard
                  key={line.id}
                  to={path(`/orders/${line.orderId}`)}
                  title={line.summary || t('commissions.order')}
                  tags={
                    <Tag tone={line.statementId ? 'neutral' : 'accent'}>{t(line.statementId ? 'commissions.billed' : 'commissions.unbilled')}</Tag>
                  }
                  excerpt={t('commissions.chargeLine', {
                    price: formatMoney(line.orderPrice, lng),
                    rate: formatPercent(line.ratePercent, lng),
                    amount: formatMoney(line.amount, lng),
                  })}
                  meta={t('commissions.completedOn', { date: formatDate(line.completedAt, lng) })}
                />
              ))}
            </RequestList>
            <Pagination page={data.page} pageSize={data.pageSize} totalCount={data.totalCount} onChange={onPage} />
          </>
        )
      }
    </QueryState>
  )
}
