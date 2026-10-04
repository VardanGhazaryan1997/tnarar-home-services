import { DeleteOutlined, EditOutlined, PlusOutlined } from '@ant-design/icons'
import { Alert, App, Button, Flex, Popconfirm, Space, Table, Tag, Tooltip, Typography } from 'antd'
import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { useSelector } from 'react-redux'
import { errorMessage } from '@/api/errors'
import { selectStaff } from '@/features/auth/authSlice'
import { hasPermission } from '@/features/auth/permissions'
import PermissionList from './PermissionList'
import RoleFormModal from './RoleFormModal'
import { useDeleteRoleMutation, useGetRolesQuery } from './staffApi'

/** Roles bundle permissions. Everyone with staff.view sees them; only Super Admins (roles.manage) change them. */
export default function RolesTab() {
  const { t } = useTranslation()
  const { message } = App.useApp()
  const actor = useSelector(selectStaff)
  const { data: roles = [], isFetching, error } = useGetRolesQuery()
  const [deleteRole] = useDeleteRoleMutation()
  // null = closed, {} = new role, otherwise the role being edited.
  const [editing, setEditing] = useState(null)
  const canManage = hasPermission(actor, 'roles.manage')

  const remove = async (role) => {
    try {
      await deleteRole(role.id).unwrap()
      message.success(t('roles.deleted'))
    } catch (failure) {
      message.error(errorMessage(t, failure))
    }
  }

  const columns = [
    {
      title: t('roles.columns.name'),
      key: 'name',
      render: (_, role) => (
        <Space wrap>
          <Typography.Text strong>{role.name}</Typography.Text>
          {role.isSystem && <Tag>{t('roles.builtIn')}</Tag>}
        </Space>
      ),
    },
    {
      title: t('roles.columns.description'),
      dataIndex: 'description',
      responsive: ['md'],
    },
    {
      title: t('roles.columns.permissions'),
      key: 'permissions',
      render: (_, role) => t('roles.permissionCount', { count: role.permissions.length }),
    },
    ...(canManage
      ? [
          {
            title: t('catalog.columns.actions'),
            key: 'actions',
            render: (_, role) => (
              <Space>
                <Tooltip title={t('catalog.actions.edit')}>
                  <Button type="text" icon={<EditOutlined />} aria-label={t('roles.editAria', { name: role.name })} onClick={() => setEditing(role)} />
                </Tooltip>
                {!role.isSystem && (
                  <Popconfirm
                    title={t('roles.deleteConfirm', { name: role.name })}
                    description={t('roles.deleteHelp')}
                    okText={t('catalog.actions.delete')}
                    okButtonProps={{ danger: true }}
                    cancelText={t('catalog.form.cancel')}
                    onConfirm={() => remove(role)}
                  >
                    <Button type="text" danger icon={<DeleteOutlined />} aria-label={t('roles.deleteAria', { name: role.name })} />
                  </Popconfirm>
                )}
              </Space>
            ),
          },
        ]
      : []),
  ]

  return (
    <Flex vertical gap="middle">
      <Flex justify="space-between" align="center" wrap gap="small">
        <Typography.Text type="secondary">{t(canManage ? 'roles.help' : 'roles.viewOnly')}</Typography.Text>
        {canManage && (
          <Button type="primary" icon={<PlusOutlined />} onClick={() => setEditing({})}>
            {t('roles.add')}
          </Button>
        )}
      </Flex>
      {error && <Alert type="error" showIcon title={errorMessage(t, error)} />}
      <Table
        rowKey="id"
        columns={columns}
        dataSource={roles}
        loading={isFetching}
        pagination={false}
        scroll={{ x: true }}
        locale={{ emptyText: t('roles.empty') }}
        expandable={{ expandedRowRender: (role) => <PermissionList codes={role.permissions} /> }}
      />
      <RoleFormModal
        open={editing !== null}
        role={editing?.id ? editing : undefined}
        onCancel={() => setEditing(null)}
        onDone={() => {
          message.success(t(editing?.id ? 'catalog.saved' : 'catalog.created'))
          setEditing(null)
        }}
      />
    </Flex>
  )
}
