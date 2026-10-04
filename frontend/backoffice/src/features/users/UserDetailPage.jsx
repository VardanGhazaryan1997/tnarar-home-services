import { ArrowLeftOutlined } from '@ant-design/icons'
import { Alert, App, Button, Card, Descriptions, Flex, Space, Spin, Tag, Typography } from 'antd'
import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { useSelector } from 'react-redux'
import { Link, useParams } from 'react-router'
import { errorMessage } from '@/api/errors'
import { selectStaff } from '@/features/auth/authSlice'
import { hasPermission } from '@/features/auth/permissions'
import PartnerStatusTag from '@/features/partners/PartnerStatusTag'
import { formatDate } from '@/i18n/format'
import BlockUserModal from './BlockUserModal'
import UserStatusTag from './UserStatusTag'
import { useGetUserQuery, useUnblockUserMutation } from './usersApi'

/** One Portal user: contact details, roles, partner profile, and block / unblock. */
export default function UserDetailPage() {
  const { id } = useParams()
  const { t, i18n } = useTranslation()
  const { message, modal } = App.useApp()
  const staff = useSelector(selectStaff)
  const { data: user, isLoading, error } = useGetUserQuery(id)
  const [unblock] = useUnblockUserMutation()
  const [blocking, setBlocking] = useState(false)

  const back = (
    <Link to="/users">
      <ArrowLeftOutlined /> {t('users.detail.back')}
    </Link>
  )

  if (isLoading) return <Spin />
  if (error) {
    return (
      <Flex vertical gap="middle">
        {back}
        <Alert type="error" showIcon title={errorMessage(t, error)} />
      </Flex>
    )
  }

  const name = user.fullName ?? user.phone
  const blocked = user.status === 'Blocked'
  const canBlock = hasPermission(staff, 'users.block')

  const confirmUnblock = () =>
    modal.confirm({
      title: t('users.unblock.title', { name }),
      content: t('users.unblock.text'),
      okText: t('users.unblock.confirm'),
      cancelText: t('catalog.form.cancel'),
      onOk: async () => {
        try {
          await unblock(user.id).unwrap()
          message.success(t('users.unblock.done'))
        } catch (failure) {
          message.error(errorMessage(t, failure))
        }
      },
    })

  return (
    <Flex vertical gap="middle">
      {back}
      <Flex justify="space-between" align="center" wrap gap="middle">
        <Space align="center" wrap>
          <Typography.Title level={1} style={{ margin: 0 }}>
            {name}
          </Typography.Title>
          <UserStatusTag status={user.status} />
        </Space>
        {canBlock &&
          (blocked ? (
            <Button type="primary" onClick={confirmUnblock}>
              {t('users.unblock.button')}
            </Button>
          ) : (
            <Button danger onClick={() => setBlocking(true)}>
              {t('users.block.button')}
            </Button>
          ))}
      </Flex>

      {blocked && <Alert type="error" showIcon title={t('users.detail.blockReason')} description={user.blockReason ?? '—'} />}

      <Card title={t('users.detail.account')}>
        <Descriptions column={{ xs: 1, md: 2 }} size="small">
          <Descriptions.Item label={t('users.detail.phone')}>{user.phone}</Descriptions.Item>
          <Descriptions.Item label={t('users.columns.email')}>{user.email ?? '—'}</Descriptions.Item>
          <Descriptions.Item label={t('users.detail.name')}>{user.fullName ?? '—'}</Descriptions.Item>
          <Descriptions.Item label={t('users.columns.roles')}>
            <Space wrap size={[4, 4]}>
              {user.roles.map((role) => (
                <Tag key={role}>{t(`users.role.${role}`)}</Tag>
              ))}
            </Space>
          </Descriptions.Item>
          <Descriptions.Item label={t('users.columns.joined')}>{formatDate(user.createdAt, i18n.language)}</Descriptions.Item>
          <Descriptions.Item label={t('users.detail.lastSignIn')}>
            {formatDate(user.lastSignInAt, i18n.language, { withTime: true }) || t('staff.never')}
          </Descriptions.Item>
        </Descriptions>
      </Card>

      {user.partner && (
        <Card title={t('users.detail.partner')}>
          <Space wrap>
            {hasPermission(staff, 'partners.view') ? (
              <Link to={`/partners/${user.partner.id}`}>{user.partner.displayName}</Link>
            ) : (
              <Typography.Text>{user.partner.displayName}</Typography.Text>
            )}
            <PartnerStatusTag status={user.partner.status} />
          </Space>
          {blocked && (
            <Typography.Paragraph type="secondary" style={{ marginTop: 8, marginBottom: 0 }}>
              {t('users.detail.partnerHidden')}
            </Typography.Paragraph>
          )}
        </Card>
      )}

      <BlockUserModal
        user={user}
        open={blocking}
        onCancel={() => setBlocking(false)}
        onDone={() => {
          setBlocking(false)
          message.success(t('users.block.done'))
        }}
      />
    </Flex>
  )
}
