import { DeleteOutlined, EditOutlined, EyeInvisibleOutlined, EyeOutlined, LockOutlined, PlusOutlined } from '@ant-design/icons'
import { Alert, App, Button, Flex, Input, Popconfirm, Select, Space, Table, Tag, Tooltip, Typography } from 'antd'
import { useMemo, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { errorMessage } from '@/api/errors'
import { formatMoney } from '@/i18n/format'
import {
  useCreateWorkItemMutation,
  useDeleteWorkItemMutation,
  useGetCategoriesQuery,
  useGetWorkItemsQuery,
  useSetWorkItemActiveMutation,
  useUpdateWorkItemMutation,
} from './catalogApi'
import { localizedName } from './names'
import { useCatalogLanguages } from './useCatalogLanguages'
import WorkItemFormModal from './WorkItemFormModal'
import { categoryLookup, filterWorkItems, hasPrice, marketRangeText, priceRangeText } from './workItems'

const STATUSES = ['all', 'active', 'hidden', 'unpriced']

/**
 * Work items: the units of work partners price and customers estimate ("Wall plastering, per m²"), each under a
 * subcategory, with the usual labour price range. Filter by category, status or text.
 */
export default function WorkItemsTab() {
  const { t, i18n } = useTranslation()
  const { message } = App.useApp()
  const lng = i18n.resolvedLanguage
  const { defaultCode } = useCatalogLanguages()
  const { data = [], isLoading, error } = useGetWorkItemsQuery()
  const { data: categories = [] } = useGetCategoriesQuery()
  const [createWorkItem] = useCreateWorkItemMutation()
  const [updateWorkItem] = useUpdateWorkItemMutation()
  const [setWorkItemActive] = useSetWorkItemActiveMutation()
  const [deleteWorkItem] = useDeleteWorkItemMutation()
  const [categoryFilter, setCategoryFilter] = useState(null)
  const [status, setStatus] = useState('all')
  const [search, setSearch] = useState('')
  // { item } while the dialog is open; item is null when adding.
  const [dialog, setDialog] = useState(null)

  const nameOf = (item) => localizedName(item.name, lng, defaultCode)
  const lookup = useMemo(() => categoryLookup(categories, lng, defaultCode), [categories, lng, defaultCode])
  const rows = useMemo(
    () => filterWorkItems(data, { categoryIds: categoryFilter ? lookup.get(categoryFilter)?.ids ?? [] : null, status, search }),
    [data, categoryFilter, lookup, status, search],
  )

  const categoryOptions = categories.map((main) => ({
    label: localizedName(main.name, lng, defaultCode),
    title: main.slug,
    options: [
      { value: main.id, label: t('catalog.workItems.allIn', { name: localizedName(main.name, lng, defaultCode) }) },
      ...(main.children ?? []).map((sub) => ({ value: sub.id, label: localizedName(sub.name, lng, defaultCode) })),
    ],
  }))

  const run = async (action, done) => {
    try {
      await action().unwrap()
      message.success(done)
    } catch (failure) {
      message.error(errorMessage(t, failure))
    }
  }

  const save = (body) => (dialog?.item ? updateWorkItem({ id: dialog.item.id, ...body }) : createWorkItem(body)).unwrap()

  const columns = [
    {
      title: t('catalog.columns.name'),
      key: 'name',
      render: (_, item) => (
        <Flex vertical>
          <Typography.Text>{nameOf(item)}</Typography.Text>
          <Typography.Text type="secondary" style={{ fontSize: 12 }}>
            {item.slug}
          </Typography.Text>
        </Flex>
      ),
    },
    {
      title: t('catalog.workItems.category'),
      key: 'category',
      responsive: ['md'],
      render: (_, item) => {
        const category = lookup.get(item.categoryId)
        if (!category) return '—'
        return (
          <Flex vertical>
            <Typography.Text>{category.name}</Typography.Text>
            {category.main && (
              <Typography.Text type="secondary" style={{ fontSize: 12 }}>
                {category.main}
              </Typography.Text>
            )}
          </Flex>
        )
      },
    },
    {
      title: t('catalog.workItems.unit'),
      key: 'unit',
      width: 110,
      render: (_, item) => <Tag>{t(`catalog.workItems.units.${item.unit}`)}</Tag>,
    },
    {
      title: t('catalog.workItems.price'),
      key: 'price',
      render: (_, item) =>
        hasPrice(item) ? (
          <Flex vertical>
            <Space size={4}>
              <Typography.Text strong>{formatMoney(item.priceTypical, lng)}</Typography.Text>
              {item.isPriceLocked && (
                <Tooltip title={t('catalog.workItems.locked')}>
                  <LockOutlined aria-label={t('catalog.workItems.locked')} />
                </Tooltip>
              )}
            </Space>
            <Typography.Text type="secondary" style={{ fontSize: 12 }}>
              {priceRangeText(item, lng)}
            </Typography.Text>
            {item.marketSource === 'Partners' && (
              <Tooltip title={t('catalog.workItems.marketHelp', { count: item.marketPartnerCount })}>
                <Tag color="blue" style={{ marginTop: 4, width: 'fit-content' }}>
                  {t('catalog.workItems.market', { range: marketRangeText(item, lng) })}
                </Tag>
              </Tooltip>
            )}
          </Flex>
        ) : (
          <Tag color="orange">{t('catalog.workItems.noPrice')}</Tag>
        ),
    },
    {
      title: t('catalog.workItems.partners'),
      key: 'partners',
      width: 100,
      responsive: ['md'],
      render: (_, item) => (
        <Tooltip title={t('catalog.workItems.partnersHelp')}>
          <span>{item.partnerCount ?? 0}</span>
        </Tooltip>
      ),
    },
    { title: t('catalog.columns.sortOrder'), dataIndex: 'sortOrder', key: 'sortOrder', width: 80, responsive: ['lg'] },
    {
      title: t('catalog.columns.status'),
      key: 'status',
      width: 110,
      render: (_, item) =>
        item.isActive ? <Tag color="green">{t('catalog.status.active')}</Tag> : <Tag>{t('catalog.status.hidden')}</Tag>,
    },
    {
      title: t('catalog.columns.actions'),
      key: 'actions',
      align: 'right',
      render: (_, item) => {
        const name = nameOf(item)
        const toggleLabel = t(item.isActive ? 'catalog.actions.hide' : 'catalog.actions.show')
        return (
          <Space size={0}>
            <Tooltip title={t('catalog.actions.edit')}>
              <Button type="text" icon={<EditOutlined />} aria-label={`${t('catalog.actions.edit')}: ${name}`} onClick={() => setDialog({ item })} />
            </Tooltip>
            <Tooltip title={toggleLabel}>
              <Button
                type="text"
                icon={item.isActive ? <EyeInvisibleOutlined /> : <EyeOutlined />}
                aria-label={`${toggleLabel}: ${name}`}
                onClick={() =>
                  run(
                    () => setWorkItemActive({ id: item.id, isActive: !item.isActive }),
                    t(item.isActive ? 'catalog.hiddenDone' : 'catalog.shownDone'),
                  )
                }
              />
            </Tooltip>
            <Popconfirm
              title={t('catalog.workItems.deleteConfirm', { name })}
              description={t('catalog.workItems.deleteHelp')}
              okText={t('catalog.actions.delete')}
              cancelText={t('catalog.form.cancel')}
              okButtonProps={{ danger: true }}
              onConfirm={() => run(() => deleteWorkItem(item.id), t('catalog.deleted'))}
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
        <Typography.Text type="secondary">{t('catalog.workItems.help')}</Typography.Text>
        <Button type="primary" icon={<PlusOutlined />} onClick={() => setDialog({ item: null })}>
          {t('catalog.workItems.add')}
        </Button>
      </Flex>
      <Flex gap="middle" wrap style={{ marginBottom: 16 }}>
        <Select
          aria-label={t('catalog.workItems.category')}
          style={{ minWidth: 260 }}
          showSearch
          optionFilterProp="label"
          value={categoryFilter}
          onChange={setCategoryFilter}
          options={[{ value: null, label: t('catalog.workItems.allCategories') }, ...categoryOptions]}
        />
        <Select
          aria-label={t('catalog.columns.status')}
          style={{ minWidth: 160 }}
          value={status}
          onChange={setStatus}
          options={STATUSES.map((value) => ({ value, label: t(`catalog.workItems.statuses.${value}`) }))}
        />
        <Input.Search
          allowClear
          aria-label={t('catalog.workItems.search')}
          placeholder={t('catalog.workItems.search')}
          style={{ maxWidth: 280 }}
          value={search}
          onChange={(event) => setSearch(event.target.value)}
        />
        <Typography.Text type="secondary" style={{ alignSelf: 'center' }}>
          {t('catalog.workItems.count', { shown: rows.length, total: data.length })}
        </Typography.Text>
      </Flex>
      {error && <Alert type="error" showIcon title={errorMessage(t, error)} style={{ marginBottom: 16 }} />}
      <Table
        rowKey="id"
        columns={columns}
        dataSource={rows}
        loading={isLoading}
        pagination={{ pageSize: 50, hideOnSinglePage: true, showSizeChanger: false }}
        scroll={{ x: 'max-content' }}
        locale={{ emptyText: t('catalog.workItems.empty') }}
      />
      <WorkItemFormModal
        open={Boolean(dialog)}
        item={dialog?.item ?? null}
        categories={categories}
        defaultCategoryId={lookup.get(categoryFilter)?.main ? categoryFilter : null}
        onSave={save}
        onClose={() => setDialog(null)}
      />
    </>
  )
}
