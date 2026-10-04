import { DeleteOutlined, EditOutlined, EyeInvisibleOutlined, EyeOutlined, PlusOutlined } from '@ant-design/icons'
import { Alert, App, Button, Flex, Popconfirm, Segmented, Space, Table, Tag, Tooltip, Typography } from 'antd'
import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { errorMessage } from '@/api/errors'
import { localizedName } from '@/features/catalog/names'
import { useCatalogLanguages } from '@/features/catalog/useCatalogLanguages'
import { useDeleteFaqMutation, useGetFaqsQuery, useSetFaqPublishedMutation } from './contentApi'
import { AUDIENCES } from './audiences'
import FaqFormModal from './FaqFormModal'
import PublishedTag from './PublishedTag'

const ALL = 'all'

/** Frequently asked questions for the website, by audience and position. */
export default function FaqsTab() {
  const { t, i18n } = useTranslation()
  const { message } = App.useApp()
  const { defaultCode } = useCatalogLanguages()
  const { data: faqs = [], isFetching, error } = useGetFaqsQuery()
  const [setPublished] = useSetFaqPublishedMutation()
  const [deleteFaq] = useDeleteFaqMutation()
  const [audience, setAudience] = useState(ALL)
  // null = closed, {} = new question, otherwise the question being edited.
  const [editing, setEditing] = useState(null)

  const run = async (action, done) => {
    try {
      await action().unwrap()
      message.success(t(done))
    } catch (failure) {
      message.error(errorMessage(t, failure))
    }
  }

  const questionOf = (faq) => localizedName(faq.question, i18n.resolvedLanguage, defaultCode)

  const columns = [
    {
      title: t('content.faqs.question'),
      key: 'question',
      render: (_, faq) => <Typography.Text>{questionOf(faq)}</Typography.Text>,
    },
    {
      title: t('content.faqs.audience'),
      key: 'audience',
      responsive: ['md'],
      render: (_, faq) => <Tag>{t(`content.audience.${faq.audience}`)}</Tag>,
    },
    { title: t('catalog.columns.sortOrder'), dataIndex: 'sortOrder', responsive: ['md'] },
    {
      title: t('catalog.columns.status'),
      key: 'status',
      render: (_, faq) => <PublishedTag published={faq.isPublished} />,
    },
    {
      title: t('catalog.columns.actions'),
      key: 'actions',
      render: (_, faq) => {
        const name = questionOf(faq)
        const toggle = faq.isPublished ? 'content.unpublish' : 'content.publish'
        return (
          <Space>
            <Tooltip title={t('catalog.actions.edit')}>
              <Button type="text" icon={<EditOutlined />} aria-label={`${t('catalog.actions.edit')}: ${name}`} onClick={() => setEditing(faq)} />
            </Tooltip>
            <Tooltip title={t(toggle)}>
              <Button
                type="text"
                icon={faq.isPublished ? <EyeInvisibleOutlined /> : <EyeOutlined />}
                aria-label={`${t(toggle)}: ${name}`}
                onClick={() => run(() => setPublished({ id: faq.id, published: !faq.isPublished }), faq.isPublished ? 'content.unpublished' : 'content.publishedDone')}
              />
            </Tooltip>
            <Popconfirm
              title={t('content.faqs.deleteConfirm')}
              okText={t('catalog.actions.delete')}
              okButtonProps={{ danger: true }}
              cancelText={t('catalog.form.cancel')}
              onConfirm={() => run(() => deleteFaq(faq.id), 'catalog.deleted')}
            >
              <Button type="text" danger icon={<DeleteOutlined />} aria-label={`${t('catalog.actions.delete')}: ${name}`} />
            </Popconfirm>
          </Space>
        )
      },
    },
  ]

  return (
    <Flex vertical gap="middle">
      <Flex justify="space-between" align="center" wrap gap="small">
        <Segmented
          aria-label={t('content.faqs.audience')}
          value={audience}
          onChange={setAudience}
          options={[{ value: ALL, label: t('partners.filters.all') }, ...AUDIENCES.map((value) => ({ value, label: t(`content.audience.${value}`) }))]}
        />
        <Button type="primary" icon={<PlusOutlined />} onClick={() => setEditing({})}>
          {t('content.faqs.add')}
        </Button>
      </Flex>
      {error && <Alert type="error" showIcon title={errorMessage(t, error)} />}
      <Table
        rowKey="id"
        columns={columns}
        dataSource={audience === ALL ? faqs : faqs.filter((faq) => faq.audience === audience)}
        loading={isFetching}
        pagination={false}
        scroll={{ x: true }}
        locale={{ emptyText: t('content.faqs.empty') }}
        expandable={{
          expandedRowRender: (faq) => (
            <Typography.Paragraph style={{ whiteSpace: 'pre-line', margin: 0 }}>{localizedName(faq.answer, i18n.resolvedLanguage, defaultCode)}</Typography.Paragraph>
          ),
        }}
      />
      <FaqFormModal open={editing !== null} faq={editing?.id ? editing : undefined} onClose={() => setEditing(null)} />
    </Flex>
  )
}
