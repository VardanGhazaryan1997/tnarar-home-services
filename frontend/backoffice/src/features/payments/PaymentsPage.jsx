import { Alert, Button, Flex, Segmented, Table, Tag, Typography } from 'antd'
import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { useSelector } from 'react-redux'
import { Link } from 'react-router'
import { errorMessage } from '@/api/errors'
import { selectStaff } from '@/features/auth/authSlice'
import { hasPermission } from '@/features/auth/permissions'
import { PAYMENT_STATUS_COLORS } from '@/features/orders/orderParts'
import { formatDate, formatMoney } from '@/i18n/format'
import { useUrlFilters } from '@/shared/useUrlFilters'
import { useGetPaymentsQuery } from './paymentsApi'
import ResolvePaymentModal from './ResolvePaymentModal'

const ALL = 'all'
const STATUSES = ['Disputed', 'Pending', 'Confirmed', 'Rejected', 'Withdrawn']

/** Payments recorded between customers and partners. Opens on the disputes, waiting longest first. */
export default function PaymentsPage() {
  const { t, i18n } = useTranslation()
  const staff = useSelector(selectStaff)
  const { get, update, page } = useUrlFilters()
  const status = get('status', 'Disputed')
  const [resolving, setResolving] = useState(null)
  const { data, isFetching, error } = useGetPaymentsQuery({ status: status === ALL ? undefined : status, page })
  const canManage = hasPermission(staff, 'payments.manage')

  const columns = [
    {
      title: t('payments.columns.amount'),
      key: 'amount',
      render: (_, payment) => (
        <Flex vertical>
          <Typography.Text strong>{formatMoney(payment.amount, i18n.language)}</Typography.Text>
          <Link to={`/orders/${payment.orderId}`}>{t('payments.ofOrder', { price: formatMoney(payment.orderPrice, i18n.language) })}</Link>
        </Flex>
      ),
    },
    {
      title: t('payments.columns.how'),
      key: 'how',
      responsive: ['md'],
      render: (_, payment) => (
        <Flex vertical>
          <span>
            {t(`payments.method.${payment.method}`)} · {formatDate(payment.paidOn, i18n.language)}
          </span>
          <Typography.Text type="secondary" style={{ fontSize: 12 }}>
            {t(`payments.recordedBy.${payment.recordedBy}`)}
          </Typography.Text>
        </Flex>
      ),
    },
    {
      title: t('payments.columns.parties'),
      key: 'parties',
      responsive: ['lg'],
      render: (_, payment) => (
        <Flex vertical>
          <span>
            {payment.customerName ?? '—'} · {payment.customerPhone}
          </span>
          <Link to={`/partners/${payment.partnerId}`}>{payment.partnerName}</Link>
        </Flex>
      ),
    },
    {
      title: t('payments.columns.status'),
      key: 'status',
      render: (_, payment) => (
        <Flex vertical gap={2} align="flex-start">
          <Tag color={PAYMENT_STATUS_COLORS[payment.status]}>{t(`payments.status.${payment.status}`)}</Tag>
          {payment.disputeReason && <Typography.Text type="secondary">{payment.disputeReason}</Typography.Text>}
          {payment.resolutionNote && <Typography.Text type="secondary">{payment.resolutionNote}</Typography.Text>}
        </Flex>
      ),
    },
    {
      title: '',
      key: 'actions',
      render: (_, payment) =>
        canManage &&
        payment.status === 'Disputed' && (
          <Button size="small" type="primary" onClick={() => setResolving(payment)}>
            {t('payments.resolve.button')}
          </Button>
        ),
    },
  ]

  return (
    <>
      <Typography.Title level={1}>{t('nav.payments')}</Typography.Title>
      <Flex vertical gap="middle">
        <Typography.Text type="secondary">{t('payments.help')}</Typography.Text>
        <Segmented
          aria-label={t('payments.filters.status')}
          value={status}
          onChange={(value) => update({ status: value === 'Disputed' ? undefined : value })}
          options={[
            ...STATUSES.map((value) => ({ value, label: t(value === 'Disputed' ? 'payments.disputes' : `payments.status.${value}`) })),
            { value: ALL, label: t('payments.filters.all') },
          ]}
          style={{ maxWidth: '100%', overflowX: 'auto' }}
        />
        {error && <Alert type="error" showIcon title={errorMessage(t, error)} />}
        <Table
          rowKey="id"
          columns={columns}
          dataSource={data?.items ?? []}
          loading={isFetching}
          scroll={{ x: true }}
          locale={{ emptyText: t(status === 'Disputed' ? 'payments.disputesEmpty' : 'payments.empty') }}
          pagination={{
            current: page,
            pageSize: data?.pageSize ?? 20,
            total: data?.totalCount ?? 0,
            showSizeChanger: false,
            hideOnSinglePage: true,
            onChange: (next) => update({ page: next === 1 ? undefined : next }),
          }}
        />
      </Flex>
      <ResolvePaymentModal payment={resolving} onClose={() => setResolving(null)} />
    </>
  )
}
