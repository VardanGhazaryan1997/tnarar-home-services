import { useSelector } from 'react-redux'
import { Navigate, Outlet, useLocation } from 'react-router'
import { selectAuthStatus } from './authSlice'

const pathOf = (location) => (location ? `${location.pathname}${location.search ?? ''}${location.hash ?? ''}` : '/')

/** Sign-in pages. Once signed in, continues to the page the staff member wanted (or the dashboard). */
export default function GuestOnly() {
  const status = useSelector(selectAuthStatus)
  const location = useLocation()

  if (status === 'authenticated') {
    return <Navigate replace to={pathOf(location.state?.from)} />
  }

  return <Outlet />
}
