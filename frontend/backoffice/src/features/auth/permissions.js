/** Super Admins have every permission; other staff have their roles' permissions. No permission needed → allowed. */
export function hasPermission(staff, permission) {
  if (!permission) return true
  if (!staff) return false
  return staff.isSuperAdmin || staff.permissions.includes(permission)
}
