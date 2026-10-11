import { DeleteOutlined, PlusOutlined, ShopOutlined } from '@ant-design/icons'
import { Alert, App, Avatar, Button, Flex, Input, Popconfirm, Segmented, Table, Tag, Tooltip, Typography } from 'antd'
import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Link, useNavigate } from 'react-router'
import { errorMessage } from '@/api/errors'
import { useUrlFilters } from '@/shared/useUrlFilters'
import StoreFormModal from './StoreFormModal'
import { STORE_STATUSES } from './stores'
import { useCreateStoreMutation, useDeleteStoreMutation, useGetStoresQuery } from './storesApi'

/**
 * Stores whose prices customers compare: each with its branches and its products. Staff add stores here and fill in
 * branches and products on the store's page. Filters live in the address (?status=&search=).
 */
export default function StoresPage() {
  const { t } = useTranslation()
  const { message } = App.useApp()
  const navigate = useNavigate()
  const filters = useUrlFilters()
  const status = filters.get('status', 'all')
  const search = filters.get('search', '')
  const { data = [], isLoading, error } = useGetStoresQuery({ search, isActive: status === 'all' ? undefined : status === 'active' })
  const [createStore] = useCreateStoreMutation()
  const [deleteStore] = useDeleteStoreMutation()
  const [adding, setAdding] = useState(false)

  const remove = async (store) => {
    try {
      await deleteStore(store.id).unwrap()
      message.success(t('catalog.deleted'))
    } catch (failure) {
      message.error(errorMessage(t, failure))
    }
  }

  const columns = [
    {
      title: t('stores.columns.name'),
      key: 'name',
      render: (_, store) => (
        <Flex align="center" gap="small">
          <Avatar shape="square" src={store.logoUrl} icon={<ShopOutlined />} alt="" />
          <Flex vertical>
            <Link to={`/stores/${store.id}`}>{store.name}</Link>
            <Typography.Text type="secondary" style={{ fontSize: 12 }}>
              {store.slug}
            </Typography.Text>
          </Flex>
        </Flex>
      ),
    },
    { title: t('stores.columns.branches'), dataIndex: 'branchCount', key: 'branches', align: 'right', responsive: ['md'] },
    { title: t('stores.columns.products'), dataIndex: 'productCount', key: 'products', align: 'right' },
    {
      title: t('stores.columns.linked'),
      key: 'linked',
      align: 'right',
      responsive: ['md'],
      render: (_, store) => (
        <Tooltip title={t('stores.columns.linkedHelp')}>
          <span>{store.linkedCount}</span>
        </Tooltip>
      ),
    },
    {
      title: t('stores.columns.stale'),
      key: 'stale',
      align: 'right',
      render: (_, store) =>
        store.staleCount > 0 ? (
          <Tooltip title={t('stores.columns.staleHelp')}>
            <Tag color="orange">{store.staleCount}</Tag>
          </Tooltip>
        ) : (
          0
        ),
    },
    {
      title: t('catalog.columns.status'),
      key: 'status',
      render: (_, store) => (store.isActive ? <Tag color="green">{t('catalog.status.active')}</Tag> : <Tag>{t('catalog.status.hidden')}</Tag>),
    },
    {
      title: t('catalog.columns.actions'),
      key: 'actions',
      align: 'right',
      render: (_, store) => (
        <Popconfirm
          title={t('stores.deleteConfirm', { name: store.name })}
          description={t('stores.deleteHelp')}
          okText={t('catalog.actions.delete')}
          cancelText={t('catalog.form.cancel')}
          okButtonProps={{ danger: true }}
          onConfirm={() => remove(store)}
        >
          <Button type="text" danger icon={<DeleteOutlined />} aria-label={`${t('catalog.actions.delete')}: ${store.name}`} />
        </Popconfirm>
      ),
    },
  ]

  return (
    <>
      <Typography.Title level={1}>{t('nav.stores')}</Typography.Title>
      <Flex justify="space-between" align="center" gap="middle" wrap style={{ marginBottom: 16 }}>
        <Typography.Text type="secondary">{t('stores.help')}</Typography.Text>
        <Button type="primary" icon={<PlusOutlined />} onClick={() => setAdding(true)}>
          {t('stores.add')}
        </Button>
      </Flex>
      <Flex gap="middle" wrap style={{ marginBottom: 16 }}>
        <Segmented
          aria-label={t('catalog.columns.status')}
          value={status}
          onChange={(value) => filters.update({ status: value === 'all' ? undefined : value })}
          options={STORE_STATUSES.map((value) => ({ value, label: t(`stores.statuses.${value}`) }))}
        />
        <Input.Search
          allowClear
          aria-label={t('stores.search')}
          placeholder={t('stores.search')}
          defaultValue={search}
          onSearch={(value) => filters.update({ search: value.trim() || undefined })}
          style={{ maxWidth: 280 }}
        />
      </Flex>
      {error && <Alert type="error" showIcon title={errorMessage(t, error)} style={{ marginBottom: 16 }} />}
      <Table
        rowKey="id"
        columns={columns}
        dataSource={data}
        loading={isLoading}
        pagination={{ pageSize: 50, hideOnSinglePage: true, showSizeChanger: false }}
        scroll={{ x: 'max-content' }}
        locale={{ emptyText: t('stores.empty') }}
      />
      <StoreFormModal
        open={adding}
        onSave={(body) => createStore(body).unwrap()}
        onDone={(store) => {
          setAdding(false)
          navigate(`/stores/${store.id}`)
        }}
        onClose={() => setAdding(false)}
      />
    </>
  )
}
