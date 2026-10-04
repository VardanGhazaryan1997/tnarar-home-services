import { Alert, Flex, Input, Segmented, Table, Tag, Typography } from 'antd'
import { useTranslation } from 'react-i18next'
import { Link } from 'react-router'
import { errorMessage } from '@/api/errors'
import { placeText } from '@/features/requests/requestParts'
import { formatDate, formatMoney } from '@/i18n/format'
import { useUrlFilters } from '@/shared/useUrlFilters'
import { ORDER_STATUSES, ORDER_STATUS_COLORS } from './orderParts'
import { useGetOrdersQuery } from './ordersApi'

const ALL = 'all'
const ATTENTION = 'attention'

/** Every order, newest first; "Needs attention" lists orders cancelled after work started, waiting longest first. */
export default function OrdersPage() {
  const { t, i18n } = useTranslation()
  const { get, update, page } = useUrlFilters()
  const show = get('show', ALL)
  const search = get('search', '')
  const { data, isFetching, error } = useGetOrdersQuery({
    needsAttention: show === ATTENTION ? true : undefined,
    status: ORDER_STATUSES.includes(show) ? show : undefined,
    search,
    page,
  })

  const columns = [
    {
      title: t('orders.columns.order'),
      key: 'order',
      render: (_, order) => (
        <Flex vertical>
          <Link to={`/orders/${order.id}`}>{order.summary}</Link>
          <Typography.Text type="secondary" style={{ fontSize: 12 }}>
            {placeText(order.place)} · {t(`orders.kind.${order.kind}`)}
          </Typography.Text>
        </Flex>
      ),
    },
    {
      title: t('orders.columns.parties'),
      key: 'parties',
      responsive: ['md'],
      render: (_, order) => (
        <Flex vertical>
          <span>
            {order.customerName ?? '—'} · {order.customerPhone}
          </span>
          <Link to={`/partners/${order.partnerId}`}>{order.partnerName}</Link>
        </Flex>
      ),
    },
    { title: t('orders.columns.price'), key: 'price', render: (_, order) => formatMoney(order.price, i18n.language) },
    { title: t('orders.columns.created'), key: 'created', responsive: ['lg'], render: (_, order) => formatDate(order.createdAt, i18n.language) },
    {
      title: t('orders.columns.status'),
      key: 'status',
      render: (_, order) => (
        <Flex vertical gap={4} align="flex-start">
          <Tag color={ORDER_STATUS_COLORS[order.status]}>{t(`orders.status.${order.status}`)}</Tag>
          {order.needsAttentionSince && <Tag color="error">{t('orders.needsAttention')}</Tag>}
          {order.pendingChange && <Tag color="warning">{t('orders.pendingChange')}</Tag>}
        </Flex>
      ),
    },
  ]

  return (
    <>
      <Typography.Title level={1}>{t('nav.orders')}</Typography.Title>
      <Flex vertical gap="middle">
        <Segmented
          aria-label={t('orders.filters.show')}
          value={show}
          onChange={(value) => update({ show: value === ALL ? undefined : value })}
          options={[
            { value: ALL, label: t('orders.filters.all') },
            { value: ATTENTION, label: t('orders.needsAttention') },
            ...ORDER_STATUSES.map((value) => ({ value, label: t(`orders.status.${value}`) })),
          ]}
          style={{ maxWidth: '100%', overflowX: 'auto' }}
        />
        <Input.Search
          aria-label={t('orders.filters.search')}
          placeholder={t('orders.filters.searchPlaceholder')}
          defaultValue={search}
          allowClear
          onSearch={(value) => update({ search: value.trim() || undefined })}
          style={{ maxWidth: 360 }}
        />
        {error && <Alert type="error" showIcon title={errorMessage(t, error)} />}
        <Table
          rowKey="id"
          columns={columns}
          dataSource={data?.items ?? []}
          loading={isFetching}
          scroll={{ x: true }}
          locale={{ emptyText: t(show === ATTENTION ? 'orders.attentionEmpty' : 'orders.empty') }}
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
    </>
  )
}
