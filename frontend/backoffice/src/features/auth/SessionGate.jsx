import { Flex, Spin } from 'antd'
import { useEffect } from 'react'
import { useTranslation } from 'react-i18next'
import { useDispatch, useSelector } from 'react-redux'
import { Outlet } from 'react-router'
import { bootstrapSession } from './authApi'
import { selectAuthStatus } from './authSlice'

/** Root route: restores the session from the refresh cookie before showing any page. */
export default function SessionGate() {
  const { t } = useTranslation()
  const dispatch = useDispatch()
  const status = useSelector(selectAuthStatus)

  useEffect(() => {
    dispatch(bootstrapSession())
  }, [dispatch])

  if (status === 'unknown') {
    return (
      <Flex align="center" justify="center" style={{ minHeight: '100vh' }}>
        <Spin size="large" description={t('auth.loading')} />
      </Flex>
    )
  }

  return <Outlet />
}
