import { ArrowLeftOutlined } from '@ant-design/icons'
import { Alert, App, Button, Card, Descriptions, Flex, Image, Space, Spin, Table, Tag, Typography } from 'antd'
import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { useSelector } from 'react-redux'
import { Link, useParams } from 'react-router'
import { errorMessage } from '@/api/errors'
import ReasonModal from '@/components/ReasonModal/ReasonModal'
import { selectStaff } from '@/features/auth/authSlice'
import { hasPermission } from '@/features/auth/permissions'
import { formatDate, formatMoney } from '@/i18n/format'
import AssignModal from './AssignModal'
import { placeText, RECIPIENT_STATUS_COLORS, REQUEST_STATUS_COLORS } from './requestParts'
import { useAssignRequestMutation, useCancelRequestMutation, useGetRequestQuery } from './requestsApi'

function budgetText(request, language) {
  const { budgetMin: min, budgetMax: max } = request
  if (min == null && max == null) return '—'
  if (min != null && max != null) return `${formatMoney(min, language)} – ${formatMoney(max, language)}`
  return formatMoney(min ?? max, language)
}

/** One request for operators: what is needed and where, the customer, every partner who received it, and the actions. */
export default function RequestDetailPage() {
  const { id } = useParams()
  const { t, i18n } = useTranslation()
  const { message } = App.useApp()
  const staff = useSelector(selectStaff)
  const { data: request, isLoading, error } = useGetRequestQuery(id)
  const [assign] = useAssignRequestMutation()
  const [cancel] = useCancelRequestMutation()
  const [dialog, setDialog] = useState(null)

  const back = (
    <Link to="/requests">
      <ArrowLeftOutlined /> {t('requests.detail.back')}
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

  const canManage = hasPermission(staff, 'requests.manage') && request.status === 'Open'

  const onAssign = async (partnerIds) => {
    await assign({ id, partnerIds }).unwrap()
    message.success(t('requests.assign.done', { n: partnerIds.length }))
    setDialog(null)
  }

  const onCancel = async (reason) => {
    try {
      await cancel({ id, reason }).unwrap()
      message.success(t('requests.cancel.done'))
    } catch (failure) {
      if (failure?.status === 400) throw failure
      message.error(errorMessage(t, failure))
    }
    setDialog(null)
  }

  const recipientColumns = [
    {
      title: t('requests.detail.partner'),
      key: 'partner',
      render: (_, recipient) => <Link to={`/partners/${recipient.partnerId}`}>{recipient.displayName}</Link>,
    },
    { title: t('requests.detail.source'), key: 'source', render: (_, recipient) => t(`requests.source.${recipient.source}`) },
    { title: t('requests.detail.sent'), key: 'sent', responsive: ['md'], render: (_, recipient) => formatDate(recipient.sentAt, i18n.language, { withTime: true }) },
    {
      title: t('requests.columns.status'),
      key: 'status',
      render: (_, recipient) => (
        <Flex vertical gap={2} align="flex-start">
          <Tag color={RECIPIENT_STATUS_COLORS[recipient.status]}>{t(`requests.recipientStatus.${recipient.status}`)}</Tag>
          {recipient.declineReason && <Typography.Text type="secondary">{recipient.declineReason}</Typography.Text>}
        </Flex>
      ),
    },
  ]

  return (
    <Flex vertical gap="middle">
      {back}
      <Flex justify="space-between" align="center" wrap gap="middle">
        <Space align="center" wrap>
          <Typography.Title level={1} style={{ margin: 0 }}>
            {placeText(request.place)}
          </Typography.Title>
          <Tag color={REQUEST_STATUS_COLORS[request.status]}>{t(`requests.status.${request.status}`)}</Tag>
        </Space>
        {canManage && (
          <Space wrap>
            <Button type="primary" onClick={() => setDialog('assign')}>
              {t('requests.assign.button')}
            </Button>
            <Button danger onClick={() => setDialog('cancel')}>
              {t('requests.cancel.button')}
            </Button>
          </Space>
        )}
      </Flex>

      {request.attentionReason && (
        <Alert
          type="warning"
          showIcon
          title={t(`requests.attention.${request.attentionReason}`)}
          description={t('requests.detail.waitingSince', { date: formatDate(request.needsAttentionSince, i18n.language, { withTime: true }) })}
        />
      )}
      {request.cancelReason && <Alert type="info" showIcon title={t('requests.detail.cancelled')} description={request.cancelReason} />}

      <Card title={t('requests.detail.request')}>
        <Descriptions column={{ xs: 1, md: 2 }} size="small">
          <Descriptions.Item label={t('requests.detail.kind')}>{t(`requests.kind.${request.kind}`)}</Descriptions.Item>
          <Descriptions.Item label={t('requests.columns.created')}>{formatDate(request.createdAt, i18n.language, { withTime: true })}</Descriptions.Item>
          <Descriptions.Item label={t('requests.detail.when')}>
            {[formatDate(request.preferredDate, i18n.language), request.timeNote].filter(Boolean).join(' · ') || '—'}
          </Descriptions.Item>
          <Descriptions.Item label={t('requests.detail.budget')}>{budgetText(request, i18n.language)}</Descriptions.Item>
          <Descriptions.Item label={t('requests.detail.description')} span={2}>
            <Typography.Paragraph style={{ whiteSpace: 'pre-line', marginBottom: 0 }}>{request.description}</Typography.Paragraph>
          </Descriptions.Item>
        </Descriptions>
        {request.media.length > 0 && (
          <Image.PreviewGroup>
            <Flex wrap gap="small" style={{ marginTop: 16 }}>
              {request.media.map((file) => (
                <Image key={file.id} width={96} height={96} style={{ objectFit: 'cover' }} src={file.thumbnailUrl ?? file.url} preview={{ src: file.url }} alt={file.fileName} />
              ))}
            </Flex>
          </Image.PreviewGroup>
        )}
      </Card>

      <Card title={t('requests.detail.customer')}>
        <Descriptions column={{ xs: 1, md: 2 }} size="small">
          <Descriptions.Item label={t('requests.detail.name')}>
            <Link to={`/users/${request.customer.userId}`}>{request.customer.fullName ?? '—'}</Link>
          </Descriptions.Item>
          <Descriptions.Item label={t('requests.detail.phone')}>{request.customer.phone}</Descriptions.Item>
          <Descriptions.Item label={t('requests.detail.email')}>{request.customer.email ?? '—'}</Descriptions.Item>
          <Descriptions.Item label={t('requests.detail.account')}>
            {request.customer.isBlocked ? <Tag color="error">{t('requests.detail.blocked')}</Tag> : <Tag color="success">{t('requests.detail.active')}</Tag>}
          </Descriptions.Item>
        </Descriptions>
      </Card>

      <Card title={t('requests.detail.recipients', { n: request.recipients.length })}>
        <Table rowKey="partnerId" columns={recipientColumns} dataSource={request.recipients} pagination={false} scroll={{ x: true }} locale={{ emptyText: t('requests.detail.noRecipients') }} />
      </Card>

      <AssignModal open={dialog === 'assign'} exclude={request.recipients.map((recipient) => recipient.partnerId)} onConfirm={onAssign} onCancel={() => setDialog(null)} />
      <ReasonModal
        open={dialog === 'cancel'}
        title={t('requests.cancel.title')}
        text={t('requests.cancel.text')}
        label={t('requests.cancel.reason')}
        okText={t('requests.cancel.confirm')}
        danger
        onConfirm={onCancel}
        onCancel={() => setDialog(null)}
      />
    </Flex>
  )
}
