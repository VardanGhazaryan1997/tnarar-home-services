import { CheckCircleOutlined, DeleteOutlined, EditOutlined, PlusOutlined } from '@ant-design/icons'
import { Alert, App, Avatar, Button, Flex, Input, Popconfirm, Select, Space, Table, Tag, Tooltip, Typography } from 'antd'
import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { errorMessage } from '@/api/errors'
import { formatDate, formatMoney, formatUnitPrice } from '@/i18n/format'
import { useUrlFilters } from '@/shared/useUrlFilters'
import ProductFormModal from './ProductFormModal'
import { GRADE_COLORS } from './stores'
import {
  useConfirmStoreProductPriceMutation,
  useCreateStoreProductMutation,
  useDeleteStoreProductMutation,
  useGetStoreProductsQuery,
  useUpdateStoreProductMutation,
} from './storesApi'

const SHOWN = ['all', 'linked', 'unlinked', 'stale']

/** The store's products with the price each has today. Filters live in the address (?search=&show=&page=). */
export default function ProductsTab({ store }) {
  const { t, i18n } = useTranslation()
  const { message } = App.useApp()
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
  const [dialog, setDialog] = useState(null)

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

  return (
    <>
      <Flex justify="space-between" align="center" gap="middle" wrap style={{ marginBottom: 16 }}>
        <Typography.Text type="secondary">{t('stores.product.help')}</Typography.Text>
        <Button type="primary" icon={<PlusOutlined />} onClick={() => setDialog({ product: null })}>
          {t('stores.product.add')}
        </Button>
      </Flex>
      <Flex gap="middle" wrap style={{ marginBottom: 16 }}>
        <Select
          aria-label={t('stores.product.show')}
          style={{ minWidth: 180 }}
          value={show}
          onChange={(value) => filters.update({ show: value === 'all' ? undefined : value })}
          options={SHOWN.map((value) => ({ value, label: t(`stores.product.shown.${value}`) }))}
        />
        <Input.Search
          allowClear
          aria-label={t('stores.product.search')}
          placeholder={t('stores.product.search')}
          defaultValue={search}
          onSearch={(value) => filters.update({ search: value.trim() || undefined })}
          style={{ maxWidth: 280 }}
        />
      </Flex>
      {error && <Alert type="error" showIcon title={errorMessage(t, error)} style={{ marginBottom: 16 }} />}
      <Table
        rowKey="id"
        columns={columns}
        dataSource={data?.items ?? []}
        loading={isLoading}
        pagination={{
          current: filters.page,
          pageSize: data?.pageSize ?? 25,
          total: data?.totalCount ?? 0,
          showSizeChanger: false,
          hideOnSinglePage: true,
          onChange: (page) => filters.update({ page: page > 1 ? page : undefined }),
        }}
        scroll={{ x: 'max-content' }}
        locale={{ emptyText: t('stores.product.empty') }}
      />
      <ProductFormModal open={Boolean(dialog)} product={dialog?.product ?? null} onSave={save} onClose={() => setDialog(null)} />
    </>
  )
}
