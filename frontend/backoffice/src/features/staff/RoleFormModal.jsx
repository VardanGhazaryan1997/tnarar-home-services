import { Alert, Checkbox, Flex, Form, Input, Modal, Typography } from 'antd'
import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { errorMessage, fieldErrors, problemCode } from '@/api/errors'
import { groupPermissions, permissionGroupLabel, permissionLabel } from './permissionGroups'
import { useCreateRoleMutation, useGetPermissionsQuery, useUpdateRoleMutation } from './staffApi'

const NAME_MAX = 64
const DESCRIPTION_MAX = 256

/** Creates a role (no `role`) or changes one. Super-Admin-only permissions can't be put in a role, so they aren't offered. */
export default function RoleFormModal({ open, role, onDone, onCancel }) {
  const { t } = useTranslation()
  const [form] = Form.useForm()
  const { data: catalog = [] } = useGetPermissionsQuery(undefined, { skip: !open })
  const [create, { isLoading: creating }] = useCreateRoleMutation()
  const [update, { isLoading: updating }] = useUpdateRoleMutation()
  const [failure, setFailure] = useState(null)
  const editing = Boolean(role)
  const grantable = catalog.filter((permission) => !permission.superAdminOnly).map((permission) => permission.code)

  const submit = async (values) => {
    setFailure(null)
    const body = { name: values.name.trim(), description: values.description?.trim() || null, permissions: values.permissions ?? [] }
    try {
      const result = editing ? await update({ id: role.id, ...body }).unwrap() : await create(body).unwrap()
      onDone(result)
    } catch (error) {
      const fields = fieldErrors(t, error)
      if (problemCode(error) === 'role.name_taken') fields.push({ name: 'name', errors: [errorMessage(t, error)] })
      if (fields.length > 0) form.setFields(fields)
      else setFailure(errorMessage(t, error))
    }
  }

  return (
    <Modal
      open={open}
      title={editing ? t('roles.form.editTitle', { name: role.name }) : t('roles.form.addTitle')}
      okText={t('catalog.form.save')}
      okButtonProps={{ htmlType: 'submit', form: 'role-form', loading: creating || updating }}
      cancelText={t('catalog.form.cancel')}
      onCancel={onCancel}
      afterClose={() => setFailure(null)}
      width={640}
      destroyOnHidden
    >
      {failure && <Alert type="error" showIcon title={failure} style={{ marginBottom: 16 }} />}
      <Form
        id="role-form"
        form={form}
        layout="vertical"
        requiredMark={false}
        onFinish={submit}
        initialValues={editing ? { name: role.name, description: role.description, permissions: role.permissions } : { permissions: [] }}
      >
        <Form.Item
          name="name"
          label={t('roles.form.name')}
          extra={role?.isSystem ? t('roles.form.systemName') : undefined}
          rules={[
            { required: true, whitespace: true, message: t('errors.name.required') },
            { max: NAME_MAX, message: t('errors.name.too_long') },
          ]}
        >
          <Input maxLength={NAME_MAX} disabled={role?.isSystem} />
        </Form.Item>
        <Form.Item name="description" label={t('roles.form.description')} rules={[{ max: DESCRIPTION_MAX, message: t('errors.description.too_long') }]}>
          <Input.TextArea rows={2} maxLength={DESCRIPTION_MAX} showCount />
        </Form.Item>
        <Form.Item name="permissions" label={t('roles.form.permissions')}>
          <Checkbox.Group style={{ width: '100%' }}>
            <Flex vertical gap="small" style={{ width: '100%' }}>
              {groupPermissions(grantable).map(([area, codes]) => (
                <Flex key={area} vertical gap={4}>
                  <Typography.Text strong>{permissionGroupLabel(t, area)}</Typography.Text>
                  <Flex wrap gap="small">
                    {codes.map((code) => (
                      <Checkbox key={code} value={code}>
                        {permissionLabel(t, code)}
                      </Checkbox>
                    ))}
                  </Flex>
                </Flex>
              ))}
            </Flex>
          </Checkbox.Group>
        </Form.Item>
      </Form>
    </Modal>
  )
}
