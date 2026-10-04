import { useSelector } from 'react-redux'
import { Navigate, Outlet } from 'react-router'
import { useLocalizedPath } from '@/i18n/hooks'
import { selectIsPartner } from './authSlice'

/** Partner-only pages (inbox, offers). Others go to their requests. Use inside RequireAuth. */
export default function RequirePartner() {
  const isPartner = useSelector(selectIsPartner)
  const path = useLocalizedPath()
  return isPartner ? <Outlet /> : <Navigate replace to={path('/requests')} />
}
