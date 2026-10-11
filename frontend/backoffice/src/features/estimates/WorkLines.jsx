import { Table } from 'antd'
import { useTranslation } from 'react-i18next'
import { formatMoney } from '@/i18n/format'

/**
 * Lines of work as a table: room, work, amount and a price range. `lines`: `[{ key, room, name, unit, quantity, min, max }]`
 * (min/max null when there's no price).
 */
export default function WorkLines({ lines, priceTitle }) {
  const { t, i18n } = useTranslation()
  const lng = i18n.language
  const number = (value) => new Intl.NumberFormat(lng, { maximumFractionDigits: 2 }).format(value)

  const columns = [
    { title: t('estimates.columns.room'), dataIndex: 'room', key: 'room', responsive: ['md'] },
    { title: t('estimates.columns.work'), dataIndex: 'name', key: 'name' },
    {
      title: t('estimates.columns.amount'),
      key: 'amount',
      align: 'right',
      render: (_, line) => (line.quantity != null ? `${number(line.quantity)} ${t(`catalog.workItems.units.${line.unit}`)}` : t('estimates.toAgree')),
    },
    {
      title: priceTitle ?? t('estimates.columns.price'),
      key: 'price',
      align: 'right',
      render: (_, line) => (line.max != null ? `${formatMoney(line.min, lng)} – ${formatMoney(line.max, lng)}` : '—'),
    },
  ]

  return <Table rowKey="key" size="small" columns={columns} dataSource={lines} pagination={false} scroll={{ x: true }} />
}
