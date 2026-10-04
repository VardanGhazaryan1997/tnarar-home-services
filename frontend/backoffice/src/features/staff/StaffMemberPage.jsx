import { ArrowLeftOutlined, CrownOutlined } from '@ant-design/icons'
import { Alert, App, Button, Card, Descriptions, Flex, Space, Spin, Tag, Typography } from 'antd'
import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { useSelector } from 'react-redux'
import { Link, useParams } from 'react-router'
import { errorMessage } from '@/api/errors'
import { selectStaff } from '@/features/auth/authSlice'
import { hasPermission } from '@/features/auth/permissions'
import { formatDate } from '@/i18n/format'
import InviteLinkModal from './InviteLinkModal'
import PermissionList from './PermissionList'
import StaffFormModal from './StaffFormModal'
import StaffStatusTag from './StaffStatusTag'
import { useGetStaffMemberQuery, useRenewInviteMutation, useStaffActionMutation } from './staffApi'

// Actions that change an account, in button order. `danger` ones get a red confirm button.
function availableActions(member, actor) {
  const self = member.id === actor.id
  const actions = []
  if (member.status === 'Invited') actions.push('renewInvite')
  if (member.status === 'Suspended') actions.push('activate')
  else if (!self) actions.push('suspend')
  if (member.twoFactorEnabled) actions.push('resetTwoFactor')
  if (actor.isSuperAdmin) actions.push(member.isSuperAdmin ? 'revokeSuperAdmin' : 'grantSuperAdmin')
  return actions
}

const DANGER = new Set(['suspend', 'resetTwoFactor', 'revokeSuperAdmin'])

/** One staff account: details, roles, effective permissions and account actions. */
export default function StaffMemberPage() {
  const { id } = useParams()
  const { t, i18n } = useTranslation()
  const { message, modal } = App.useApp()
  const actor = useSelector(selectStaff)
  const { data, isLoading, error } = useGetStaffMemberQuery(id)
  const [runAction] = useStaffActionMutation()
  const [renewInvite] = useRenewInviteMutation()
  const [editing, setEditing] = useState(false)
  const [invite, setInvite] = useState(null)

  const back = (
    <Link to="/staff/members">
      <ArrowLeftOutlined /> {t('staff.detail.back')}
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

  const { member, permissions } = data
  // Only Super Admins may change a Super Admin's account.
  const canManage = hasPermission(actor, 'staff.manage') && (actor.isSuperAdmin || !member.isSuperAdmin)
  const actions = canManage ? availableActions(member, actor) : []

  const perform = async (action) => {
    try {
      if (action === 'renewInvite') {
        setInvite(await renewInvite(member.id).unwrap())
      } else {
        await runAction({ id: member.id, action }).unwrap()
        message.success(t(`staff.actions.${action}.done`))
      }
    } catch (failure) {
      message.error(errorMessage(t, failure))
    }
  }

  const confirm = (action) =>
    modal.confirm({
      title: t(`staff.actions.${action}.title`, { name: member.fullName }),
      content: t(`staff.actions.${action}.text`),
      okText: t(`staff.actions.${action}.confirm`),
      okButtonProps: { danger: DANGER.has(action) },
      cancelText: t('catalog.form.cancel'),
      onOk: () => perform(action),
    })

  return (
    <Flex vertical gap="middle">
      {back}
      <Flex justify="space-between" align="center" wrap gap="middle">
        <Space align="center" wrap>
          <Typography.Title level={1} style={{ margin: 0 }}>
            {member.fullName}
          </Typography.Title>
          <StaffStatusTag status={member.status} />
          {member.isSuperAdmin && (
            <Tag color="gold" icon={<CrownOutlined />}>
              {t('staff.superAdmin')}
            </Tag>
          )}
        </Space>
        {canManage && (
          <Space wrap>
            <Button type="primary" onClick={() => setEditing(true)}>
              {t('staff.detail.edit')}
            </Button>
            {actions.map((action) => (
              <Button key={action} danger={DANGER.has(action)} onClick={() => confirm(action)}>
                {t(`staff.actions.${action}.button`)}
              </Button>
            ))}
          </Space>
        )}
      </Flex>

      <Card title={t('staff.detail.account')}>
        <Descriptions column={{ xs: 1, md: 2 }} size="small">
          <Descriptions.Item label={t('staff.form.email')}>{member.email}</Descriptions.Item>
          <Descriptions.Item label={t('staff.detail.twoFactor')}>
            {member.twoFactorEnabled ? <Tag color="success">{t('staff.detail.twoFactorOn')}</Tag> : <Tag>{t('staff.detail.twoFactorOff')}</Tag>}
          </Descriptions.Item>
          <Descriptions.Item label={t('staff.columns.lastSignIn')}>
            {formatDate(member.lastSignInAt, i18n.language, { withTime: true }) || t('staff.never')}
          </Descriptions.Item>
          <Descriptions.Item label={t('staff.detail.created')}>{formatDate(member.createdAt, i18n.language)}</Descriptions.Item>
          {member.status === 'Invited' && (
            <Descriptions.Item label={t('staff.detail.inviteExpires')}>
              {formatDate(member.inviteExpiresAt, i18n.language, { withTime: true })}
            </Descriptions.Item>
          )}
          <Descriptions.Item label={t('staff.form.roles')} span={2}>
            <Space wrap size={[4, 4]}>
              {member.roles.map((role) => (
                <Tag key={role.id}>{role.name}</Tag>
              ))}
              {member.roles.length === 0 && <Typography.Text type="secondary">{t('staff.noRoles')}</Typography.Text>}
            </Space>
          </Descriptions.Item>
        </Descriptions>
      </Card>

      <Card title={t('staff.detail.permissions')}>
        {member.isSuperAdmin ? <Typography.Text>{t('staff.detail.allPermissions')}</Typography.Text> : <PermissionList codes={permissions} />}
      </Card>

      <StaffFormModal
        open={editing}
        member={member}
        onCancel={() => setEditing(false)}
        onDone={() => {
          setEditing(false)
          message.success(t('staff.form.saved'))
        }}
      />
      <InviteLinkModal invite={invite} onClose={() => setInvite(null)} />
    </Flex>
  )
}
