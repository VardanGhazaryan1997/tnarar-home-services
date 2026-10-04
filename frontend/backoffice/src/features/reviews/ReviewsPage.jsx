import { Alert, Button, Flex, Rate, Segmented, Select, Table, Tag, Typography } from 'antd'
import { useTranslation } from 'react-i18next'
import { Link } from 'react-router'
import { errorMessage } from '@/api/errors'
import { formatDate } from '@/i18n/format'
import { useUrlFilters } from '@/shared/useUrlFilters'
import { useGetReviewsQuery } from './reviewsApi'
import { useReviewModeration } from './useReviewModeration'

const SHOW = { all: undefined, visible: false, hidden: true }

/** Every review, newest first; moderators hide ones that break the rules (contacts, insults) and restore them. */
export default function ReviewsPage() {
  const { t, i18n } = useTranslation()
  const { get, update, page } = useUrlFilters()
  const show = get('show', 'all')
  const maxRating = get('maxRating')
  const { data, isFetching, error } = useGetReviewsQuery({ hidden: SHOW[show], maxRating, page })
  const { hide, restore, dialog } = useReviewModeration()

  const columns = [
    {
      title: t('reviews.columns.review'),
      key: 'review',
      render: (_, review) => (
        <Flex vertical gap={4} style={{ maxWidth: 520 }}>
          <Rate disabled value={review.rating} style={{ fontSize: 14 }} aria-label={t('reviews.rating', { n: review.rating })} />
          {review.text && <Typography.Paragraph style={{ whiteSpace: 'pre-line', marginBottom: 0 }}>{review.text}</Typography.Paragraph>}
          {review.reply && (
            <Typography.Text type="secondary">
              {t('reviews.reply')}: {review.reply}
            </Typography.Text>
          )}
          {review.hiddenReason && <Typography.Text type="warning">{t('reviews.hiddenBecause', { reason: review.hiddenReason })}</Typography.Text>}
        </Flex>
      ),
    },
    {
      title: t('reviews.columns.partner'),
      key: 'partner',
      responsive: ['md'],
      render: (_, review) => (
        <Flex vertical>
          <Link to={`/partners/${review.partnerId}`}>{review.partnerName}</Link>
          <Typography.Text type="secondary" style={{ fontSize: 12 }}>
            {review.customerName ?? '—'} · <Link to={`/orders/${review.orderId}`}>{t('reviews.order')}</Link>
          </Typography.Text>
        </Flex>
      ),
    },
    { title: t('reviews.columns.date'), key: 'date', responsive: ['sm'], render: (_, review) => formatDate(review.submittedAt, i18n.language) },
    {
      title: t('reviews.columns.status'),
      key: 'status',
      render: (_, review) => (
        <Flex vertical gap={4} align="flex-start">
          <Tag color={review.isHidden ? 'default' : 'success'}>{t(review.isHidden ? 'reviews.hidden' : 'reviews.visible')}</Tag>
          {review.isHidden ? (
            <Button size="small" onClick={() => restore(review)}>
              {t('reviews.restore.button')}
            </Button>
          ) : (
            <Button size="small" danger onClick={() => hide(review)}>
              {t('reviews.hide.button')}
            </Button>
          )}
        </Flex>
      ),
    },
  ]

  return (
    <>
      <Typography.Title level={1}>{t('nav.reviews')}</Typography.Title>
      <Flex vertical gap="middle">
        <Flex gap="small" wrap align="center">
          <Segmented
            aria-label={t('reviews.filters.show')}
            value={show}
            onChange={(value) => update({ show: value === 'all' ? undefined : value })}
            options={Object.keys(SHOW).map((value) => ({ value, label: t(`reviews.filters.${value}`) }))}
          />
          <Select
            aria-label={t('reviews.filters.rating')}
            placeholder={t('reviews.filters.anyRating')}
            allowClear
            value={maxRating}
            onChange={(value) => update({ maxRating: value })}
            options={['2', '3'].map((value) => ({ value, label: t('reviews.filters.atMost', { n: value }) }))}
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
          locale={{ emptyText: t('reviews.empty') }}
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
      {dialog}
    </>
  )
}
