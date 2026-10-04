import { Alert, Button, Form, Input, Result } from 'antd'
import { useTranslation } from 'react-i18next'
import { Link, useSearchParams } from 'react-router'
import { errorMessage, fieldErrors } from '@/api/errors'
import { useAcceptInviteMutation } from './authApi'
import AuthCard from './AuthCard'

const MIN_PASSWORD = 12

/**
 * Opened from an invitation link (/accept-invite?token=…): the new staff member chooses a password,
 * then signs in and sets up their authenticator app.
 */
export default function AcceptInvitePage() {
  const { t } = useTranslation()
  const [form] = Form.useForm()
  const [params] = useSearchParams()
  const token = params.get('token')
  const [accept, { isLoading, isSuccess, error }] = useAcceptInviteMutation()

  if (!token) {
    return (
      <AuthCard title={t('auth.acceptInvite.title')}>
        <Alert type="error" showIcon title={t('errors.staff.invite_invalid')} />
      </AuthCard>
    )
  }

  if (isSuccess) {
    return (
      <AuthCard title={t('auth.acceptInvite.title')}>
        <Result
          status="success"
          title={t('auth.acceptInvite.doneTitle')}
          subTitle={t('auth.acceptInvite.doneText')}
          extra={
            <Link to="/login">
              <Button type="primary">{t('auth.signIn.submit')}</Button>
            </Link>
          }
        />
      </AuthCard>
    )
  }

  const submit = async ({ password }) => {
    try {
      await accept({ token, password }).unwrap()
    } catch (failure) {
      form.setFields(fieldErrors(t, failure))
    }
  }

  const failed = error && fieldErrors(t, error).length === 0

  return (
    <AuthCard title={t('auth.acceptInvite.title')}>
      <p>{t('auth.acceptInvite.text', { min: MIN_PASSWORD })}</p>
      {failed && <Alert type="error" showIcon title={errorMessage(t, error)} style={{ marginBottom: 16 }} />}
      <Form form={form} layout="vertical" requiredMark={false} onFinish={submit} disabled={isLoading}>
        <Form.Item
          name="password"
          label={t('auth.acceptInvite.password')}
          rules={[
            { required: true, message: t('auth.signIn.passwordRequired') },
            { min: MIN_PASSWORD, message: t('errors.password.too_short') },
          ]}
        >
          <Input.Password autoComplete="new-password" autoFocus />
        </Form.Item>
        <Form.Item
          name="confirm"
          label={t('auth.acceptInvite.confirm')}
          dependencies={['password']}
          rules={[
            { required: true, message: t('auth.acceptInvite.confirmRequired') },
            ({ getFieldValue }) => ({
              validator: (_, value) =>
                !value || value === getFieldValue('password') ? Promise.resolve() : Promise.reject(new Error(t('auth.acceptInvite.mismatch'))),
            }),
          ]}
        >
          <Input.Password autoComplete="new-password" />
        </Form.Item>
        <Button type="primary" htmlType="submit" block loading={isLoading}>
          {t('auth.acceptInvite.submit')}
        </Button>
      </Form>
    </AuthCard>
  )
}
