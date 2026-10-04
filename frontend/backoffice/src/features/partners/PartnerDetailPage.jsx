import { ArrowLeftOutlined, FileOutlined, PlayCircleOutlined } from '@ant-design/icons'
import { Alert, App, Button, Card, Descriptions, Empty, Flex, Image, Space, Spin, Tag, Timeline, Typography } from 'antd'
import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { useSelector } from 'react-redux'
import { Link, useParams } from 'react-router'
import { errorMessage, fieldErrors } from '@/api/errors'
import { selectStaff } from '@/features/auth/authSlice'
import { hasPermission } from '@/features/auth/permissions'
import DecisionModal from './DecisionModal'
import { formatDate } from '@/i18n/format'
import PartnerStatusTag from './PartnerStatusTag'
import { DECISIONS_BY_STATUS, useDecideOnPartnerMutation, useGetCategoryNamesQuery, useGetPartnerQuery, useGetPlaceNamesQuery, useGetRegionNamesQuery } from './partnersApi'

// Category names by id, subcategories included.
function categoryNames(tree = []) {
  const names = {}
  const walk = (nodes) =>
    nodes.forEach((node) => {
      names[node.id] = node.name
      walk(node.children ?? [])
    })
  walk(tree)
  return names
}

function areaLabel(area, cities = [], regions = [], t) {
  if (area.regionId) {
    const region = regions.find((r) => r.id === area.regionId)
    return region ? t('partners.detail.wholeRegion', { region: region.name }) : t('partners.detail.unknownPlace')
  }
  const city = cities.find((c) => c.id === area.cityId)
  if (!city) return t('partners.detail.unknownPlace')
  if (!area.districtId) return t('partners.detail.wholeCity', { city: city.name })
  const district = city.districts.find((d) => d.id === area.districtId)
  return district ? `${city.name} · ${district.name}` : city.name
}

function WorkExamples({ items }) {
  const { t } = useTranslation()
  if (items.length === 0) return <Empty image={Empty.PRESENTED_IMAGE_SIMPLE} description={t('partners.detail.noWorkExamples')} />

  return (
    <Image.PreviewGroup>
      <Flex wrap gap="small">
        {items.map(({ id, caption, file }) =>
          file.kind === 'Image' ? (
            <Image key={id} width={120} height={120} style={{ objectFit: 'cover' }} src={file.thumbnailUrl ?? file.url} preview={{ src: file.url }} alt={caption ?? file.fileName} />
          ) : (
            <Button key={id} href={file.url} target="_blank" rel="noreferrer" icon={<PlayCircleOutlined />} style={{ height: 120, width: 120, whiteSpace: 'normal' }}>
              {caption ?? file.fileName}
            </Button>
          ),
        )}
      </Flex>
    </Image.PreviewGroup>
  )
}

