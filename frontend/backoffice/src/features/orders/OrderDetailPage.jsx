import { ArrowLeftOutlined } from '@ant-design/icons'
import { Alert, App, Button, Card, Descriptions, Flex, Rate, Space, Spin, Table, Tag, Timeline, Typography } from 'antd'
import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { useSelector } from 'react-redux'
import { Link, useParams } from 'react-router'
import { errorMessage } from '@/api/errors'
import ReasonModal from '@/components/ReasonModal/ReasonModal'
import { selectStaff } from '@/features/auth/authSlice'
import { hasPermission } from '@/features/auth/permissions'
import ResolvePaymentModal from '@/features/payments/ResolvePaymentModal'
import { placeText } from '@/features/requests/requestParts'
import { useReviewModeration } from '@/features/reviews/useReviewModeration'
import { formatDate, formatMoney } from '@/i18n/format'
import { ORDER_STATUS_COLORS, PAYMENT_STATUS_COLORS } from './orderParts'
import { useCancelOrderMutation, useGetOrderQuery, useResolveOrderMutation } from './ordersApi'

/** "Oct 8, 2026 · 3 days" for work, the visit time for a visit. */
function scheduleText(order, t, language) {
  if (order.kind === 'Visit') return formatDate(order.visitAt, language, { withTime: true }) || '—'
  const parts = [formatDate(order.startDate, language), order.durationDays && t('orders.days', { n: order.durationDays })]
  return parts.filter(Boolean).join(' · ') || '—'
}

