import { CheckCircleOutlined, DeleteOutlined, EditOutlined, PlusOutlined } from '@ant-design/icons'
import { Alert, App, Avatar, Button, Checkbox, DatePicker, Flex, Input, InputNumber, Popconfirm, Select, Space, Switch, Table, Tag, Tooltip, Typography } from 'antd'
import dayjs from 'dayjs'
import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { errorMessage } from '@/api/errors'
import { formatDate, formatMoney, formatUnitPrice } from '@/i18n/format'
import { useUrlFilters } from '@/shared/useUrlFilters'
import ProductFormModal from './ProductFormModal'
import { draftProblem, isChanged, isPending, toDraft, toItems } from './quickPrices'
import { GRADE_COLORS, PRICE_LIMIT } from './stores'
import {
  useConfirmStoreProductPriceMutation,
  useCreateStoreProductMutation,
  useDeleteStoreProductMutation,
  useGetStoreProductsQuery,
  useSaveStoreProductPricesMutation,
  useUpdateStoreProductMutation,
} from './storesApi'

const SHOWN = ['all', 'linked', 'unlinked', 'stale']

/**
 * The store's products with the price each has today. Filters live in the address (?search=&show=&page=). "Quick edit"
 * (R7) turns the price, sale and stock of the rows on the page into inputs, saved together with "Save all".
 */
