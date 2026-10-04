import { Descriptions, Space, Tag, Typography } from 'antd'
import { useTranslation } from 'react-i18next'
import { groupPermissions, permissionGroupLabel, permissionLabel } from './permissionGroups'

/** Permissions grouped by area, read-only. */
export default function PermissionList({ codes }) {
  const { t } = useTranslation()
  if (codes.length === 0) return <Typography.Text type="secondary">{t('staff.noPermissions')}</Typography.Text>

  return (
    <Descriptions column={1} size="small">
      {groupPermissions(codes).map(([area, items]) => (
        <Descriptions.Item key={area} label={permissionGroupLabel(t, area)}>
          <Space wrap size={[4, 4]}>
            {items.map((code) => (
              <Tag key={code}>{permissionLabel(t, code)}</Tag>
            ))}
          </Space>
        </Descriptions.Item>
      ))}
    </Descriptions>
  )
}
