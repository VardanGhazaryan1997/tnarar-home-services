import { useTranslation } from 'react-i18next'
import { useSelector } from 'react-redux'
import { Navigate, Outlet, useLocation } from 'react-router'
import Spinner from '@/components/ui/Spinner/Spinner'
import { useLocalizedPath } from '@/i18n/hooks'
import { selectAuthStatus, selectUser } from './authSlice'
import styles from './auth.module.scss'

/**
 * Pages for signed-in users. While the session is being restored it waits; visitors go to the sign-in
 * page and come back afterwards; users who haven't told us their name finish that first.
 */
export default function RequireAuth() {
  const { t } = useTranslation()
  const status = useSelector(selectAuthStatus)
  const user = useSelector(selectUser)
  const location = useLocation()
  const path = useLocalizedPath()

  if (status === 'unknown') {
    return (
      <div className={styles['auth-wait']}>
        <Spinner size="lg" label={t('common.loading')} />
      </div>
    )
  }

  if (status !== 'authenticated' || !user.isProfileComplete) {
    return <Navigate replace to={path('/sign-in')} state={{ from: location }} />
  }

  return <Outlet />
}
