import { ArrowLeftOutlined } from '@ant-design/icons'
import { Alert, Button, Card, Descriptions, Flex, Spin, Table, Tag, Typography } from 'antd'
import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { useSelector } from 'react-redux'
import { Link, useParams } from 'react-router'
import { errorMessage } from '@/api/errors'
import { selectStaff } from '@/features/auth/authSlice'
import { hasPermission } from '@/features/auth/permissions'
import { formatDate, formatMoney } from '@/i18n/format'
import { useGetStatementQuery } from './commissionsApi'
import SettlementModal from './SettlementModal'
import { formatPercent, weekText } from './commissionParts'
import StatementStatusTag from './StatementStatusTag'

/** One weekly statement: the partner, totals, the orders it covers and the payments recorded against it. */
export default function StatementDetailPage() {
  const { id } = useParams()
  const { t, i18n } = useTranslation()
  const language = i18n.language
  const staff = useSelector(selectStaff)
  const { data, isLoading, error } = useGetStatementQuery(id)
  const [settling, setSettling] = useState(false)

  const back = (
    <Link to="/commissions/statements">
      <ArrowLeftOutlined /> {t('commissions.detail.back')}
    </Link>
  )

  if (isLoading) return <Spin />
  if (error) {
    return (
      <Flex vertical gap="middle">
        {back}
        <Alert type="error" showIcon title={errorMessage(t, error)} />
      </Flex>
    )
  }

  const { summary, lines, settlements } = data
  const statement = summary.statement
  const canSettle = hasPermission(staff, 'commissions.manage') && statement.status === 'Open'

  const lineColumns = [
    {
      title: t('commissions.detail.order'),
      key: 'order',
      render: (_, line) => (
        <Flex vertical>
          <Link to={`/orders/${line.orderId}`}>{line.summary || t('commissions.detail.orderLink')}</Link>
          <Typography.Text type="secondary" style={{ fontSize: 12 }}>
            {t('commissions.detail.completed', { date: formatDate(line.completedAt, language) })}
          </Typography.Text>
        </Flex>
      ),
    },
    { title: t('commissions.detail.price'), key: 'price', responsive: ['md'], render: (_, line) => formatMoney(line.orderPrice, language) },
    { title: t('commissions.detail.rate'), key: 'rate', responsive: ['md'], render: (_, line) => formatPercent(line.ratePercent, language) },
    { title: t('commissions.detail.commission'), key: 'amount', render: (_, line) => <Typography.Text strong>{formatMoney(line.amount, language)}</Typography.Text> },
  ]

  const settlementColumns = [
    { title: t('commissions.settle.amount'), key: 'amount', render: (_, s) => formatMoney(s.amount, language) },
    {
      title: t('commissions.settle.method'),
      key: 'method',
      render: (_, s) => `${t(`commissions.method.${s.method}`)} · ${formatDate(s.paidOn, language)}`,
    },
    { title: t('commissions.settle.reference'), key: 'reference', responsive: ['md'], render: (_, s) => s.reference ?? '—' },
    { title: t('commissions.detail.recorded'), key: 'recorded', responsive: ['lg'], render: (_, s) => formatDate(s.recordedAt, language, { withTime: true }) },
  ]

  return (
    <Flex vertical gap="middle">
      {back}
      <Flex justify="space-between" align="center" wrap gap="small">
        <Typography.Title level={1} style={{ margin: 0 }}>
          {t('commissions.detail.title', { week: weekText(statement, language) })}
        </Typography.Title>
        {canSettle && (
          <Button type="primary" onClick={() => setSettling(true)}>
            {t('commissions.settle.button')}
          </Button>
        )}
      </Flex>

      <Card>
        <Descriptions column={{ xs: 1, md: 2 }} size="small">
          <Descriptions.Item label={t('commissions.columns.partner')}>
            <Flex gap="small" wrap align="center">
              <Link to={`/partners/${summary.partnerId}`}>{summary.partnerName}</Link>
              <span>{summary.partnerPhone}</span>
              {summary.partnerPaused && <Tag color="warning">{t('commissions.paused')}</Tag>}
            </Flex>
          </Descriptions.Item>
          <Descriptions.Item label={t('commissions.columns.status')}>
            <StatementStatusTag statement={statement} />
          </Descriptions.Item>
          <Descriptions.Item label={t('commissions.detail.total')}>{formatMoney(statement.total, language)}</Descriptions.Item>
          <Descriptions.Item label={t('commissions.detail.paid')}>{formatMoney(statement.paidAmount, language)}</Descriptions.Item>
          <Descriptions.Item label={t('commissions.columns.outstanding')}>
            <Typography.Text strong>{formatMoney(statement.outstanding, language)}</Typography.Text>
          </Descriptions.Item>
          <Descriptions.Item label={t('commissions.columns.due')}>{formatDate(statement.dueOn, language)}</Descriptions.Item>
          <Descriptions.Item label={t('commissions.detail.issued')}>{formatDate(statement.issuedAt, language, { withTime: true })}</Descriptions.Item>
          {statement.paidAt && <Descriptions.Item label={t('commissions.detail.paidAt')}>{formatDate(statement.paidAt, language, { withTime: true })}</Descriptions.Item>}
        </Descriptions>
      </Card>

      <Card title={t('commissions.detail.lines', { count: lines.length })}>
        <Table rowKey="id" columns={lineColumns} dataSource={lines} pagination={false} scroll={{ x: true }} size="small" />
      </Card>

      <Card title={t('commissions.detail.settlements')}>
        <Table
          rowKey="id"
          columns={settlementColumns}
          dataSource={settlements}
          pagination={false}
          scroll={{ x: true }}
          size="small"
          locale={{ emptyText: t('commissions.detail.noSettlements') }}
        />
      </Card>

      <SettlementModal statement={settling ? statement : null} partnerName={summary.partnerName} onClose={() => setSettling(false)} />
    </Flex>
  )
}
