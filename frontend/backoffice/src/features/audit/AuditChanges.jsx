import { Table, Typography } from 'antd'
import { useTranslation } from 'react-i18next'

const show = (value) => (value === null || value === undefined || value === '' ? <Typography.Text type="secondary">—</Typography.Text> : value)

/** The properties one audit entry changed, old value → new value. */
export default function AuditChanges({ changes }) {
  const { t } = useTranslation()
  return (
    <Table
      size="small"
      rowKey="property"
      pagination={false}
      dataSource={changes}
      columns={[
        { title: t('audit.property'), dataIndex: 'property', render: (property) => <Typography.Text code>{property}</Typography.Text> },
        { title: t('audit.oldValue'), dataIndex: 'oldValue', render: (value) => <span style={{ wordBreak: 'break-word' }}>{show(value)}</span> },
        { title: t('audit.newValue'), dataIndex: 'newValue', render: (value) => <span style={{ wordBreak: 'break-word' }}>{show(value)}</span> },
      ]}
    />
  )
}
