import { Alert, Flex, Input, Select, Space, Table, Tag, Typography } from 'antd'
import { useTranslation } from 'react-i18next'
import { Link, useSearchParams } from 'react-router'
import { errorMessage } from '@/api/errors'
import PartnerStatusTag from '@/features/partners/PartnerStatusTag'
import { formatDate } from '@/i18n/format'
import UserStatusTag from './UserStatusTag'
import { useGetUsersQuery } from './usersApi'

const ROLES = ['Customer', 'Partner']
const STATUSES = ['Active', 'Blocked']

/** Portal users, newest first. Filters live in the address (?search=&role=&status=&page=). */
export default function UsersPage() {
  const { t, i18n } = useTranslation()
  const [params, setParams] = useSearchParams()
  const search = params.get('search') ?? ''
  const role = params.get('role') ?? undefined
  const status = params.get('status') ?? undefined
  const page = Number(params.get('page') ?? 1)
  const { data, isFetching, error } = useGetUsersQuery({ search, role, status, page })

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
      title: t('users.columns.user'),
      key: 'user',
      render: (_, user) => (
        <Flex vertical>
          <Link to={`/users/${user.id}`}>{user.fullName ?? user.phone}</Link>
          <Typography.Text type="secondary" style={{ fontSize: 12 }}>
            {user.fullName ? user.phone : t('users.noName')}
          </Typography.Text>
        </Flex>
      ),
    },
    {
      title: t('users.columns.email'),
      key: 'email',
      responsive: ['lg'],
      render: (_, user) => user.email ?? '—',
    },
    {
      title: t('users.columns.roles'),
      key: 'roles',
      responsive: ['md'],
      render: (_, user) => (
        <Space wrap size={[4, 4]}>
          {user.roles.map((name) => (
            <Tag key={name}>{t(`users.role.${name}`)}</Tag>
          ))}
          {user.partnerStatus && <PartnerStatusTag status={user.partnerStatus} />}
        </Space>
      ),
    },
    {
      title: t('users.columns.joined'),
      key: 'joined',
      responsive: ['sm'],
      render: (_, user) => formatDate(user.createdAt, i18n.language),
    },
    {
      title: t('users.columns.status'),
      key: 'status',
      render: (_, user) => <UserStatusTag status={user.status} />,
    },
  ]

  return (
    <>
      <Typography.Title level={1}>{t('nav.users')}</Typography.Title>
      <Flex vertical gap="middle">
        <Flex gap="small" wrap>
          <Input.Search
            aria-label={t('users.filters.search')}
            placeholder={t('users.filters.searchPlaceholder')}
            defaultValue={search}
            allowClear
            onSearch={(value) => update({ search: value.trim() || undefined })}
            style={{ maxWidth: 320 }}
          />
          <Select
            aria-label={t('users.filters.role')}
            placeholder={t('users.filters.anyRole')}
            allowClear
            value={role}
            onChange={(value) => update({ role: value })}
            options={ROLES.map((value) => ({ value, label: t(`users.role.${value}`) }))}
            style={{ minWidth: 160 }}
          />
          <Select
            aria-label={t('users.filters.status')}
            placeholder={t('users.filters.anyStatus')}
            allowClear
            value={status}
            onChange={(value) => update({ status: value })}
            options={STATUSES.map((value) => ({ value, label: t(`users.status.${value}`) }))}
            style={{ minWidth: 160 }}
          />
        </Flex>
        {error && <Alert type="error" showIcon title={errorMessage(t, error)} />}
        <Table
          rowKey="id"
          columns={columns}
          dataSource={data?.items ?? []}
          loading={isFetching}
          scroll={{ x: true }}
          locale={{ emptyText: t('users.empty') }}
          pagination={{
            current: page,
            pageSize: data?.pageSize ?? 50,
            total: data?.totalCount ?? 0,
            showSizeChanger: false,
            hideOnSinglePage: true,
            onChange: (next) => update({ page: next === 1 ? undefined : next }),
          }}
        />
      </Flex>
    </>
  )
}
