import { DeleteOutlined, EditOutlined, EyeInvisibleOutlined, EyeOutlined, PlusOutlined } from '@ant-design/icons'
import { Alert, App, Button, Flex, Popconfirm, Space, Table, Tooltip, Typography } from 'antd'
import { useTranslation } from 'react-i18next'
import { Link, useNavigate } from 'react-router'
import { errorMessage } from '@/api/errors'
import { localizedName } from '@/features/catalog/names'
import { useCatalogLanguages } from '@/features/catalog/useCatalogLanguages'
import { formatDate } from '@/i18n/format'
import { useDeletePageMutation, useGetPagesQuery, useSetPagePublishedMutation } from './contentApi'
import PublishedTag from './PublishedTag'

/** Static pages (About, Terms, …): drafts and published ones, by position. Editing opens a full page. */
export default function PagesTab() {
  const { t, i18n } = useTranslation()
  const { message } = App.useApp()
  const navigate = useNavigate()
  const { defaultCode } = useCatalogLanguages()
  const { data: pages = [], isFetching, error } = useGetPagesQuery()
  const [setPublished] = useSetPagePublishedMutation()
  const [deletePage] = useDeletePageMutation()

  const run = async (action, done) => {
    try {
      await action().unwrap()
      message.success(t(done))
    } catch (failure) {
      message.error(errorMessage(t, failure))
    }
  }

  const titleOf = (page) => localizedName(page.title, i18n.resolvedLanguage, defaultCode)

  const columns = [
    {
      title: t('content.columns.title'),
      key: 'title',
      render: (_, page) => (
        <Flex vertical>
          <Link to={`/content/pages/${page.id}`}>{titleOf(page)}</Link>
          <Typography.Text type="secondary" style={{ fontSize: 12 }}>
            /pages/{page.slug}
          </Typography.Text>
        </Flex>
      ),
    },
    {
      title: t('content.columns.footer'),
      key: 'footer',
      responsive: ['md'],
      render: (_, page) => (page.showInFooter ? t('content.yes') : '—'),
    },
    { title: t('catalog.columns.sortOrder'), dataIndex: 'sortOrder', responsive: ['md'] },
    {
      title: t('content.columns.updated'),
      key: 'updated',
      responsive: ['lg'],
      render: (_, page) => formatDate(page.updatedAt, i18n.language),
    },
    {
      title: t('catalog.columns.status'),
      key: 'status',
      render: (_, page) => <PublishedTag published={page.isPublished} />,
    },
    {
      title: t('catalog.columns.actions'),
      key: 'actions',
      render: (_, page) => {
        const name = titleOf(page)
        return (
          <Space>
            <Tooltip title={t('catalog.actions.edit')}>
              <Button type="text" icon={<EditOutlined />} aria-label={`${t('catalog.actions.edit')}: ${name}`} onClick={() => navigate(`/content/pages/${page.id}`)} />
            </Tooltip>
            <Tooltip title={t(page.isPublished ? 'content.unpublish' : 'content.publish')}>
              <Button
                type="text"
                icon={page.isPublished ? <EyeInvisibleOutlined /> : <EyeOutlined />}
                aria-label={`${t(page.isPublished ? 'content.unpublish' : 'content.publish')}: ${name}`}
                onClick={() =>
                  run(() => setPublished({ id: page.id, published: !page.isPublished }), page.isPublished ? 'content.unpublished' : 'content.publishedDone')
                }
              />
            </Tooltip>
            <Popconfirm
              title={t('content.pages.deleteConfirm', { name })}
              description={t('content.pages.deleteHelp')}
              okText={t('catalog.actions.delete')}
              okButtonProps={{ danger: true }}
              cancelText={t('catalog.form.cancel')}
              onConfirm={() => run(() => deletePage(page.id), 'catalog.deleted')}
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
        <Typography.Text type="secondary">{t('content.pages.help')}</Typography.Text>
        <Button type="primary" icon={<PlusOutlined />} onClick={() => navigate('/content/pages/new')}>
          {t('content.pages.add')}
        </Button>
      </Flex>
      {error && <Alert type="error" showIcon title={errorMessage(t, error)} />}
      <Table rowKey="id" columns={columns} dataSource={pages} loading={isFetching} pagination={false} scroll={{ x: true }} locale={{ emptyText: t('content.pages.empty') }} />
    </Flex>
  )
}
