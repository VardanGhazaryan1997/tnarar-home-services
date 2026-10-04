import { Alert, Flex, Input, Segmented, Select, Table, Tag, Typography } from 'antd'
import { useTranslation } from 'react-i18next'
import { Link } from 'react-router'
import { errorMessage } from '@/api/errors'
import { formatDate } from '@/i18n/format'
import { useUrlFilters } from '@/shared/useUrlFilters'
import { placeText, REQUEST_STATUS_COLORS } from './requestParts'
import { useGetRequestsQuery } from './requestsApi'

const QUEUE = 'queue'
const ALL = 'all'
const STATUSES = ['Open', 'Closed', 'Cancelled']

/**
 * Service requests. Opens on the operator queue (requests nobody could take or answer, waiting longest first);
 * filters live in the address (?show=&kind=&search=&page=).
 */
export default function RequestsPage() {
  const { t, i18n } = useTranslation()
  const { get, update, page } = useUrlFilters()
  const show = get('show', QUEUE)
  const kind = get('kind')
  const search = get('search', '')

  const { data, isFetching, error } = useGetRequestsQuery({
    needsAttention: show === QUEUE ? true : undefined,
    status: STATUSES.includes(show) ? show : undefined,
    kind,
    search,
    page,
  })

  const columns = [
    {
      title: t('requests.columns.request'),
      key: 'request',
      render: (_, request) => (
        <Flex vertical>
          <Link to={`/requests/${request.id}`}>{request.excerpt}</Link>
          <Typography.Text type="secondary" style={{ fontSize: 12 }}>
            {placeText(request.place)} · {t(`requests.kind.${request.kind}`)}
          </Typography.Text>
        </Flex>
      ),
    },
    {
      title: t('requests.columns.customer'),
      key: 'customer',
      responsive: ['md'],
      render: (_, request) => (
        <Flex vertical>
          <Typography.Text>{request.customerName ?? '—'}</Typography.Text>
          <Typography.Text type="secondary" style={{ fontSize: 12 }}>
            {request.customerPhone}
          </Typography.Text>
        </Flex>
      ),
    },
    {
      title: t('requests.columns.partners'),
      key: 'partners',
      responsive: ['lg'],
      render: (_, request) => t('requests.counts', { sent: request.sentTo, responded: request.responded, declined: request.declined }),
    },
    {
      title: t('requests.columns.created'),
      key: 'created',
      responsive: ['sm'],
      render: (_, request) => formatDate(request.createdAt, i18n.language, { withTime: true }),
    },
    {
      title: t('requests.columns.status'),
      key: 'status',
      render: (_, request) => (
        <Flex vertical gap={4} align="flex-start">
          <Tag color={REQUEST_STATUS_COLORS[request.status]}>{t(`requests.status.${request.status}`)}</Tag>
          {request.attentionReason && <Tag color="warning">{t(`requests.attention.${request.attentionReason}`)}</Tag>}
        </Flex>
      ),
    },
  ]

  return (
    <>
      <Typography.Title level={1}>{t('nav.requests')}</Typography.Title>
      <Flex vertical gap="middle">
        <Segmented
          aria-label={t('requests.filters.show')}
          value={show}
          onChange={(value) => update({ show: value === QUEUE ? undefined : value })}
          options={[
            { value: QUEUE, label: t('requests.queue') },
            ...STATUSES.map((value) => ({ value, label: t(`requests.status.${value}`) })),
            { value: ALL, label: t('requests.filters.all') },
          ]}
          style={{ maxWidth: '100%', overflowX: 'auto' }}
        />
        <Flex gap="small" wrap>
          <Input.Search
            aria-label={t('requests.filters.search')}
            placeholder={t('requests.filters.searchPlaceholder')}
            defaultValue={search}
            allowClear
            onSearch={(value) => update({ search: value.trim() || undefined })}
            style={{ maxWidth: 360 }}
          />
          <Select
            aria-label={t('requests.filters.kind')}
            placeholder={t('requests.filters.anyKind')}
            allowClear
            value={kind}
            onChange={(value) => update({ kind: value })}
            options={['Open', 'Direct'].map((value) => ({ value, label: t(`requests.kind.${value}`) }))}
            style={{ minWidth: 200 }}
          />
        </Flex>
        {error && <Alert type="error" showIcon title={errorMessage(t, error)} />}
        <Table
          rowKey="id"
          columns={columns}
          dataSource={data?.items ?? []}
          loading={isFetching}
          scroll={{ x: true }}
          locale={{ emptyText: t(show === QUEUE ? 'requests.queueEmpty' : 'requests.empty') }}
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