/** One partner profile for reviewers: what they offer, where, their work and documents, owner and history. */
export default function PartnerDetailPage() {
  const { id } = useParams()
  const { t, i18n } = useTranslation()
  const { message } = App.useApp()
  const staff = useSelector(selectStaff)
  const { data, isLoading, error } = useGetPartnerQuery(id)
  const { data: categories } = useGetCategoryNamesQuery(i18n.language)
  const { data: cities } = useGetPlaceNamesQuery(i18n.language)
  const { data: regions } = useGetRegionNamesQuery(i18n.language)
  const [decide] = useDecideOnPartnerMutation()
  const [decision, setDecision] = useState(null)

  const back = (
    <Link to="/partners">
      <ArrowLeftOutlined /> {t('partners.detail.back')}
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

  const { profile, owner, history } = data
  const names = categoryNames(categories)
  const decisions = hasPermission(staff, 'partners.approve') ? (DECISIONS_BY_STATUS[profile.status] ?? []) : []

  const confirm = async (comment) => {
    await decide({ id, decision, comment }).unwrap()
    message.success(t(`partners.decisions.${decision}.done`))
    setDecision(null)
  }

  // Validation errors go back to the dialog; anything else closes it with a message.
  const onConfirm = async (comment) => {
    try {
      await confirm(comment)
    } catch (failure) {
      if (fieldErrors(t, failure).length > 0) throw failure
      message.error(errorMessage(t, failure))
      setDecision(null)
    }
  }

  return (
    <Flex vertical gap="middle">
      {back}
      <Flex justify="space-between" align="center" wrap gap="middle">
        <Space align="center" wrap>
          <Typography.Title level={1} style={{ margin: 0 }}>
            {profile.displayName}
          </Typography.Title>
          <PartnerStatusTag status={profile.status} />
        </Space>
        {decisions.length > 0 && (
          <Space wrap>
            {decisions.map((key) => (
              <Button key={key} type={key === 'approve' || key === 'reinstate' ? 'primary' : 'default'} danger={key === 'reject' || key === 'suspend'} onClick={() => setDecision(key)}>
                {t(`partners.decisions.${key}.button`)}
              </Button>
            ))}
          </Space>
        )}
      </Flex>

      {profile.reviewComment && <Alert type="warning" showIcon title={t('partners.detail.reviewComment')} description={profile.reviewComment} />}

      <Card title={t('partners.detail.profile')}>
        <Descriptions column={{ xs: 1, md: 2 }} size="small">
          <Descriptions.Item label={t('partners.detail.type')}>{t(`partners.type.${profile.type}`)}</Descriptions.Item>
          <Descriptions.Item label={t('partners.detail.experience')}>
            {profile.yearsOfExperience == null ? '—' : t('partners.detail.years', { count: profile.yearsOfExperience })}
          </Descriptions.Item>
          <Descriptions.Item label={t('partners.detail.submitted')}>{formatDate(profile.submittedAt, i18n.language, { withTime: true }) || '—'}</Descriptions.Item>
          <Descriptions.Item label={t('partners.detail.publicAddress')}>{profile.slug ?? '—'}</Descriptions.Item>
          <Descriptions.Item label={t('partners.detail.about')} span={2}>
            <Typography.Paragraph style={{ whiteSpace: 'pre-line', marginBottom: 0 }}>{profile.about || '—'}</Typography.Paragraph>
          </Descriptions.Item>
          <Descriptions.Item label={t('partners.detail.services')} span={2}>
            <Space wrap size={[4, 4]}>
              {profile.categoryIds.map((categoryId) => (
                <Tag key={categoryId}>{names[categoryId] ?? t('partners.detail.unknownCategory')}</Tag>
              ))}
            </Space>
          </Descriptions.Item>
          <Descriptions.Item label={t('partners.detail.areas')} span={2}>
            <Space wrap size={[4, 4]}>
              {profile.areas.map((area) => (
                <Tag key={`${area.regionId ?? area.cityId}-${area.districtId ?? 'all'}`}>{areaLabel(area, cities, regions, t)}</Tag>
              ))}
            </Space>
          </Descriptions.Item>
        </Descriptions>
      </Card>

      <Card title={t('partners.detail.workExamples', { count: profile.workExamples.length })}>
        <WorkExamples items={profile.workExamples} />
      </Card>

      <Card title={t('partners.detail.documents', { count: profile.documents.length })}>
        {profile.documents.length === 0 ? (
          <Empty image={Empty.PRESENTED_IMAGE_SIMPLE} description={t('partners.detail.noDocuments')} />
        ) : (
          <Flex vertical gap="small">
            {profile.documents.map(({ id: documentId, caption, file }) => (
              <a key={documentId} href={file.url} target="_blank" rel="noreferrer">
                <FileOutlined /> {caption ?? file.fileName}
              </a>
            ))}
          </Flex>
        )}
      </Card>

      <Card title={t('partners.detail.owner')}>
        <Descriptions column={{ xs: 1, md: 2 }} size="small">
          <Descriptions.Item label={t('partners.detail.ownerName')}>{owner.fullName ?? '—'}</Descriptions.Item>
          <Descriptions.Item label={t('partners.detail.phone')}>{owner.phone}</Descriptions.Item>
          <Descriptions.Item label={t('partners.detail.email')}>{owner.email ?? '—'}</Descriptions.Item>
          <Descriptions.Item label={t('partners.detail.account')}>
            {owner.isBlocked ? <Tag color="error">{t('partners.detail.blocked')}</Tag> : <Tag color="success">{t('partners.detail.active')}</Tag>}
          </Descriptions.Item>
        </Descriptions>
      </Card>

      <Card title={t('partners.detail.history')}>
        <Timeline
          items={history.map((step) => ({
            key: step.sequence,
            content: (
              <Flex vertical>
                <Typography.Text strong>
                  {t(`partners.status.${step.fromStatus}`)} → {t(`partners.status.${step.toStatus}`)}
                </Typography.Text>
                <Typography.Text type="secondary" style={{ fontSize: 12 }}>
                  {formatDate(step.at, i18n.language, { withTime: true })} · {step.actorName ?? t('partners.detail.system')}
                </Typography.Text>
                {step.comment && <Typography.Text>{step.comment}</Typography.Text>}
              </Flex>
            ),
          }))}
        />
      </Card>

      <DecisionModal decision={decision} partnerName={profile.displayName} onConfirm={onConfirm} onCancel={() => setDecision(null)} />
    </Flex>
  )
}
