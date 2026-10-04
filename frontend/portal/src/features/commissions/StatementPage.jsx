import { useTranslation } from 'react-i18next'
import { Link, useParams } from 'react-router'
import PageHeader from '@/components/PageHeader/PageHeader'
import QueryState from '@/components/QueryState/QueryState'
import Card from '@/components/ui/Card/Card'
import EmptyState from '@/components/ui/EmptyState/EmptyState'
import Tag from '@/components/ui/Tag/Tag'
import { useLocalizedPath } from '@/i18n/hooks'
import { formatDate, formatDateTime, formatMoney } from '@/shared/format'
import { formatPercent, statementState, weekText } from './commissionParts'
import { useGetMyStatementQuery } from './commissionsApi'
import styles from './commissions.module.scss'

/** One weekly statement: totals, the orders it covers and the payments the team recorded. */
export default function StatementPage() {
  const { id } = useParams()
  const { t, i18n } = useTranslation()
  const lng = i18n.language
  const path = useLocalizedPath()
  const query = useGetMyStatementQuery(id)
  const back = { to: path('/commissions'), label: t('commissions.back') }

  return (
    <QueryState
      query={query}
      notFound={
        <div className={styles['commissions-page']}>
          <PageHeader title={t('commissions.notFound')} back={back} />
          <EmptyState icon="money" title={t('commissions.notFound')} description={t('commissions.notFoundText')} />
        </div>
      }
    >
      {({ statement, lines, settlements }) => {
        const state = statementState(statement)
        const facts = [
          ['total', formatMoney(statement.total, lng)],
          ['paid', formatMoney(statement.paidAmount, lng)],
          ['outstanding', formatMoney(statement.outstanding, lng)],
          ['dueOn', formatDate(statement.dueOn, lng)],
          ['issuedAt', formatDate(statement.issuedAt, lng)],
          ...(statement.paidAt ? [['paidAt', formatDate(statement.paidAt, lng)]] : []),
        ]
        return (
          <div className={styles['commissions-page']}>
            <PageHeader
              title={t('commissions.statementTitle', { week: weekText(statement, lng) })}
              back={back}
              actions={<Tag tone={state.tone}>{t(`commissions.status.${state.key}`)}</Tag>}
            />
            <Card>
              <dl className={styles['statement-facts']}>
                {facts.map(([key, value]) => (
                  <div key={key} className={styles['statement-facts__row']}>
                    <dt>{t(`commissions.facts.${key}`)}</dt>
                    <dd>{value}</dd>
                  </div>
                ))}
              </dl>
              {statement.status !== 'Paid' && <p className={styles['commissions-page__how']}>{t('commissions.howToPay')}</p>}
            </Card>
            <Card title={t('commissions.orders', { count: lines.length })}>
              <ul className={styles['commission-lines']}>
                {lines.map((line) => (
                  <li key={line.id} className={styles['commission-lines__item']}>
                    <span className={styles['commission-lines__main']}>
                      <Link to={path(`/orders/${line.orderId}`)}>{line.summary || t('commissions.order')}</Link>
                      <span className={styles['commission-lines__meta']}>
                        {t('commissions.completedOn', { date: formatDate(line.completedAt, lng) })} ·{' '}
                        {t('commissions.priceRate', { price: formatMoney(line.orderPrice, lng), rate: formatPercent(line.ratePercent, lng) })}
                      </span>
                    </span>
                    <strong className={styles['commission-lines__amount']}>{formatMoney(line.amount, lng)}</strong>
                  </li>
                ))}
              </ul>
            </Card>
            <Card title={t('commissions.payments')}>
              {settlements.length === 0 ? (
                <p className={styles['commission-lines__meta']}>{t('commissions.noPayments')}</p>
              ) : (
                <ul className={styles['commission-lines']}>
                  {settlements.map((settlement) => (
                    <li key={settlement.id} className={styles['commission-lines__item']}>
                      <span className={styles['commission-lines__main']}>
                        <span>
                          {t(`commissions.method.${settlement.method}`)} · {formatDate(settlement.paidOn, lng)}
                        </span>
                        <span className={styles['commission-lines__meta']}>
                          {[settlement.reference, t('commissions.recordedAt', { date: formatDateTime(settlement.recordedAt, lng) })].filter(Boolean).join(' · ')}
                        </span>
                      </span>
                      <strong className={styles['commission-lines__amount']}>{formatMoney(settlement.amount, lng)}</strong>
                    </li>
                  ))}
                </ul>
              )}
            </Card>
          </div>
        )
      }}
    </QueryState>
  )
}
