import {
  AppstoreOutlined,
  DashboardOutlined,
  FileTextOutlined,
  HistoryOutlined,
  InboxOutlined,
  PercentageOutlined,
  SafetyOutlined,
  ShoppingOutlined,
  StarOutlined,
  TeamOutlined,
  TranslationOutlined,
  UserOutlined,
  WalletOutlined,
} from '@ant-design/icons'
import { hasPermission } from '@/features/auth/permissions'

// Side-menu entries. Labels come from `nav.<key>` translations. An entry with a
// `permission` is shown only to staff who have it (Super Admins see everything).
export const NAV_ITEMS = [
  { key: 'dashboard', path: '/', icon: <DashboardOutlined /> },
  { key: 'requests', path: '/requests', icon: <InboxOutlined />, permission: 'requests.view' },
  { key: 'orders', path: '/orders', icon: <ShoppingOutlined />, permission: 'orders.view' },
  { key: 'payments', path: '/payments', icon: <WalletOutlined />, permission: 'payments.view' },
  { key: 'commissions', path: '/commissions', icon: <PercentageOutlined />, permission: 'commissions.view' },
  { key: 'reviews', path: '/reviews', icon: <StarOutlined />, permission: 'reviews.moderate' },
  { key: 'partners', path: '/partners', icon: <TeamOutlined />, permission: 'partners.view' },
  { key: 'users', path: '/users', icon: <UserOutlined />, permission: 'users.view' },
  { key: 'catalog', path: '/catalog', icon: <AppstoreOutlined />, permission: 'catalog.manage' },
  { key: 'content', path: '/content', icon: <FileTextOutlined />, permission: 'content.manage' },
  { key: 'translations', path: '/translations', icon: <TranslationOutlined />, permission: 'translations.manage' },
  { key: 'staff', path: '/staff', icon: <SafetyOutlined />, permission: 'staff.view' },
  { key: 'audit', path: '/audit', icon: <HistoryOutlined />, permission: 'audit.view' },
]

export const navItemsFor = (staff) => NAV_ITEMS.filter((item) => hasPermission(staff, item.permission))

export const findNavItem = (pathname) =>
  NAV_ITEMS.find((item) => (item.path === '/' ? pathname === '/' : pathname.startsWith(item.path)))
