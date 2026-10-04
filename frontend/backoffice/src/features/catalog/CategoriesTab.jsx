import { DeleteOutlined, EditOutlined, EyeInvisibleOutlined, EyeOutlined, PlusOutlined } from '@ant-design/icons'
import { Alert, App, Button, Flex, Popconfirm, Space, Table, Tag, Tooltip, Typography } from 'antd'
import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { errorMessage } from '@/api/errors'
import { useDeleteCategoryMutation, useGetCategoriesQuery, useSetCategoryActiveMutation } from './catalogApi'
import CategoryFormModal from './CategoryFormModal'
import { localizedName } from './names'
import { useCatalogLanguages } from './useCatalogLanguages'

// Ant Design shows an expand arrow for any `children`, so leaves get none.
const toRows = (nodes) =>
  nodes.map(({ children, ...node }) => ({
    ...node,
    key: node.id,
    ...(children.length > 0 ? { children: toRows(children) } : {}),
  }))

/** Service categories as an editable two-level tree. */
export default function CategoriesTab() {
  const { t, i18n } = useTranslation()
  const { message } = App.useApp()
  const { defaultCode } = useCatalogLanguages()
  const { data = [], isLoading, error } = useGetCategoriesQuery()
  const [setActive] = useSetCategoryActiveMutation()
  const [deleteCategory] = useDeleteCategoryMutation()
  const [dialog, setDialog] = useState({ open: false, category: null, parentId: null })
  // Parents are expanded unless the staff member collapsed them.
  const [collapsed, setCollapsed] = useState(() => new Set())

  const rows = toRows(data)
  const expandedRowKeys = rows.filter((row) => row.children && !collapsed.has(row.id)).map((row) => row.id)
  const handleExpand = (expanded, row) =>
    setCollapsed((current) => {
      const next = new Set(current)
      if (expanded) next.delete(row.id)
      else next.add(row.id)
      return next
    })
  const openDialog = (category = null, parentId = null) => setDialog({ open: true, category, parentId })
  const closeDialog = () => setDialog((current) => ({ ...current, open: false }))

  const run = async (action, success) => {
    try {
      await action().unwrap()
      message.success(success)
    } catch (failure) {
      message.error(errorMessage(t, failure))
    }
  }

  const columns = [
    {
      title: t('catalog.columns.name'),
      key: 'name',
      render: (_, row) => (
        <Flex vertical>
          <Typography.Text strong={!row.parentId}>{localizedName(row.name, i18n.resolvedLanguage, defaultCode)}</Typography.Text>
          <Typography.Text type="secondary" style={{ fontSize: 12 }}>
            {row.slug}
          </Typography.Text>
        </Flex>
      ),
    },
    {
      title: t('catalog.categories.icon'),
      dataIndex: 'icon',
      key: 'icon',
      responsive: ['md'],
      render: (icon) => icon || '—',
    },
    {
      title: t('catalog.columns.sortOrder'),
      dataIndex: 'sortOrder',
      key: 'sortOrder',
      responsive: ['sm'],
      width: 90,
    },
    {
      title: t('catalog.columns.status'),
      key: 'status',
      width: 110,
      render: (_, row) =>
        row.isActive ? <Tag color="green">{t('catalog.status.active')}</Tag> : <Tag>{t('catalog.status.hidden')}</Tag>,
    },
    {
      title: t('catalog.columns.actions'),
      key: 'actions',
      align: 'right',
      render: (_, row) => {
        const name = localizedName(row.name, i18n.resolvedLanguage, defaultCode)
        return (
          <Space size={0} wrap>
            <Tooltip title={t('catalog.actions.edit')}>
              <Button type="text" icon={<EditOutlined />} aria-label={`${t('catalog.actions.edit')}: ${name}`} onClick={() => openDialog(row)} />
            </Tooltip>
            {!row.parentId && (
              <Tooltip title={t('catalog.categories.addSub')}>
                <Button
                  type="text"
                  icon={<PlusOutlined />}
                  aria-label={`${t('catalog.categories.addSub')}: ${name}`}
                  onClick={() => openDialog(null, row.id)}
                />
              </Tooltip>
            )}
            <Tooltip title={t(row.isActive ? 'catalog.actions.hide' : 'catalog.actions.show')}>
              <Button
                type="text"
                icon={row.isActive ? <EyeInvisibleOutlined /> : <EyeOutlined />}
                aria-label={`${t(row.isActive ? 'catalog.actions.hide' : 'catalog.actions.show')}: ${name}`}
                onClick={() =>
                  run(
                    () => setActive({ id: row.id, isActive: !row.isActive }),
                    t(row.isActive ? 'catalog.hiddenDone' : 'catalog.shownDone'),
                  )
                }
              />
            </Tooltip>
            <Popconfirm
              title={t('catalog.categories.deleteConfirm', { name })}
              description={t('catalog.categories.deleteHelp')}
              okText={t('catalog.actions.delete')}
              cancelText={t('catalog.form.cancel')}
              okButtonProps={{ danger: true }}
              onConfirm={() => run(() => deleteCategory(row.id), t('catalog.deleted'))}
            >
              <Button type="text" danger icon={<DeleteOutlined />} aria-label={`${t('catalog.actions.delete')}: ${name}`} />
            </Popconfirm>
          </Space>
        )
      },
    },
  ]

  return (
    <>
      <Flex justify="space-between" align="center" gap="middle" wrap style={{ marginBottom: 16 }}>
        <Typography.Text type="secondary">{t('catalog.categories.help')}</Typography.Text>
        <Button type="primary" icon={<PlusOutlined />} onClick={() => openDialog()}>
          {t('catalog.categories.add')}
        </Button>
      </Flex>
      {error && <Alert type="error" showIcon title={errorMessage(t, error)} style={{ marginBottom: 16 }} />}
      <Table
        rowKey="id"
        columns={columns}
        dataSource={rows}
        loading={isLoading}
        pagination={false}
        expandable={{ expandedRowKeys, onExpand: handleExpand }}
        scroll={{ x: 'max-content' }}
        locale={{ emptyText: t('catalog.categories.empty') }}
      />
      <CategoryFormModal
        open={dialog.open}
        category={dialog.category}
        parentId={dialog.parentId}
        topLevel={data}
        onClose={closeDialog}
      />
    </>
  )
}
