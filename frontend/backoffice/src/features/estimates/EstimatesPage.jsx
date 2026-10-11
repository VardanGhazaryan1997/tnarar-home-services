import { Alert, Card, Col, Flex, Input, Row, Segmented, Select, Statistic, Table, Tag, Typography } from 'antd'
import { useTranslation } from 'react-i18next'
import { Link } from 'react-router'
import { errorMessage } from '@/api/errors'
import { formatDate, formatMoney } from '@/i18n/format'
import { useUrlFilters } from '@/shared/useUrlFilters'
import { useGetEstimateStatsQuery, useGetEstimatesQuery } from './estimatesApi'

const SHOW = ['all', 'withRequest', 'withoutRequest']

/**
 * Customers' saved estimates: how many there are and how many became requests, then the list (last changed first).
 * Filters live in the address (?show=&shared=&search=&page=).
 */
export default function EstimatesPage() {
  const { t, i18n } = useTranslation()
  const lng = i18n.language
  const { get, update, page } = useUrlFilters()
  const show = get('show', 'all')
  const shared = get('shared')
  const search = get('search', '')
  const stats = useGetEstimateStatsQuery()
  const { data, isFetching, error } = useGetEstimatesQuery({
    search,
    withRequest: show === 'withRequest' ? true : show === 'withoutRequest' ? false : undefined,
    shared: shared === 'yes' ? true : shared === 'no' ? false : undefined,
    page,
  })

  const columns = [
    {
      title: t('estimates.columns.estimate'),
      key: 'estimate',
      render: (_, estimate) => (
        <Flex vertical>
          <Link to={`/estimates/${estimate.id}`}>{estimate.title}</Link>
          <Typography.Text type="secondary" style={{ fontSize: 12 }}>
            {t('estimates.size', { rooms: estimate.roomCount, lines: estimate.lineCount })}
          </Typography.Text>
        </Flex>
      ),
    },
    {
      title: t('estimates.columns.owner'),
      key: 'owner',
      responsive: ['md'],
      render: (_, estimate) =>
        estimate.owner ? (
          <Flex vertical>
            <Link to={`/users/${estimate.owner.userId}`}>{estimate.owner.fullName ?? '—'}</Link>
            <Typography.Text type="secondary" style={{ fontSize: 12 }}>
              {estimate.owner.phone}
            </Typography.Text>
          </Flex>
        ) : (
          '—'
        ),
    },
    {
      title: t('estimates.columns.total'),
      key: 'total',
      align: 'right',
      render: (_, estimate) => (estimate.totalTypical > 0 ? `${formatMoney(estimate.totalMin, lng)} – ${formatMoney(estimate.totalMax, lng)}` : '—'),
    },
    {
      title: t('estimates.columns.requests'),
      key: 'requests',
      render: (_, estimate) => (
        <Flex gap={4} wrap>
          {estimate.requestCount > 0 ? <Tag color="success">{t('estimates.requestCount', { n: estimate.requestCount })}</Tag> : <Tag>{t('estimates.noRequest')}</Tag>}
          {estimate.isShared && <Tag color="processing">{t('estimates.shared')}</Tag>}
        </Flex>
      ),
    },
    {
      title: t('estimates.columns.updated'),
      key: 'updated',
      responsive: ['sm'],
      render: (_, estimate) => formatDate(estimate.updatedAt ?? estimate.createdAt, lng, { withTime: true }),
    },
  ]

  const stat = (key, value, suffix) => (
    <Col xs={12} md={6} key={key}>
      <Card size="small">
        <Statistic title={t(`estimates.stats.${key}`)} value={stats.data ? value(stats.data) : '—'} suffix={stats.data ? suffix : undefined} />
      </Card>
    </Col>
  )

  return (
    <>
      <Typography.Title level={1}>{t('nav.estimates')}</Typography.Title>
      <Flex vertical gap="middle">
        <Row gutter={[16, 16]}>
          {stat('total', (s) => s.total)}
          {stat('withRequest', (s) => s.withRequest)}
          {stat('conversion', (s) => s.conversionPercent, '%')}
          {stat('last30', (s) => `${s.withRequestLast30Days} / ${s.totalLast30Days}`)}
        </Row>
        <Typography.Text type="secondary">{t('estimates.statsHelp')}</Typography.Text>
        <Segmented
          aria-label={t('estimates.filters.show')}
          value={show}
          onChange={(value) => update({ show: value === 'all' ? undefined : value })}
          options={SHOW.map((value) => ({ value, label: t(`estimates.filters.${value}`) }))}
          style={{ maxWidth: '100%', overflowX: 'auto' }}
        />
        <Flex gap="small" wrap>
          <Input.Search
            aria-label={t('estimates.filters.search')}
            placeholder={t('estimates.filters.searchPlaceholder')}
            defaultValue={search}
            allowClear
            onSearch={(value) => update({ search: value.trim() || undefined })}
            style={{ maxWidth: 360 }}
          />
          <Select
            aria-label={t('estimates.filters.shared')}
            placeholder={t('estimates.filters.sharedAny')}
            allowClear
            value={shared}
            onChange={(value) => update({ shared: value })}
            options={['yes', 'no'].map((value) => ({ value, label: t(`estimates.filters.shared_${value}`) }))}
            style={{ minWidth: 200 }}
          />
        </Flex>
        {(error || stats.error) && <Alert type="error" showIcon title={errorMessage(t, error ?? stats.error)} />}
        <Table
          rowKey="id"
          columns={columns}
          dataSource={data?.items ?? []}
          loading={isFetching}
          scroll={{ x: true }}
          locale={{ emptyText: t('estimates.empty') }}
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
