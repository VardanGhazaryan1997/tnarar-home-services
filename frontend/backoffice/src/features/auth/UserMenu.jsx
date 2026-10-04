import { LogoutOutlined, UserOutlined } from '@ant-design/icons'
import { Button, Dropdown, Grid } from 'antd'
import { useTranslation } from 'react-i18next'
import { useSelector } from 'react-redux'
import { useSignOutMutation } from './authApi'
import { selectStaff } from './authSlice'

/** The signed-in staff member's name, with "Sign out". On phones only the icon shows. */
export default function UserMenu() {
  const { t } = useTranslation()
  const staff = useSelector(selectStaff)
  const screens = Grid.useBreakpoint()
  const [signOut] = useSignOutMutation()

  const items = [
    { key: 'email', label: staff.email, disabled: true },
    { type: 'divider' },
    { key: 'signOut', icon: <LogoutOutlined />, label: t('auth.signOut') },
  ]

  const handleClick = ({ key }) => {
    if (key === 'signOut') signOut()
  }

  return (
    <Dropdown menu={{ items, onClick: handleClick }} trigger={['click']} placement="bottomRight">
      <Button type="text" icon={<UserOutlined />} aria-label={t('auth.userMenu')}>
        {screens.md ? staff.fullName : null}
      </Button>
    </Dropdown>
  )
}
