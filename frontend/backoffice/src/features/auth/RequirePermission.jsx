import { Result } from 'antd'
import { useTranslation } from 'react-i18next'
import { useSelector } from 'react-redux'
import { selectStaff } from './authSlice'
import { hasPermission } from './permissions'

/** Shows `children` only to staff with `permission`; others see a "no access" page. */
export default function RequirePermission({ permission, children }) {
  const { t } = useTranslation()
  const staff = useSelector(selectStaff)

  if (!hasPermission(staff, permission)) {
    return <Result status="403" title={t('forbidden.title')} subTitle={t('forbidden.text')} />
  }

  return children
}
