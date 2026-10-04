import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { useSelector } from 'react-redux'
import { useNavigate } from 'react-router'
import { errorMessage, fieldErrors } from '@/api/errors'
import PageHeader from '@/components/PageHeader/PageHeader'
import Alert from '@/components/ui/Alert/Alert'
import Button from '@/components/ui/Button/Button'
import Card from '@/components/ui/Card/Card'
import { TextField } from '@/components/ui/Field/Field'
import Icon from '@/components/ui/Icon/Icon'
import Tag from '@/components/ui/Tag/Tag'
import { useSignOutMutation, useUpdateProfileMutation } from '@/features/auth/authApi'
import { selectIsPartner, selectUser } from '@/features/auth/authSlice'
import { useLocalizedPath } from '@/i18n/hooks'
import styles from './AccountPage.module.scss'

/** The signed-in user's details (name, email, phone), and signing out. */
export default function AccountPage() {
  const { t } = useTranslation()
  const user = useSelector(selectUser)
  const isPartner = useSelector(selectIsPartner)
  const navigate = useNavigate()
  const path = useLocalizedPath()
  const [name, setName] = useState(user.fullName ?? '')
  const [email, setEmail] = useState(user.email ?? '')
  const [saved, setSaved] = useState(false)
  const [updateProfile, saving] = useUpdateProfileMutation()
  const [signOut, signingOut] = useSignOutMutation()
  const errors = fieldErrors(t, saving.error)

  const save = async (event) => {
    event.preventDefault()
    setSaved(false)
    const result = await updateProfile({ fullName: name.trim(), email: email.trim() || null })
    if (result.data) setSaved(true)
  }

  const leave = async () => {
    await signOut()
    navigate(path('/'), { replace: true })
  }

  return (
    <div className={styles['account-page']}>
      <PageHeader title={t('account.title')} />
      <Card title={t('account.details')}>
        <form className={styles['account-page__form']} onSubmit={save} noValidate>
          <TextField label={t('account.phone')} value={user.phoneNumber} disabled hint={t('account.phoneHint')} />
          <TextField label={t('account.name')} required value={name} onChange={(event) => setName(event.target.value)} error={errors.fullName} />
          <TextField
            label={t('account.email')}
            optionalText={t('common.optional')}
            type="email"
            value={email}
            onChange={(event) => setEmail(event.target.value)}
            error={errors.email}
          />
          {saved && <Alert tone="success" title={t('account.saved')} />}
          {saving.error && !errors.fullName && !errors.email && <Alert tone="danger" title={errorMessage(t, saving.error)} />}
          <div className={styles['account-page__actions']}>
            <Button type="submit" loading={saving.isLoading} disabled={!name.trim()}>
              {t('common.save')}
            </Button>
          </div>
        </form>
      </Card>
      <Card title={t('account.roles')}>
        <div className={styles['account-page__roles']}>
          {user.roles.map((role) => (
            <Tag key={role} tone={role === 'Partner' ? 'accent' : 'neutral'}>
              {t(`account.role.${role}`, { defaultValue: role })}
            </Tag>
          ))}
        </div>
      </Card>
      <Card className={styles['account-page__partner']}>
        <div className={styles['account-page__partner-text']}>
          <p className={styles['account-page__partner-title']}>{isPartner ? t('account.partner.title') : t('account.partner.joinTitle')}</p>
          <p className={styles['account-page__partner-body']}>{isPartner ? t('account.partner.text') : t('account.partner.joinText')}</p>
        </div>
        <Button to={path('/partner')} variant={isPartner ? 'secondary' : 'accent'} icon={<Icon name={isPartner ? 'edit' : 'tools'} />}>
          {isPartner ? t('account.partner.open') : t('account.partner.join')}
        </Button>
      </Card>
      <Button variant="danger" icon={<Icon name="logout" />} onClick={leave} loading={signingOut.isLoading}>
        {t('account.signOut')}
      </Button>
    </div>
  )
}
