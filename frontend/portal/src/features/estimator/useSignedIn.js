import { useSelector } from 'react-redux'
import { selectAuthStatus, selectUser } from '@/features/auth/authSlice'

/** True for a signed-in user who finished signing up (the ones who can save estimates). */
export function useSignedIn() {
  const status = useSelector(selectAuthStatus)
  const user = useSelector(selectUser)
  return status === 'authenticated' && Boolean(user?.isProfileComplete)
}
