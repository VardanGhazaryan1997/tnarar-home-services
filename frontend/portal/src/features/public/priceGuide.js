/**
 * The price guide: priced work items grouped by main service, then subcategory, in catalog order:
 * `[{ id, name, groups: [{ id, name, items }] }]`. `search` matches work and service names; `category` keeps one main
 * service.
 */
export function priceGuide(categories, workItems, { search = '', category = '' } = {}) {
  const term = search.trim().toLocaleLowerCase()
  const byCategory = new Map()
  for (const item of workItems) {
    if (item.priceTypical == null) continue
    const list = byCategory.get(item.categoryId) ?? []
    list.push(item)
    byCategory.set(item.categoryId, list)
  }

  return categories
    .filter((main) => !category || main.id === category)
    .map((main) => {
      const mainMatches = term && main.name.toLocaleLowerCase().includes(term)
      const groups = (main.children ?? [])
        .map((sub) => {
          const subMatches = mainMatches || (term && sub.name.toLocaleLowerCase().includes(term))
          const items = (byCategory.get(sub.id) ?? []).filter((item) => !term || subMatches || item.name.toLocaleLowerCase().includes(term))
          return { id: sub.id, name: sub.name, items }
        })
        .filter((group) => group.items.length > 0)
      return { id: main.id, name: main.name, groups }
    })
    .filter((main) => main.groups.length > 0)
}
