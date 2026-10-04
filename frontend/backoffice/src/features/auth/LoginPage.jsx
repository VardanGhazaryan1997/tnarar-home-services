import { Alert, Button, Form, Input } from 'antd'
import { useTranslation } from 'react-i18next'
import { useSelector } from 'react-redux'
import { useLocation, useNavigate } from 'react-router'
import { errorMessage, fieldErrors } from '@/api/errors'
import { useSignInMutation } from './authApi'
import { selectNotice } from './authSlice'
import AuthCard from './AuthCard'

/** Step 1 of signing in: email and password. */
export default function LoginPage() {
  const { t } = useTranslation()
  const [form] = Form.useForm()
  const navigate = useNavigate()
  const location = useLocation()
  const notice = useSelector(selectNotice)
  const [signIn, { isLoading, error }] = useSignInMutation()

  const handleFinish = async (values) => {
    try {
      await signIn({ email: values.email.trim(), password: values.password }).unwrap()
      // Keep where the staff member was going for after the authenticator step.
      navigate('/login/2fa', { state: location.state })
    } catch (failure) {
      form.setFields(fieldErrors(t, failure))
    }
  }

  const message = error ? errorMessage(t, error) : notice && t(`errors.${notice}`, { defaultValue: t('errors.generic') })

  return (
    <AuthCard title={t('auth.signIn.title')}>
      {message && <Alert type={error ? 'error' : 'info'} title={message} showIcon style={{ marginBottom: 16 }} />}
      <Form form={form} layout="vertical" requiredMark={false} onFinish={handleFinish} disabled={isLoading}>
        <Form.Item
          name="email"
          label={t('auth.signIn.email')}
          rules={[
            { required: true, message: t('auth.signIn.emailRequired') },
            { type: 'email', message: t('auth.signIn.emailInvalid') },
          ]}
        >
          <Input type="email" autoComplete="username" autoFocus />
        </Form.Item>
        <Form.Item
          name="password"
          label={t('auth.signIn.password')}
          rules={[{ required: true, message: t('auth.signIn.passwordRequired') }]}
        >
          <Input.Password autoComplete="current-password" />
        </Form.Item>
        <Button type="primary" htmlType="submit" block loading={isLoading}>
          {t('auth.signIn.submit')}
        </Button>
      </Form>
    </AuthCard>
  )
}
