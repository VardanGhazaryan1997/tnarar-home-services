import { useEffect, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { useDispatch, useSelector } from 'react-redux'
import { Navigate, useLocation, useNavigate } from 'react-router'
import { errorMessage, fieldErrors } from '@/api/errors'
import { BrandMark } from '@/components/BrandLogo/BrandLogo'
import Alert from '@/components/ui/Alert/Alert'
import Button from '@/components/ui/Button/Button'
import { TextField } from '@/components/ui/Field/Field'
import { useLocalizedPath } from '@/i18n/hooks'
import { useSendCodeMutation, useUpdateProfileMutation, useVerifyCodeMutation } from './authApi'
import { noticeShown, selectAuthStatus, selectNotice, selectUser } from './authSlice'
import styles from './auth.module.scss'

/** Seconds left until `until` (a timestamp), refreshed every second. */
function useSecondsLeft(until) {
  const [now, setNow] = useState(() => Date.now())
  useEffect(() => {
    if (!until) return undefined
    let timer
    const tick = () => {
      setNow(Date.now())
      if (until <= Date.now()) clearInterval(timer)
    }
    timer = setInterval(tick, 1000)
    tick()
    return () => clearInterval(timer)
  }, [until])
  return until ? Math.max(0, Math.ceil((until - now) / 1000)) : 0
}

/**
 * Sign in or sign up with a phone number: (1) phone, (2) the SMS code, (3) for new users, their name.
 * Afterwards the user goes back to the page that sent them here.
 */
export default function SignInPage() {
  const { t } = useTranslation()
  const dispatch = useDispatch()
  const navigate = useNavigate()
  const location = useLocation()
  const path = useLocalizedPath()
  const status = useSelector(selectAuthStatus)
  const user = useSelector(selectUser)
  const notice = useSelector(selectNotice)
  const [shownNotice] = useState(notice)
  const [phone, setPhone] = useState('')
  const [code, setCode] = useState('')
  const [step, setStep] = useState('phone')
  const [resendAt, setResendAt] = useState(null)
  const [name, setName] = useState('')
  const [email, setEmail] = useState('')
  const [sendCode, sending] = useSendCodeMutation()
  const [verifyCode, verifying] = useVerifyCodeMutation()
  const [updateProfile, saving] = useUpdateProfileMutation()
  const secondsLeft = useSecondsLeft(resendAt)
  const target = location.state?.from?.pathname ?? path('/')

  useEffect(() => {
    if (notice) dispatch(noticeShown())
  }, [notice, dispatch])

  if (status === 'authenticated' && user.isProfileComplete && step !== 'done') {
    return <Navigate replace to={target} />
  }

  const needsProfile = status === 'authenticated' && !user.isProfileComplete
  const current = needsProfile ? 'profile' : step

  const requestCode = async (event) => {
    event?.preventDefault()
    const result = await sendCode(phone.trim())
    if (result.data) {
      setResendAt(Date.now() + result.data.resendAfterSeconds * 1000)
      setCode('')
      setStep('code')
    }
  }

  const confirmCode = async (event) => {
    event.preventDefault()
    const result = await verifyCode({ phone: phone.trim(), code: code.trim() })
    if (result.data?.user.isProfileComplete) {
      setStep('done')
      navigate(target, { replace: true })
    }
  }

  const saveProfile = async (event) => {
    event.preventDefault()
    const result = await updateProfile({ fullName: name.trim(), email: email.trim() || null })
    if (result.data) {
      setStep('done')
      navigate(target, { replace: true })
    }
  }

  const sendErrors = fieldErrors(t, sending.error)
  const verifyErrors = fieldErrors(t, verifying.error)
  const profileErrors = fieldErrors(t, saving.error)

  return (
    <section className={styles['sign-in']}>
      <div className={styles['sign-in__card']}>
        <BrandMark size={48} />
        {shownNotice && current === 'phone' && <Alert tone="warning" title={t(`errors.${shownNotice}`, { defaultValue: t('errors.session.expired') })} />}

        {current === 'phone' && (
          <form className={styles['sign-in__form']} onSubmit={requestCode} noValidate>
            <h1 className={styles['sign-in__title']}>{t('auth.phoneTitle')}</h1>
            <p className={styles['sign-in__lead']}>{t('auth.phoneLead')}</p>
            <TextField
              label={t('auth.phoneLabel')}
              hint={t('auth.phoneHint')}
              type="tel"
              inputMode="tel"
              autoComplete="tel"
              required
              value={phone}
              onChange={(event) => setPhone(event.target.value)}
              error={sendErrors.phone ?? (sending.error && !sendErrors.phone ? errorMessage(t, sending.error) : undefined)}
            />
            <Button type="submit" size="lg" block loading={sending.isLoading} disabled={!phone.trim()}>
              {t('auth.sendCode')}
            </Button>
          </form>
        )}

        {current === 'code' && (
          <form className={styles['sign-in__form']} onSubmit={confirmCode} noValidate>
            <h1 className={styles['sign-in__title']}>{t('auth.codeTitle')}</h1>
            <p className={styles['sign-in__lead']}>{t('auth.codeLead', { phone: phone.trim() })}</p>
            <TextField
              label={t('auth.codeLabel')}
              inputMode="numeric"
              autoComplete="one-time-code"
              maxLength={6}
              required
              value={code}
              onChange={(event) => setCode(event.target.value.replace(/\D/g, ''))}
              error={verifyErrors.code ?? (verifying.error ? errorMessage(t, verifying.error) : undefined)}
            />
            <Button type="submit" size="lg" block loading={verifying.isLoading} disabled={code.length !== 6}>
              {t('auth.confirm')}
            </Button>
            <div className={styles['sign-in__links']}>
              <Button variant="ghost" size="sm" onClick={() => setStep('phone')}>
                {t('auth.changePhone')}
              </Button>
              <Button variant="ghost" size="sm" onClick={requestCode} disabled={secondsLeft > 0} loading={sending.isLoading}>
                {secondsLeft > 0 ? t('auth.resendIn', { seconds: secondsLeft }) : t('auth.resend')}
              </Button>
            </div>
            {sending.error && <Alert tone="danger" title={errorMessage(t, sending.error)} />}
          </form>
        )}

        {current === 'profile' && (
          <form className={styles['sign-in__form']} onSubmit={saveProfile} noValidate>
            <h1 className={styles['sign-in__title']}>{t('auth.profileTitle')}</h1>
            <p className={styles['sign-in__lead']}>{t('auth.profileLead')}</p>
            <TextField
              label={t('account.name')}
              autoComplete="name"
              required
              value={name}
              onChange={(event) => setName(event.target.value)}
              error={profileErrors.fullName}
            />
            <TextField
              label={t('account.email')}
              optionalText={t('common.optional')}
              type="email"
              autoComplete="email"
              value={email}
              onChange={(event) => setEmail(event.target.value)}
              error={profileErrors.email}
            />
            {saving.error && !profileErrors.fullName && !profileErrors.email && <Alert tone="danger" title={errorMessage(t, saving.error)} />}
            <Button type="submit" size="lg" block loading={saving.isLoading} disabled={!name.trim()}>
              {t('auth.finish')}
            </Button>
          </form>
        )}
      </div>
    </section>
  )
}
