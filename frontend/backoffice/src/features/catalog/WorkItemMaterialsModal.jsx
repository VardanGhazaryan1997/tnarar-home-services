import { DeleteOutlined } from '@ant-design/icons'
import { Alert, App, Button, Flex, InputNumber, Modal, Select, Table, Typography } from 'antd'
import { useMemo, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { errorMessage } from '@/api/errors'
import { useGetMaterialsQuery, useGetWorkItemMaterialsQuery, useSetWorkItemMaterialsMutation } from './catalogApi'
import {
  MAX_MATERIALS_PER_ITEM,
  MAX_QUANTITY_PER_UNIT,
  MAX_WASTE,
  amountFor,
  materialLineProblem,
  toMaterialInputs,
  toMaterialLines,
} from './materials'
import { localizedName } from './names'
import { useCatalogLanguages } from './useCatalogLanguages'

/** How much work the example under the table shows: 10 m², 10 m, 10 pieces… */
const EXAMPLE_QUANTITY = 10

/**
 * The materials a work item uses (E1): how much of each per unit of work, plus waste in percent. The estimator
 * multiplies them by the work's quantity and prices them for the grade the customer picks. `item` is the work item
 * (null while closed).
 */
export default function WorkItemMaterialsModal({ item, onClose }) {
  const { t, i18n } = useTranslation()
  const { message } = App.useApp()
  const lng = i18n.resolvedLanguage
  const { defaultCode } = useCatalogLanguages()
  const saved = useGetWorkItemMaterialsQuery(item?.id, { skip: !item })
  const { data: materials = [] } = useGetMaterialsQuery(undefined, { skip: !item })
  const [setWorkItemMaterials, saving] = useSetWorkItemMaterialsMutation()
  // Unsaved lines; null shows what's saved.
  const [edited, setEdited] = useState(null)

  const byId = useMemo(() => new Map(materials.map((material) => [material.id, material])), [materials])
  const lines = edited ?? toMaterialLines(saved.data)
  const problems = lines.map(materialLineProblem)
  const nameOf = (material) => (material ? localizedName(material.name, lng, defaultCode) : '—')
  const workUnit = item ? t(`catalog.workItems.units.${item.unit}`) : ''
  const unitOf = (material) => (material ? t(`catalog.materials.units.${material.unit}`) : '')
  const update = (index, patch) => setEdited(lines.map((line, i) => (i === index ? { ...line, ...patch } : line)))

  const close = () => {
    setEdited(null)
    onClose()
  }

  const save = async () => {
    try {
      await setWorkItemMaterials({ id: item.id, items: toMaterialInputs(lines) }).unwrap()
      message.success(t('catalog.saved'))
      close()
    } catch (failure) {
      message.error(errorMessage(t, failure))
    }
  }

  const used = new Set(lines.map((line) => line.materialId))
  const addOptions = materials
    .filter((material) => !used.has(material.id))
    .map((material) => ({ value: material.id, label: `${nameOf(material)} · ${unitOf(material)}`, title: material.slug }))

  const columns = [
    {
      title: t('catalog.workItemMaterials.material'),
      key: 'material',
      render: (_, line) => {
        const material = byId.get(line.materialId)
        return (
          <Flex vertical>
            <Typography.Text>{nameOf(material)}</Typography.Text>
            {material && !material.isActive && (
              <Typography.Text type="secondary" style={{ fontSize: 12 }}>
                {t('catalog.workItemMaterials.hiddenHelp')}
              </Typography.Text>
            )}
          </Flex>
        )
      },
    },
    {
      title: t('catalog.workItemMaterials.amount'),
      key: 'amount',
      width: 200,
      render: (_, line, index) => (
        <InputNumber
          aria-label={`${t('catalog.workItemMaterials.amount')}: ${nameOf(byId.get(line.materialId))}`}
          min={0}
          max={MAX_QUANTITY_PER_UNIT}
          step={0.1}
          precision={3}
          value={line.quantityPerUnit}
          status={problems[index] === 'catalog.workItemMaterials.amountInvalid' ? 'error' : undefined}
          suffix={t('catalog.workItemMaterials.per', { unit: unitOf(byId.get(line.materialId)), work: workUnit })}
          onChange={(value) => update(index, { quantityPerUnit: value })}
          style={{ width: '100%' }}
        />
      ),
    },
    {
      title: t('catalog.workItemMaterials.waste'),
      key: 'waste',
      width: 120,
      render: (_, line, index) => (
        <InputNumber
          aria-label={`${t('catalog.workItemMaterials.waste')}: ${nameOf(byId.get(line.materialId))}`}
          min={0}
          max={MAX_WASTE}
          precision={0}
          value={line.wastePercent}
          status={problems[index] === 'catalog.workItemMaterials.wasteInvalid' ? 'error' : undefined}
          suffix="%"
          onChange={(value) => update(index, { wastePercent: value })}
          style={{ width: '100%' }}
        />
      ),
    },
    {
      title: t('catalog.workItemMaterials.example', { quantity: EXAMPLE_QUANTITY, unit: workUnit }),
      key: 'example',
      width: 140,
      render: (_, line) =>
        materialLineProblem(line)
          ? '—'
          : `${new Intl.NumberFormat(lng, { maximumFractionDigits: 2 }).format(amountFor(line, EXAMPLE_QUANTITY))} ${unitOf(byId.get(line.materialId))}`,
    },
    {
      key: 'remove',
      width: 48,
      render: (_, line, index) => (
        <Button
          type="text"
          danger
          icon={<DeleteOutlined />}
          aria-label={`${t('catalog.roomTemplates.remove')}: ${nameOf(byId.get(line.materialId))}`}
          onClick={() => setEdited(lines.filter((_, i) => i !== index))}
        />
      ),
    },
  ]

  return (
    <Modal
      open={Boolean(item)}
      title={item ? t('catalog.workItemMaterials.title', { name: localizedName(item.name, lng, defaultCode) }) : ''}
      okText={t('catalog.form.save')}
      cancelText={t('catalog.form.cancel')}
      okButtonProps={{ disabled: edited === null || problems.some(Boolean), loading: saving.isLoading }}
      onOk={save}
      onCancel={close}
      destroyOnHidden
      width={860}
    >
      <Typography.Paragraph type="secondary">{t('catalog.workItemMaterials.help')}</Typography.Paragraph>
      {saved.error && <Alert type="error" showIcon title={errorMessage(t, saved.error)} style={{ marginBottom: 16 }} />}
      <Table
        rowKey="materialId"
        size="small"
        columns={columns}
        dataSource={lines}
        loading={saved.isLoading}
        pagination={false}
        scroll={{ x: 'max-content' }}
        locale={{ emptyText: t('catalog.workItemMaterials.empty') }}
      />
      <Select
        aria-label={t('catalog.workItemMaterials.add')}
        placeholder={t('catalog.workItemMaterials.add')}
        showSearch
        optionFilterProp="label"
        value={null}
        disabled={lines.length >= MAX_MATERIALS_PER_ITEM}
        options={addOptions}
        onChange={(materialId) => setEdited([...lines, { materialId, quantityPerUnit: 1, wastePercent: 0 }])}
        style={{ width: '100%', marginTop: 16 }}
      />
    </Modal>
  )
}
