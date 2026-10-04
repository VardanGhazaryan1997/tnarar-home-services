import { Alert, Flex, Input, Segmented, Select, Table, Typography } from 'antd'
import { useTranslation } from 'react-i18next'
import { Link, useSearchParams } from 'react-router'
import { errorMessage } from '@/api/errors'
import { formatDate } from '@/i18n/format'
import PartnerStatusTag from './PartnerStatusTag'
import { PARTNER_STATUSES } from './statuses'
import { useGetPartnersQuery } from './partnersApi'

const ALL = 'all'
const TYPES = ['Specialist', 'Company', 'Supplier']

/**
 * Partner profiles. Opens on the review queue (Under review, oldest first); filters live in the
 * address (?status=&type=&search=&page=) so a link shows the same list.
 */
export default function PartnersPage() {
  const { t, i18n } = useTranslation()
  const [params, setParams] = useSearchParams()
  const status = params.get('status') ?? 'UnderReview'
  const type = params.get('type') ?? undefined
  const search = params.get('search') ?? ''
  const page = Number(params.get('page') ?? 1)

  const { data, isFetching, error } = useGetPartnersQuery({
    status: status === ALL ? undefined : status,
    type,
    search,
    page,
  })

  const update = (changes) => {
    const next = new URLSearchParams(params)
    for (const [key, value] of Object.entries({ page: undefined, ...changes })) {
      if (value === undefined || value === '') next.delete(key)
      else next.set(key, String(value))
    }
    setParams(next)
  }

  const columns = [
    {
      title: t('partners.columns.name'),
      key: 'name',
      render: (_, partner) => (
        <Flex vertical>
          <Link to={`/partners/${partner.id}`}>{partner.displayName}</Link>
          <Typography.Text type="secondary" style={{ fontSize: 12 }}>
            {t(`partners.type.${partner.type}`)}
          </Typography.Text>
        </Flex>
      ),
    },
    {
      title: t('partners.columns.owner'),
      key: 'owner',
      responsive: ['md'],
      render: (_, partner) => (
        <Flex vertical>
          <Typography.Text>{partner.ownerName ?? '—'}</Typography.Text>
          <Typography.Text type="secondary" style={{ fontSize: 12 }}>
            {partner.phone}
          </Typography.Text>
        </Flex>
      ),
    },
    {
      title: t('partners.columns.coverage'),
      key: 'coverage',
      responsive: ['lg'],
      render: (_, partner) => t('partners.coverage', { services: partner.serviceCount, areas: partner.areaCount }),
    },
    {
      title: t('partners.columns.submitted'),
      key: 'submitted',
      responsive: ['sm'],
      render: (_, partner) => formatDate(partner.submittedAt ?? partner.createdAt, i18n.language),
    },
    {
      title: t('partners.columns.status'),
      key: 'status',
      render: (_, partner) => <PartnerStatusTag status={partner.status} />,
    },
  ]

  return (
    <>
      <Typography.Title level={1}>{t('nav.partners')}</Typography.Title>
      <Flex vertical gap="middle">
        <Segmented
          aria-label={t('partners.filters.status')}
          value={status}
          onChange={(value) => update({ status: value === 'UnderReview' ? undefined : value })}
          options={[
            ...PARTNER_STATUSES.map((value) => ({ value, label: t(value === 'UnderReview' ? 'partners.queue' : `partners.status.${value}`) })),
            { value: ALL, label: t('partners.filters.all') },
          ]}
          style={{ maxWidth: '100%', overflowX: 'auto' }}
        />
        <Flex gap="small" wrap>
          <Input.Search
            aria-label={t('partners.filters.search')}
            placeholder={t('partners.filters.searchPlaceholder')}
            defaultValue={search}
            allowClear
            onSearch={(value) => update({ search: value.trim() || undefined })}
            style={{ maxWidth: 360 }}
          />
          <Select
            aria-label={t('partners.filters.type')}
            placeholder={t('partners.filters.anyType')}
            allowClear
            value={type}
            onChange={(value) => update({ type: value })}
            options={TYPES.map((value) => ({ value, label: t(`partners.type.${value}`) }))}
            style={{ minWidth: 180 }}
          />
        </Flex>
        {error && <Alert type="error" showIcon title={errorMessage(t, error)} />}
        <Table
          rowKey="id"
          columns={columns}
          dataSource={data?.items ?? []}
          loading={isFetching}
          scroll={{ x: true }}
          locale={{ emptyText: t(status === 'UnderReview' ? 'partners.queueEmpty' : 'partners.empty') }}
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