/** One order for staff: terms, both parties, payments, the review, changes and history, with cancel and resolve. */
export default function OrderDetailPage() {
  const { id } = useParams()
  const { t, i18n } = useTranslation()
  const { message } = App.useApp()
  const staff = useSelector(selectStaff)
  const { data: order, isLoading, error } = useGetOrderQuery(id)
  const [cancel] = useCancelOrderMutation()
  const [resolve, resolving] = useResolveOrderMutation()
  const [cancelOpen, setCancelOpen] = useState(false)
  const [paymentToResolve, setPaymentToResolve] = useState(null)
  const moderation = useReviewModeration()
  const language = i18n.language

  const back = (
    <Link to="/orders">
      <ArrowLeftOutlined /> {t('orders.detail.back')}
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

  const canManage = hasPermission(staff, 'orders.manage')
  const canResolvePayments = hasPermission(staff, 'payments.manage')
  const canModerate = hasPermission(staff, 'reviews.moderate')
  const review = order.review

  const onCancel = async (reason) => {
    try {
      await cancel({ id, reason }).unwrap()
      message.success(t('orders.cancel.done'))
    } catch (failure) {
      if (failure?.status === 400) throw failure
      message.error(errorMessage(t, failure))
    }
    setCancelOpen(false)
  }

  const onResolve = async () => {
    try {
      await resolve(id).unwrap()
      message.success(t('orders.resolve.done'))
    } catch (failure) {
      message.error(errorMessage(t, failure))
    }
  }

  const paymentColumns = [
    { title: t('payments.columns.amount'), key: 'amount', render: (_, payment) => formatMoney(payment.amount, language) },
    {
      title: t('payments.columns.how'),
      key: 'how',
      render: (_, payment) => `${t(`payments.method.${payment.method}`)} · ${formatDate(payment.paidOn, language)} · ${t(`payments.recordedBy.${payment.recordedBy}`)}`,
    },
    {
      title: t('payments.columns.status'),
      key: 'status',
      render: (_, payment) => (
        <Flex vertical gap={2} align="flex-start">
          <Tag color={PAYMENT_STATUS_COLORS[payment.status]}>{t(`payments.status.${payment.status}`)}</Tag>
          {payment.disputeReason && <Typography.Text type="secondary">{payment.disputeReason}</Typography.Text>}
          {payment.status === 'Disputed' && canResolvePayments && (
            <Button size="small" type="primary" onClick={() => setPaymentToResolve(payment)}>
              {t('payments.resolve.button')}
            </Button>
          )}
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
            {t(`orders.kind.${order.kind}`)} · {placeText(order.place)}
          </Typography.Title>
          <Tag color={ORDER_STATUS_COLORS[order.status]}>{t(`orders.status.${order.status}`)}</Tag>
        </Space>
        {canManage && (
          <Space wrap>
            {order.actions.includes('resolve') && (
              <Button type="primary" loading={resolving.isLoading} onClick={onResolve}>
                {t('orders.resolve.button')}
              </Button>
            )}
            {order.actions.includes('cancel') && (
              <Button danger onClick={() => setCancelOpen(true)}>
                {t('orders.cancel.button')}
              </Button>
            )}
          </Space>
        )}
      </Flex>

      {order.needsAttentionSince && (
        <Alert
          type="warning"
          showIcon
          title={t('orders.detail.attention', { date: formatDate(order.needsAttentionSince, language, { withTime: true }) })}
          description={t('orders.detail.attentionText')}
        />
      )}
      {order.cancelReason && (
        <Alert
          type="info"
          showIcon
          title={t('orders.detail.cancelledBy', { who: t(`orders.party.${order.cancelledBy}`), date: formatDate(order.cancelledAt, language, { withTime: true }) })}
          description={order.cancelReason}
        />
      )}

      <Card title={t('orders.detail.terms')}>
        <Descriptions column={{ xs: 1, md: 2 }} size="small">
          <Descriptions.Item label={t('orders.columns.price')}>
            {formatMoney(order.price, language)}
            {order.price !== order.terms.price && ` (${t('orders.detail.agreed', { price: formatMoney(order.terms.price, language) })})`}
          </Descriptions.Item>
          <Descriptions.Item label={t('orders.detail.schedule')}>{scheduleText(order, t, language)}</Descriptions.Item>
          <Descriptions.Item label={t('orders.columns.created')}>{formatDate(order.createdAt, language, { withTime: true })}</Descriptions.Item>
          <Descriptions.Item label={t('orders.detail.request')}>
            <Link to={`/requests/${order.requestId}`}>{t('orders.detail.openRequest')}</Link>
          </Descriptions.Item>
          <Descriptions.Item label={t('orders.detail.summary')} span={2}>
            <Typography.Paragraph style={{ whiteSpace: 'pre-line', marginBottom: 0 }}>{order.terms.summary}</Typography.Paragraph>
          </Descriptions.Item>
        </Descriptions>
      </Card>

      <Card title={t('orders.detail.parties')}>
        <Descriptions column={{ xs: 1, md: 2 }} size="small">
          <Descriptions.Item label={t('orders.detail.customer')}>
            <Link to={`/users/${order.customer.userId}`}>{order.customer.fullName ?? '—'}</Link> · {order.customer.phone}
          </Descriptions.Item>
          <Descriptions.Item label={t('orders.detail.partner')}>
            <Link to={`/partners/${order.partner.partnerId}`}>{order.partner.displayName}</Link> · {order.partner.phone}
          </Descriptions.Item>
        </Descriptions>
      </Card>

      <Card title={t('orders.detail.payments', { paid: formatMoney(order.paidAmount, language), total: formatMoney(order.price, language) })}>
        <Flex vertical gap="middle">
          <Typography.Text type="secondary">
            {order.stages.map((stage) => `${stage.title ?? t(`orders.purpose.${stage.purpose}`)} ${formatMoney(stage.amount, language)}`).join(' · ')}
          </Typography.Text>
          <Table rowKey="id" columns={paymentColumns} dataSource={order.payments} pagination={false} scroll={{ x: true }} locale={{ emptyText: t('orders.detail.noPayments') }} />
        </Flex>
      </Card>

      {review && (
        <Card
          title={t('orders.detail.review')}
          extra={
            canModerate &&
            (review.isHidden ? (
              <Button size="small" onClick={() => moderation.restore(review)}>
                {t('reviews.restore.button')}
              </Button>
            ) : (
              <Button size="small" danger onClick={() => moderation.hide(review)}>
                {t('reviews.hide.button')}
              </Button>
            ))
          }
        >
          <Flex vertical gap={4}>
            <Rate disabled value={review.rating} style={{ fontSize: 14 }} aria-label={t('reviews.rating', { n: review.rating })} />
            {review.text && <Typography.Paragraph style={{ whiteSpace: 'pre-line', marginBottom: 0 }}>{review.text}</Typography.Paragraph>}
            {review.reply && (
              <Typography.Text type="secondary">
                {t('reviews.reply')}: {review.reply}
              </Typography.Text>
            )}
            {review.hiddenReason && <Typography.Text type="warning">{t('reviews.hiddenBecause', { reason: review.hiddenReason })}</Typography.Text>}
          </Flex>
        </Card>
      )}

      {order.changeRequests.length > 0 && (
        <Card title={t('orders.detail.changes')}>
          <Flex vertical gap="small">
            {order.changeRequests.map((change) => (
              <Flex key={change.id} justify="space-between" gap="middle" wrap>
                <span>
                  <Typography.Text strong>{t(`orders.changeKind.${change.kind}`)}</Typography.Text>
                  {change.title && ` · ${change.title}`}
                  {change.amount != null && ` · +${formatMoney(change.amount, language)}`} · {t(`orders.party.${change.proposedBy}`)}
                </span>
                <Tag>{t(`orders.changeStatus.${change.status}`)}</Tag>
              </Flex>
            ))}
          </Flex>
        </Card>
      )}

      <Card title={t('orders.detail.history')}>
        <Timeline
          items={order.history.map((step, index) => ({
            key: index,
            content: (
              <Flex vertical>
                <Typography.Text strong>{t(`orders.status.${step.status}`)}</Typography.Text>
                <Typography.Text type="secondary" style={{ fontSize: 12 }}>
                  {formatDate(step.changedAt, language, { withTime: true })}
                  {step.by && ` · ${t(`orders.party.${step.by}`)}`}
                </Typography.Text>
                {step.note && <Typography.Text>{step.note}</Typography.Text>}
              </Flex>
            ),
          }))}
        />
      </Card>

      <ReasonModal
        open={cancelOpen}
        title={t('orders.cancel.title')}
        text={t('orders.cancel.text')}
        label={t('orders.cancel.reason')}
        okText={t('orders.cancel.confirm')}
        danger
        onConfirm={onCancel}
        onCancel={() => setCancelOpen(false)}
      />
      <ResolvePaymentModal payment={paymentToResolve} onClose={() => setPaymentToResolve(null)} />
      {moderation.dialog}
    </Flex>
  )
}
