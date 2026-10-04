import { useSelector } from 'react-redux'
import { Navigate, Outlet, useLocation } from 'react-router'
import { selectAuthStatus } from './authSlice'

/** Pages for signed-in staff. Others go to the sign-in page and come back afterwards. */
export default function RequireAuth() {
  const status = useSelector(selectAuthStatus)
  const location = useLocation()

  if (status !== 'authenticated') {
    return <Navigate replace to="/login" state={{ from: location }} />
  }

  return <Outlet />
}