export default function ProductsTab({ store }) {
  const { t, i18n } = useTranslation()
  const { message, modal } = App.useApp()
  const language = i18n.resolvedLanguage
  const filters = useUrlFilters()
  const search = filters.get('search', '')
  const show = filters.get('show', 'all')
  const { data, isLoading, error } = useGetStoreProductsQuery({
    storeId: store.id,
    search,
    linked: show === 'linked' ? true : show === 'unlinked' ? false : undefined,
    stale: show === 'stale' ? true : undefined,
    page: filters.page,
  })
  const [createProduct] = useCreateStoreProductMutation()
  const [updateProduct] = useUpdateStoreProductMutation()
  const [deleteProduct] = useDeleteStoreProductMutation()
  const [confirmPrice] = useConfirmStoreProductPriceMutation()
  const [savePrices, saving] = useSaveStoreProductPricesMutation()
  const [dialog, setDialog] = useState(null)
  const [quick, setQuick] = useState(false)
  const [drafts, setDrafts] = useState({})

  const products = data?.items ?? []
  const draftOf = (product) => drafts[product.id] ?? toDraft(product)
  const pending = products.filter((product) => isPending(product, drafts[product.id]))
  const editDraft = (product, changes) => setDrafts((current) => ({ ...current, [product.id]: { ...draftOf(product), ...changes } }))
  const discard = () => setDrafts({})

  // With unsaved quick edits, leaving the page of rows (filters, search, paging, quick edit off) asks first.
  const guarded = (action) => () => {
    if (pending.length === 0) return action()
    modal.confirm({
      title: t('stores.quickEdit.leaveConfirm', { count: pending.length }),
      okText: t('stores.quickEdit.leave'),
      cancelText: t('catalog.form.cancel'),
      onOk: () => {
        discard()
        action()
      },
    })
    return undefined
  }
  const changeFilters = (changes) => guarded(() => filters.update(changes))()

  const saveAll = async () => {
    if (pending.some((product) => draftProblem(drafts[product.id]))) {
      message.error(t('stores.quickEdit.fixRows'))
      return
    }
    try {
      const saved = await savePrices({ storeId: store.id, items: toItems(products, drafts) }).unwrap()
      message.success(t('stores.quickEdit.saved', { count: saved.length }))
      discard()
      setQuick(false)
    } catch (failure) {
      message.error(errorMessage(t, failure))
    }
  }

  const save = (body) => (dialog?.product ? updateProduct({ storeId: store.id, productId: dialog.product.id, ...body }) : createProduct({ storeId: store.id, ...body })).unwrap()

  const run = async (action, success) => {
    try {
      await action()
      if (success) message.success(t(success))
    } catch (failure) {
      message.error(errorMessage(t, failure))
    }
  }

  const columns = [
    {
      title: t('stores.product.name'),
      key: 'name',
      render: (_, product) => (
        <Flex align="center" gap="small">
          <Avatar shape="square" src={product.photoUrl} alt="" />
          <Flex vertical>
            <Typography.Text>{product.name}</Typography.Text>
            <Typography.Text type="secondary" style={{ fontSize: 12 }}>
              {[product.brand, product.code].filter(Boolean).join(' · ')}
            </Typography.Text>
          </Flex>
        </Flex>
      ),
    },
    {
      title: t('stores.product.material'),
      key: 'material',
      render: (_, product) =>
        product.materialId ? (
          <Flex gap="small" align="center" wrap>
            <span>{product.materialName}</span>
            <Tag color={GRADE_COLORS[product.grade]}>{t(`stores.grades.${product.grade}`)}</Tag>
          </Flex>
        ) : (
          <Typography.Text type="secondary">{t('stores.product.notLinked')}</Typography.Text>
        ),
    },
    {
      title: t('stores.product.pack'),
      key: 'pack',
      responsive: ['md'],
      render: (_, product) => product.packLabel || `${product.packSize}${product.unit ? ` ${t(`catalog.materials.units.${product.unit}`)}` : ''}`,
    },
    {
      title: t('stores.product.price'),
      key: 'price',
      align: 'right',
      render: (_, product) =>
        product.saleActive ? (
          <Flex vertical align="flex-end">
            <span>{formatMoney(product.salePrice, language)}</span>
            <Typography.Text type="secondary" delete style={{ fontSize: 12 }}>
              {formatMoney(product.price, language)}
            </Typography.Text>
          </Flex>
        ) : (
          formatMoney(product.price, language)
        ),
    },
    {
      title: t('stores.product.unitPrice'),
      key: 'unitPrice',
      align: 'right',
      responsive: ['md'],
      render: (_, product) => (product.unitPrice == null ? '—' : formatUnitPrice(product.unitPrice, language)),
    },
    {
      title: t('stores.product.stock'),
      key: 'stock',
      render: (_, product) => (
        <Flex gap={4} wrap>
          <Tag color={product.inStock ? 'green' : 'red'}>{t(product.inStock ? 'stores.product.inStock' : 'stores.product.outOfStock')}</Tag>
          {!product.isActive && <Tag>{t('catalog.status.hidden')}</Tag>}
        </Flex>
      ),
    },
    {
      title: t('stores.product.updated'),
      key: 'updated',
      render: (_, product) => (
        <Flex vertical>
          <span>{formatDate(product.priceUpdatedAt, language)}</span>
          {product.isStale && (
            <Tag color="orange" style={{ width: 'fit-content' }}>
              {t('stores.product.stale')}
            </Tag>
          )}
        </Flex>
      ),
    },
    {
      title: t('catalog.columns.actions'),
      key: 'actions',
      align: 'right',
      render: (_, product) => (
        <Space size={0}>
          <Tooltip title={t('stores.product.confirmPrice')}>
            <Button
              type="text"
              icon={<CheckCircleOutlined />}
              aria-label={`${t('stores.product.confirmPrice')}: ${product.name}`}
              onClick={() => run(() => confirmPrice({ storeId: store.id, productId: product.id }).unwrap(), 'stores.product.priceConfirmed')}
            />
          </Tooltip>
          <Tooltip title={t('catalog.actions.edit')}>
            <Button type="text" icon={<EditOutlined />} aria-label={`${t('catalog.actions.edit')}: ${product.name}`} onClick={() => setDialog({ product })} />
          </Tooltip>
          <Popconfirm
            title={t('stores.product.deleteConfirm', { name: product.name })}
            okText={t('catalog.actions.delete')}
            cancelText={t('catalog.form.cancel')}
            okButtonProps={{ danger: true }}
            onConfirm={() => run(() => deleteProduct({ storeId: store.id, productId: product.id }).unwrap(), 'catalog.deleted')}
          >
            <Button type="text" danger icon={<DeleteOutlined />} aria-label={`${t('catalog.actions.delete')}: ${product.name}`} />
          </Popconfirm>
        </Space>
      ),
    },
  ]

  const quickColumns = [
    columns[0],
    {
      title: t('stores.product.price'),
      key: 'price',
      render: (_, product) => {
        const draft = draftOf(product)
        const problem = isPending(product, drafts[product.id]) ? draftProblem(draft) : null
        return (
          <Flex vertical gap={4}>
            <InputNumber
              aria-label={`${t('stores.product.price')}: ${product.name}`}
              min={1}
              max={PRICE_LIMIT}
              precision={0}
              suffix="֏"
              status={problem ? 'error' : undefined}
              value={draft.price}
              onChange={(value) => editDraft(product, { price: value })}
              style={{ width: 140 }}
            />
            {problem && (
              <Typography.Text type="danger" style={{ fontSize: 12 }}>
                {t(problem)}
              </Typography.Text>
            )}
          </Flex>
        )
      },
    },
    {
      title: t('stores.product.salePrice'),
      key: 'salePrice',
      render: (_, product) => (
        <InputNumber
          aria-label={`${t('stores.product.salePrice')}: ${product.name}`}
          min={1}
          max={PRICE_LIMIT}
          precision={0}
          suffix="֏"
          value={draftOf(product).salePrice}
          onChange={(value) => editDraft(product, { salePrice: value ?? null })}
          style={{ width: 140 }}
        />
      ),
    },
    {
      title: t('stores.product.saleEndsOn'),
      key: 'saleEndsOn',
      render: (_, product) => {
        const ends = draftOf(product).saleEndsOn
        return (
          <DatePicker
            aria-label={`${t('stores.product.saleEndsOn')}: ${product.name}`}
            value={ends ? dayjs(ends) : null}
            onChange={(date) => editDraft(product, { saleEndsOn: date ? date.format('YYYY-MM-DD') : null })}
            style={{ width: 150 }}
          />
        )
      },
    },
    {
      title: t('stores.product.stock'),
      key: 'stock',
      render: (_, product) => (
        <Switch
          aria-label={`${t('stores.product.inStock')}: ${product.name}`}
          checked={draftOf(product).inStock}
          onChange={(checked) => editDraft(product, { inStock: checked })}
        />
      ),
    },
    {
      title: t('stores.quickEdit.checked'),
      key: 'checked',
      render: (_, product) => {
        const draft = draftOf(product)
        return (
          <Flex vertical>
            <Checkbox
              aria-label={`${t('stores.quickEdit.checked')}: ${product.name}`}
              checked={draft.confirm || isChanged(product, draft)}
              disabled={isChanged(product, draft)}
              onChange={(event) => editDraft(product, { confirm: event.target.checked })}
            />
            <Typography.Text type="secondary" style={{ fontSize: 12 }}>
              {formatDate(product.priceUpdatedAt, language)}
            </Typography.Text>
          </Flex>
        )
      },
    },
  ]

  return (
    <>
      <Flex justify="space-between" align="center" gap="middle" wrap style={{ marginBottom: 16 }}>
        <Typography.Text type="secondary">{t('stores.product.help')}</Typography.Text>
        <Flex gap="middle" align="center" wrap>
          <Flex gap="small" align="center">
            <Switch
              id="quick-edit"
              checked={quick}
              onChange={(checked) => (checked ? setQuick(true) : guarded(() => setQuick(false))())}
            />
            <label htmlFor="quick-edit">{t('stores.quickEdit.toggle')}</label>
          </Flex>
          <Button type="primary" icon={<PlusOutlined />} onClick={() => setDialog({ product: null })}>
            {t('stores.product.add')}
          </Button>
        </Flex>
      </Flex>
      <Flex gap="middle" wrap style={{ marginBottom: 16 }}>
        <Select
          aria-label={t('stores.product.show')}
          style={{ minWidth: 180 }}
          value={show}
          onChange={(value) => changeFilters({ show: value === 'all' ? undefined : value })}
          options={SHOWN.map((value) => ({ value, label: t(`stores.product.shown.${value}`) }))}
        />
        <Input.Search
          allowClear
          aria-label={t('stores.product.search')}
          placeholder={t('stores.product.search')}
          defaultValue={search}
          onSearch={(value) => changeFilters({ search: value.trim() || undefined })}
          style={{ maxWidth: 280 }}
        />
      </Flex>
      {error && <Alert type="error" showIcon title={errorMessage(t, error)} style={{ marginBottom: 16 }} />}
      {quick && pending.length > 0 && (
        <Flex
          role="region"
          aria-label={t('stores.quickEdit.changes', { count: pending.length })}
          justify="space-between"
          align="center"
          gap="middle"
          wrap
          style={{ position: 'sticky', top: 0, zIndex: 2, marginBottom: 16, padding: '8px 16px', background: '#fffbe6', border: '1px solid #ffe58f', borderRadius: 8 }}
        >
          <Typography.Text strong>{t('stores.quickEdit.changes', { count: pending.length })}</Typography.Text>
          <Space>
            <Button onClick={discard}>{t('stores.quickEdit.discard')}</Button>
            <Button type="primary" loading={saving.isLoading} onClick={saveAll}>
              {t('stores.quickEdit.save')}
            </Button>
          </Space>
        </Flex>
      )}
      <Table
        rowKey="id"
        columns={quick ? quickColumns : columns}
        dataSource={products}
        onRow={(product) => (quick && isPending(product, drafts[product.id]) ? { style: { background: '#fffbe6' } } : {})}
        loading={isLoading}
        pagination={{
          current: filters.page,
          pageSize: data?.pageSize ?? 25,
          total: data?.totalCount ?? 0,
          showSizeChanger: false,
          hideOnSinglePage: true,
          onChange: (page) => changeFilters({ page: page > 1 ? page : undefined }),
        }}
        scroll={{ x: 'max-content' }}
        locale={{ emptyText: t('stores.product.empty') }}
      />
      <ProductFormModal open={Boolean(dialog)} product={dialog?.product ?? null} onSave={save} onClose={() => setDialog(null)} />
    </>
  )
}
