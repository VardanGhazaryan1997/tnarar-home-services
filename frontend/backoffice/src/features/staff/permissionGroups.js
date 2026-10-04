/** Permission codes ("area.action") grouped by area, keeping their order: [[area, codes], …]. */
export function groupPermissions(codes) {
  const groups = new Map()
  for (const code of codes) {
    const area = code.split('.')[0]
    groups.set(area, [...(groups.get(area) ?? []), code])
  }
  return [...groups.entries()]
}

/** A permission's name in the UI language; unknown codes show as they are. */
export const permissionLabel = (t, code) => t(`permissions.${code}`, { defaultValue: code })

export const permissionGroupLabel = (t, area) => t(`permissionGroups.${area}`, { defaultValue: area })
