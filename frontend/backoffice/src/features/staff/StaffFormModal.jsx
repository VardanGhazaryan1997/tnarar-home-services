import { Alert, Checkbox, Form, Input, Modal, Select } from 'antd'
import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { useSelector } from 'react-redux'
import { errorMessage, fieldErrors, problemCode } from '@/api/errors'
import { selectStaff } from '@/features/auth/authSlice'
import { useGetRolesQuery, useInviteStaffMutation, useUpdateStaffMemberMutation } from './staffApi'

const NAME_MAX = 100

/**
 * Invites a staff member (no `member`) or changes a member's name and roles.
 * `onDone(result)` gets the invitation (with its one-time token) or the updated member.
 */
export default function StaffFormModal({ open, member, onDone, onCancel }) {
  const { t } = useTranslation()
  const [form] = Form.useForm()
  const actor = useSelector(selectStaff)
  const { data: roles = [], isLoading: rolesLoading } = useGetRolesQuery(undefined, { skip: !open })
  const [invite, { isLoading: inviting }] = useInviteStaffMutation()
  const [update, { isLoading: updating }] = useUpdateStaffMemberMutation()
  const [failure, setFailure] = useState(null)
  const editing = Boolean(member)

  const submit = async (values) => {
    setFailure(null)
    try {
      const result = editing
        ? await update({ id: member.id, fullName: values.fullName.trim(), roleIds: values.roleIds ?? [] }).unwrap()
        : await invite({
            email: values.email.trim(),
            fullName: values.fullName.trim(),
            roleIds: values.roleIds ?? [],
            isSuperAdmin: Boolean(values.isSuperAdmin),
          }).unwrap()
      onDone(result)
    } catch (error) {
      const fields = fieldErrors(t, error)
      if (problemCode(error) === 'staff.email_taken') fields.push({ name: 'email', errors: [errorMessage(t, error)] })
      if (fields.length > 0) form.setFields(fields)
      else setFailure(errorMessage(t, error))
    }
  }

  return (
    <Modal
      open={open}
      title={t(editing ? 'staff.form.editTitle' : 'staff.form.inviteTitle')}
      okText={t(editing ? 'staff.form.save' : 'staff.form.invite')}
      okButtonProps={{ htmlType: 'submit', form: 'staff-form', loading: inviting || updating }}
      cancelText={t('catalog.form.cancel')}
      onCancel={onCancel}
      afterClose={() => setFailure(null)}
      destroyOnHidden
    >
      {failure && <Alert type="error" showIcon title={failure} style={{ marginBottom: 16 }} />}
      <Form
        id="staff-form"
        form={form}
        layout="vertical"
        requiredMark={false}
        onFinish={submit}
        initialValues={editing ? { fullName: member.fullName, roleIds: member.roles.map((role) => role.id) } : { roleIds: [] }}
      >
        {!editing && (
          <Form.Item
            name="email"
            label={t('staff.form.email')}
            rules={[
              { required: true, message: t('auth.signIn.emailRequired') },
              { type: 'email', message: t('auth.signIn.emailInvalid') },
            ]}
          >
            <Input type="email" autoComplete="off" />
          </Form.Item>
        )}
        <Form.Item
          name="fullName"
          label={t('staff.form.fullName')}
          rules={[
            { required: true, whitespace: true, message: t('errors.name.required') },
            { max: NAME_MAX, message: t('errors.name.too_long') },
          ]}
        >
          <Input maxLength={NAME_MAX} />
        </Form.Item>
        <Form.Item name="roleIds" label={t('staff.form.roles')} extra={t('staff.form.rolesHelp')}>
          <Select
            mode="multiple"
            loading={rolesLoading}
            optionFilterProp="label"
            options={roles.map((role) => ({ value: role.id, label: role.name }))}
          />
        </Form.Item>
        {!editing && actor?.isSuperAdmin && (
          <Form.Item name="isSuperAdmin" valuePropName="checked" extra={t('staff.form.superAdminHelp')}>
            <Checkbox>{t('staff.form.superAdmin')}</Checkbox>
          </Form.Item>
        )}
      </Form>
    </Modal>
  )
}
