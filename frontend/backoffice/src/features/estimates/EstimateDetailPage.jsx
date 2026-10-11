import { ArrowLeftOutlined } from '@ant-design/icons'
import { Alert, Card, Descriptions, Flex, Space, Spin, Table, Tag, Typography } from 'antd'
import { useTranslation } from 'react-i18next'
import { Link, useParams } from 'react-router'
import { errorMessage } from '@/api/errors'
import { REQUEST_STATUS_COLORS } from '@/features/requests/requestParts'
import { formatDate, formatMoney } from '@/i18n/format'
import { useGetEstimateQuery } from './estimatesApi'
import MaterialLines from './MaterialLines'
import WorkLines from './WorkLines'

/** One customer estimate: owner, total, every room with its work and prices, and the requests made from it. */
export default function EstimateDetailPage() {
  const { id } = useParams()
  const { t, i18n } = useTranslation()
  const lng = i18n.language
  const { data: estimate, isLoading, error } = useGetEstimateQuery(id)
  const range = (min, max) => `${formatMoney(min, lng)} – ${formatMoney(max, lng)}`
  const number = (value) => new Intl.NumberFormat(lng, { maximumFractionDigits: 2 }).format(value)

  const back = (
    <Link to="/estimates">
      <ArrowLeftOutlined /> {t('estimates.back')}
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

  const { measurement } = estimate
  const requestColumns = [
    {
      title: t('estimates.columns.request'),
      key: 'request',
      render: (_, request) => <Link to={`/requests/${request.id}`}>{request.categoryName}</Link>,
    },
    { title: t('estimates.columns.lines'), dataIndex: 'lineCount', key: 'lines', responsive: ['md'] },
    { title: t('estimates.columns.created'), key: 'created', render: (_, request) => formatDate(request.createdAt, lng, { withTime: true }) },
    {
      title: t('requests.columns.status'),
      key: 'status',
      render: (_, request) => <Tag color={REQUEST_STATUS_COLORS[request.status]}>{t(`requests.status.${request.status}`)}</Tag>,
    },
  ]

  return (
    <Flex vertical gap="middle">
      {back}
      <Space align="center" wrap>
        <Typography.Title level={1} style={{ margin: 0 }}>
          {estimate.title}
        </Typography.Title>
        {estimate.isShared && <Tag color="processing">{t('estimates.shared')}</Tag>}
        {estimate.oldBuilding && <Tag color="warning">{t('estimates.oldBuilding')}</Tag>}
      </Space>

      <Card title={t('estimates.summary')}>
        <Descriptions column={{ xs: 1, md: 2 }} size="small">
          <Descriptions.Item label={t('estimates.columns.owner')}>
            {estimate.owner ? (
              <Link to={`/users/${estimate.owner.userId}`}>{[estimate.owner.fullName, estimate.owner.phone].filter(Boolean).join(' · ')}</Link>
            ) : (
              '—'
            )}
          </Descriptions.Item>
          <Descriptions.Item label={t('estimates.columns.total')}>
            {measurement.totalTypical > 0
              ? `${range(measurement.totalMin, measurement.totalMax)} (${t('estimates.usually', { amount: formatMoney(measurement.totalTypical, lng) })})`
              : '—'}
          </Descriptions.Item>
          <Descriptions.Item label={t('estimates.columns.created')}>{formatDate(estimate.createdAt, lng, { withTime: true })}</Descriptions.Item>
          <Descriptions.Item label={t('estimates.columns.updated')}>{estimate.updatedAt ? formatDate(estimate.updatedAt, lng, { withTime: true }) : '—'}</Descriptions.Item>
          {measurement.unpricedLines > 0 && (
            <Descriptions.Item label={t('estimates.unpriced')}>{measurement.unpricedLines}</Descriptions.Item>
          )}
        </Descriptions>
      </Card>

      <Card title={t('estimates.requestsTitle')}>
        <Table
          rowKey="id"
          size="small"
          columns={requestColumns}
          dataSource={estimate.requests}
          pagination={false}
          locale={{ emptyText: t('estimates.noRequestYet') }}
        />
      </Card>

      {estimate.rooms.map((room, index) => {
        const measured = measurement.rooms[index]
        const lines = measured.lines.map((line) => ({
          key: line.workItemId,
          room: room.name,
          name: line.name,
          unit: line.unit,
          quantity: line.quantity,
          min: line.priceMin,
          max: line.priceMax,
        }))
        const gone = room.lines.length - measured.lines.length
        return (
          <Card
            key={room.id}
            title={`${room.name} · ${t(`catalog.roomTemplates.roomTypes.${room.type}`)}`}
            extra={measured.totalTypical > 0 ? range(measured.totalMin, measured.totalMax) : null}
          >
            <Typography.Paragraph type="secondary">
              {t('estimates.geometry', {
                floor: number(measured.floorArea),
                walls: number(measured.wallArea),
                height: number(room.height),
                openings: room.openings.reduce((sum, opening) => sum + opening.count, 0),
              })}
            </Typography.Paragraph>
            <WorkLines lines={lines} />
            {gone > 0 && <Alert type="info" showIcon style={{ marginTop: 12 }} title={t('estimates.gone', { n: gone })} />}
          </Card>
        )
      })}
      {estimate.measurement.materialGrade && estimate.measurement.materials?.length > 0 && (
        <Card
          title={t('requests.detail.materials', { grade: t(`estimates.grades.${estimate.measurement.materialGrade}`) })}
          extra={formatMoney(estimate.measurement.materialsTotal, lng)}
        >
          <MaterialLines materials={estimate.measurement.materials} />
        </Card>
      )}
    </Flex>
  )
}
