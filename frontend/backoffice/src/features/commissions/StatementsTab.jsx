import { Alert, Button, Flex, Segmented, Space, Table, Tag, Typography } from 'antd'
import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { useSelector } from 'react-redux'
import { Link } from 'react-router'
import { errorMessage } from '@/api/errors'
import { selectStaff } from '@/features/auth/authSlice'
import { hasPermission } from '@/features/auth/permissions'
import { formatDate, formatMoney } from '@/i18n/format'
import { useUrlFilters } from '@/shared/useUrlFilters'
import { useGetStatementsQuery } from './commissionsApi'
import SettlementModal from './SettlementModal'
import { weekText } from './commissionParts'
import StatementStatusTag from './StatementStatusTag'

const ALL = 'all'
const FILTERS = ['Open', 'Overdue', 'Paid']

/** Partners' weekly statements. Opens on the unpaid ones, due soonest first; ?partnerId keeps one partner's. */
export default function StatementsTab() {
  const { t, i18n } = useTranslation()
  const language = i18n.language
  const staff = useSelector(selectStaff)
  const { get, update, page } = useUrlFilters()
  const filter = get('filter', 'Open')
  const partnerId = get('partnerId')
  const [settling, setSettling] = useState(null)
  const { data, isFetching, error } = useGetStatementsQuery({ filter: filter === ALL ? undefined : filter, partnerId, page })
  const canManage = hasPermission(staff, 'commissions.manage')
  const rows = data?.items ?? []

  const columns = [
    {
      title: t('commissions.columns.partner'),
      key: 'partner',
      render: (_, row) => (
        <Flex vertical align="flex-start">
          <Link to={`/partners/${row.partnerId}`}>{row.partnerName}</Link>
          <Typography.Text type="secondary" style={{ fontSize: 12 }}>
            {row.partnerPhone}
          </Typography.Text>
          {row.partnerPaused && <Tag color="warning">{t('commissions.paused')}</Tag>}
        </Flex>
      ),
    },
    {
      title: t('commissions.columns.week'),
      key: 'week',
      responsive: ['md'],
      render: (_, row) => <Link to={`/commissions/statements/${row.statement.id}`}>{weekText(row.statement, language)}</Link>,
    },
    {
      title: t('commissions.columns.amount'),
      key: 'amount',
      render: (_, row) => (
        <Flex vertical>
          <Typography.Text strong>{formatMoney(row.statement.outstanding, language)}</Typography.Text>
          <Typography.Text type="secondary" style={{ fontSize: 12 }}>
            {t('commissions.ofTotal', { total: formatMoney(row.statement.total, language) })}
          </Typography.Text>
        </Flex>
      ),
    },
    {
      title: t('commissions.columns.due'),
      key: 'due',
      render: (_, row) => (
        <Flex vertical gap={2} align="flex-start">
          <span>{formatDate(row.statement.dueOn, language)}</span>
          <StatementStatusTag statement={row.statement} />
        </Flex>
      ),
    },
    {
      title: '',
      key: 'actions',
      render: (_, row) => (
        <Space wrap>
          <Link to={`/commissions/statements/${row.statement.id}`}>{t('commissions.open')}</Link>
          {canManage && row.statement.status === 'Open' && (
            <Button size="small" type="primary" onClick={() => setSettling(row)}>
              {t('commissions.settle.button')}
            </Button>
          )}
        </Space>
      ),
    },
  ]

  return (
    <Flex vertical gap="middle">
      <Typography.Text type="secondary">{t('commissions.help')}</Typography.Text>
      <Segmented
        aria-label={t('commissions.filters.label')}
        value={filter}
        onChange={(value) => update({ filter: value === 'Open' ? undefined : value })}
        options={[...FILTERS.map((value) => ({ value, label: t(`commissions.filters.${value}`) })), { value: ALL, label: t('commissions.filters.all') }]}
        style={{ maxWidth: '100%', overflowX: 'auto' }}
      />
      {partnerId && (
        <div>
          <Tag closable onClose={() => update({ partnerId: undefined })}>
            {t('commissions.filters.partner', { name: rows[0]?.partnerName ?? '…' })}
          </Tag>
        </div>
      )}
      {error && <Alert type="error" showIcon title={errorMessage(t, error)} />}
      <Table
        rowKey={(row) => row.statement.id}
        columns={columns}
        dataSource={rows}
        loading={isFetching}
        scroll={{ x: true }}
        locale={{ emptyText: t(`commissions.empty.${filter}`) }}
        pagination={{
          current: page,
          pageSize: data?.pageSize ?? 20,
          total: data?.totalCount ?? 0,
          showSizeChanger: false,
          hideOnSinglePage: true,
          onChange: (next) => update({ page: next === 1 ? undefined : next }),
        }}
      />
      <SettlementModal statement={settling?.statement ?? null} partnerName={settling?.partnerName} onClose={() => setSettling(null)} />
    </Flex>
  )
}
