/**
 * The main service most of an estimate's work belongs to (by number of lines), to start a request from it: work items
 * name their subcategory, which belongs to a main category. Null when none of the work is known.
 */
export function mainCategoryOf(estimate, workItems, categories) {
  const mainOf = new Map()
  for (const main of categories) {
    mainOf.set(main.id, main.id)
    for (const sub of main.children ?? []) mainOf.set(sub.id, main.id)
  }
  const categoryOf = new Map(workItems.map((item) => [item.id, mainOf.get(item.categoryId)]))
  const counts = new Map()
  for (const room of estimate.rooms) {
    for (const line of room.lines) {
      const main = categoryOf.get(line.workItemId)
      if (main) counts.set(main, (counts.get(main) ?? 0) + 1)
    }
  }
  let best = null
  for (const [id, count] of counts) if (!best || count > counts.get(best)) best = id
  return best
}
