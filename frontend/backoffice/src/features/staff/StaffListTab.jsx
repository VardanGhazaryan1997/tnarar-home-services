import { CrownOutlined, UserAddOutlined } from '@ant-design/icons'
import { Alert, Button, Flex, Input, Select, Space, Table, Tag, Typography } from 'antd'
import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { useSelector } from 'react-redux'
import { Link, useSearchParams } from 'react-router'
import { errorMessage } from '@/api/errors'
import { selectStaff } from '@/features/auth/authSlice'
import { hasPermission } from '@/features/auth/permissions'
import { formatDate } from '@/i18n/format'
import InviteLinkModal from './InviteLinkModal'
import StaffFormModal from './StaffFormModal'
import StaffStatusTag from './StaffStatusTag'
import { STAFF_STATUSES } from './statuses'
import { useGetRolesQuery, useGetStaffQuery } from './staffApi'

/** Back Office accounts. Filters live in the address (?search=&status=&roleId=&page=). */
export default function StaffListTab() {
  const { t, i18n } = useTranslation()
  const actor = useSelector(selectStaff)
  const [params, setParams] = useSearchParams()
  const search = params.get('search') ?? ''
  const status = params.get('status') ?? undefined
  const roleId = params.get('roleId') ?? undefined
  const page = Number(params.get('page') ?? 1)
  const { data, isFetching, error } = useGetStaffQuery({ search, status, roleId, page })
  const { data: roles = [] } = useGetRolesQuery()
  const [inviting, setInviting] = useState(false)
  const [invite, setInvite] = useState(null)
  const canManage = hasPermission(actor, 'staff.manage')

  const update = (changes) => {
    const next = new URLSearchParams(params)
    for (const [key, value] of Object.entries({ page: undefined, ...changes })) {
      if (value === undefined || value === '') next.delete(key)
      else next.set(key, String(value))
    }
    setParams(next)
  }

  const columns = [
    {
      title: t('staff.columns.name'),
      key: 'name',
      render: (_, member) => (
        <Flex vertical>
          <Link to={`/staff/members/${member.id}`}>{member.fullName}</Link>
          <Typography.Text type="secondary" style={{ fontSize: 12 }}>
            {member.email}
          </Typography.Text>
        </Flex>
      ),
    },
    {
      title: t('staff.columns.roles'),
      key: 'roles',
      responsive: ['md'],
      render: (_, member) => (
        <Space wrap size={[4, 4]}>
          {member.isSuperAdmin && (
            <Tag color="gold" icon={<CrownOutlined />}>
              {t('staff.superAdmin')}
            </Tag>
          )}
          {member.roles.map((role) => (
            <Tag key={role.id}>{role.name}</Tag>
          ))}
          {!member.isSuperAdmin && member.roles.length === 0 && <Typography.Text type="secondary">{t('staff.noRoles')}</Typography.Text>}
        </Space>
      ),
    },
    {
      title: t('staff.columns.lastSignIn'),
      key: 'lastSignIn',
      responsive: ['lg'],
      render: (_, member) => formatDate(member.lastSignInAt, i18n.language, { withTime: true }) || t('staff.never'),
    },
    {
      title: t('staff.columns.status'),
      key: 'status',
      render: (_, member) => <StaffStatusTag status={member.status} />,
    },
  ]

  return (
    <Flex vertical gap="middle">
      <Flex gap="small" wrap justify="space-between">
        <Flex gap="small" wrap>
          <Input.Search
            aria-label={t('staff.filters.search')}
            placeholder={t('staff.filters.searchPlaceholder')}
            defaultValue={search}
            allowClear
            onSearch={(value) => update({ search: value.trim() || undefined })}
            style={{ maxWidth: 320 }}
          />
          <Select
            aria-label={t('staff.filters.status')}
            placeholder={t('staff.filters.anyStatus')}
            allowClear
            value={status}
            onChange={(value) => update({ status: value })}
            options={STAFF_STATUSES.map((value) => ({ value, label: t(`staff.status.${value}`) }))}
            style={{ minWidth: 160 }}
          />
          <Select
            aria-label={t('staff.filters.role')}
            placeholder={t('staff.filters.anyRole')}
            allowClear
            value={roleId}
            onChange={(value) => update({ roleId: value })}
            options={roles.map((role) => ({ value: role.id, label: role.name }))}
            style={{ minWidth: 180 }}
          />
        </Flex>
        {canManage && (
          <Button type="primary" icon={<UserAddOutlined />} onClick={() => setInviting(true)}>
            {t('staff.invite')}
          </Button>
        )}
      </Flex>
      {error && <Alert type="error" showIcon title={errorMessage(t, error)} />}
      <Table
        rowKey="id"
        columns={columns}
        dataSource={data?.items ?? []}
        loading={isFetching}
        scroll={{ x: true }}
        locale={{ emptyText: t('staff.empty') }}
        pagination={{
          current: page,
          pageSize: data?.pageSize ?? 50,
          total: data?.totalCount ?? 0,
          showSizeChanger: false,
          hideOnSinglePage: true,
          onChange: (next) => update({ page: next === 1 ? undefined : next }),
        }}
      />
      <StaffFormModal
        open={inviting}
        onCancel={() => setInviting(false)}
        onDone={(result) => {
          setInviting(false)
          setInvite(result)
        }}
      />
      <InviteLinkModal invite={invite} onClose={() => setInvite(null)} />
    </Flex>
  )
}
