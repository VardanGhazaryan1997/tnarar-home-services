import { Table } from 'antd'
import { useTranslation } from 'react-i18next'
import { formatMoney } from '@/i18n/format'

/**
 * Materials an estimate counted (E1) as a table: material, amount and cost. `materials`: `[{ materialId, name, unit,
 * quantity, cost }]` (cost null when unknown).
 */
export default function MaterialLines({ materials }) {
  const { t, i18n } = useTranslation()
  const lng = i18n.language
  const number = (value) => new Intl.NumberFormat(lng, { maximumFractionDigits: 1 }).format(value)

  const columns = [
    { title: t('catalog.workItemMaterials.material'), dataIndex: 'name', key: 'name' },
    {
      title: t('estimates.columns.amount'),
      key: 'amount',
      align: 'right',
      render: (_, material) => `${number(material.quantity)} ${t(`catalog.materials.units.${material.unit}`)}`,
    },
    {
      title: t('estimates.columns.price'),
      key: 'cost',
      align: 'right',
      render: (_, material) => (material.cost != null ? formatMoney(material.cost, lng) : '—'),
    },
  ]

  return <Table rowKey="materialId" size="small" columns={columns} dataSource={materials} pagination={false} scroll={{ x: true }} />
}
