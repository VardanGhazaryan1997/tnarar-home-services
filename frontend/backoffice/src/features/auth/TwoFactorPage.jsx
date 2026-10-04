import { Alert, Button, Flex, Form, Input, QRCode, Typography } from 'antd'
import { useTranslation } from 'react-i18next'
import { useDispatch, useSelector } from 'react-redux'
import { Navigate, useLocation, useNavigate } from 'react-router'
import { errorMessage, problemCode } from '@/api/errors'
import { useCompleteTwoFactorSetupMutation, useVerifyTwoFactorMutation } from './authApi'
import { challengeCleared, selectChallenge } from './authSlice'
import AuthCard from './AuthCard'

const SETUP_REQUIRED = 'two_factor_setup_required'
// The password step must be repeated after these.
const RESTART_CODES = ['staff.challenge_invalid', 'staff.two_factor_setup_not_started', 'staff.two_factor_not_enabled']

/**
 * Step 2 of signing in: the authenticator code. On first sign-in the staff member
 * first adds the account to an authenticator app by scanning a QR code.
 */
export default function TwoFactorPage() {
  const { t } = useTranslation()
  const [form] = Form.useForm()
  const dispatch = useDispatch()
  const navigate = useNavigate()
  const location = useLocation()
  const challenge = useSelector(selectChallenge)
  const [completeSetup, setup] = useCompleteTwoFactorSetupMutation()
  const [verify, verification] = useVerifyTwoFactorMutation()

  if (!challenge) return <Navigate replace to="/login" state={location.state} />

  const isSetup = challenge.status === SETUP_REQUIRED
  const { isLoading, error } = isSetup ? setup : verification

  const restart = (code) => {
    dispatch(challengeCleared(code))
    navigate('/login', { replace: true, state: location.state })
  }

  const handleFinish = async ({ code }) => {
    const submit = isSetup ? completeSetup : verify
    try {
      // On success the session starts and the sign-in pages redirect onwards.
      await submit({ challengeToken: challenge.challengeToken, code }).unwrap()
    } catch (failure) {
      const failureCode = problemCode(failure)
      if (RESTART_CODES.includes(failureCode)) restart(failureCode)
      else form.resetFields()
    }
  }

  return (
    <AuthCard title={t(isSetup ? 'auth.twoFactor.setupTitle' : 'auth.twoFactor.title')}>
      {isSetup ? (
        <>
          <Typography.Paragraph>{t('auth.twoFactor.step1')}</Typography.Paragraph>
          <Typography.Paragraph>{t('auth.twoFactor.step2')}</Typography.Paragraph>
          <Flex justify="center" style={{ marginBottom: 16 }}>
            <QRCode type="svg" value={challenge.setupUri} aria-label={t('auth.twoFactor.qrLabel')} />
          </Flex>
          <Typography.Paragraph type="secondary">
            {t('auth.twoFactor.secretLabel')} <Typography.Text code copyable>{challenge.setupSecret}</Typography.Text>
          </Typography.Paragraph>
          <Typography.Paragraph>{t('auth.twoFactor.step3')}</Typography.Paragraph>
        </>
      ) : (
        <Typography.Paragraph>{t('auth.twoFactor.text')}</Typography.Paragraph>
      )}

      {error && <Alert type="error" title={errorMessage(t, error)} showIcon style={{ marginBottom: 16 }} />}

      <Form form={form} layout="vertical" requiredMark={false} onFinish={handleFinish} disabled={isLoading}>
        <Form.Item
          name="code"
          label={t('auth.twoFactor.code')}
          rules={[{ required: true, len: 6, message: t('auth.twoFactor.codeRequired') }]}
        >
          <Input.OTP length={6} autoFocus inputMode="numeric" />
        </Form.Item>
        <Button type="primary" htmlType="submit" block loading={isLoading}>
          {t(isSetup ? 'auth.twoFactor.setupSubmit' : 'auth.twoFactor.submit')}
        </Button>
      </Form>

      <Button type="link" block style={{ marginTop: 8 }} onClick={() => restart(null)}>
        {t('auth.twoFactor.back')}
      </Button>
    </AuthCard>
  )
}
