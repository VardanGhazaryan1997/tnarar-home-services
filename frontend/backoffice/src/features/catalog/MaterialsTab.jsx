import { DeleteOutlined, EditOutlined, PlusOutlined } from '@ant-design/icons'
import { Alert, App, Button, Flex, Input, Popconfirm, Select, Space, Table, Tag, Tooltip, Typography } from 'antd'
import { useMemo, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { errorMessage } from '@/api/errors'
import { formatMoney } from '@/i18n/format'
import { useCreateMaterialMutation, useDeleteMaterialMutation, useGetMaterialsQuery, useUpdateMaterialMutation } from './catalogApi'
import MaterialFormModal from './MaterialFormModal'
import { filterMaterials } from './materials'
import { localizedName } from './names'
import { useCatalogLanguages } from './useCatalogLanguages'

const STATUSES = ['all', 'active', 'hidden', 'unused']

/**
 * Materials the estimator counts (E1): each with a unit and a usual price per unit for budget, standard and premium
 * materials. Which work uses how much of them is set on the work items.
 */
export default function MaterialsTab() {
  const { t, i18n } = useTranslation()
  const { message } = App.useApp()
  const lng = i18n.resolvedLanguage
  const { defaultCode } = useCatalogLanguages()
  const { data = [], isLoading, error } = useGetMaterialsQuery()
  const [createMaterial] = useCreateMaterialMutation()
  const [updateMaterial] = useUpdateMaterialMutation()
  const [deleteMaterial] = useDeleteMaterialMutation()
  const [status, setStatus] = useState('all')
  const [search, setSearch] = useState('')
  // { material } while the dialog is open; material is null when adding.
  const [dialog, setDialog] = useState(null)

  const rows = useMemo(() => filterMaterials(data, { search, status }), [data, search, status])
  const nameOf = (material) => localizedName(material.name, lng, defaultCode)
  const save = (body) => (dialog?.material ? updateMaterial({ id: dialog.material.id, ...body }) : createMaterial(body)).unwrap()

  const remove = async (material) => {
    try {
      await deleteMaterial(material.id).unwrap()
      message.success(t('catalog.deleted'))
    } catch (failure) {
      message.error(errorMessage(t, failure))
    }
  }

  const columns = [
    {
      title: t('catalog.columns.name'),
      key: 'name',
      render: (_, material) => (
        <Flex vertical>
          <Typography.Text>{nameOf(material)}</Typography.Text>
          <Typography.Text type="secondary" style={{ fontSize: 12 }}>
            {material.slug}
          </Typography.Text>
        </Flex>
      ),
    },
    {
      title: t('catalog.materials.unit'),
      key: 'unit',
      width: 110,
      render: (_, material) => <Tag>{t(`catalog.materials.units.${material.unit}`)}</Tag>,
    },
    ...['priceBudget', 'priceStandard', 'pricePremium'].map((field) => ({
      title: t(`catalog.materials.${field}`),
      key: field,
      align: 'right',
      render: (_, material) => formatMoney(material[field], lng),
    })),
    {
      title: t('catalog.materials.usedBy'),
      key: 'usedBy',
      width: 110,
      responsive: ['md'],
      render: (_, material) => (
        <Tooltip title={t('catalog.materials.usedByHelp')}>
          <span>{material.workItemCount ?? 0}</span>
        </Tooltip>
      ),
    },
    {
      title: t('catalog.columns.status'),
      key: 'status',
      width: 110,
      render: (_, material) =>
        material.isActive ? <Tag color="green">{t('catalog.status.active')}</Tag> : <Tag>{t('catalog.status.hidden')}</Tag>,
    },
    {
      title: t('catalog.columns.actions'),
      key: 'actions',
      align: 'right',
      render: (_, material) => {
        const name = nameOf(material)
        return (
          <Space size={0}>
            <Tooltip title={t('catalog.actions.edit')}>
              <Button type="text" icon={<EditOutlined />} aria-label={`${t('catalog.actions.edit')}: ${name}`} onClick={() => setDialog({ material })} />
            </Tooltip>
            <Popconfirm
              title={t('catalog.materials.deleteConfirm', { name })}
              description={t('catalog.materials.deleteHelp')}
              okText={t('catalog.actions.delete')}
              cancelText={t('catalog.form.cancel')}
              okButtonProps={{ danger: true }}
              onConfirm={() => remove(material)}
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
        <Typography.Text type="secondary">{t('catalog.materials.help')}</Typography.Text>
        <Button type="primary" icon={<PlusOutlined />} onClick={() => setDialog({ material: null })}>
          {t('catalog.materials.add')}
        </Button>
      </Flex>
      <Flex gap="middle" wrap style={{ marginBottom: 16 }}>
        <Select
          aria-label={t('catalog.columns.status')}
          style={{ minWidth: 160 }}
          value={status}
          onChange={setStatus}
          options={STATUSES.map((value) => ({ value, label: t(`catalog.materials.statuses.${value}`) }))}
        />
        <Input.Search
          allowClear
          aria-label={t('catalog.materials.search')}
          placeholder={t('catalog.materials.search')}
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
        locale={{ emptyText: t('catalog.materials.empty') }}
      />
      <MaterialFormModal open={Boolean(dialog)} material={dialog?.material ?? null} onSave={save} onClose={() => setDialog(null)} />
    </>
  )
}
