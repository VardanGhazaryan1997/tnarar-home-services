import { useState } from 'react'
import { useGetMyPricesQuery, useSaveMyPricesMutation } from './pricesApi'
import { draftErrors, draftFrom, fillWithTypical, isChanged, toPayload } from './priceList'

/**
 * The partner's price list being edited: the loaded items, the rows typed so far and saving. Rows only show errors
 * after a save attempt. `save()` resolves to true when the list was saved (or nothing changed).
 */
export function usePriceList() {
  const query = useGetMyPricesQuery(undefined, { refetchOnMountOrArgChange: true })
  const [saveMutation, saving] = useSaveMyPricesMutation()
  const [edited, setEdited] = useState(null)
  const [showErrors, setShowErrors] = useState(false)
  const items = query.data?.items ?? []
  const draft = edited ?? draftFrom(items)
  const errors = draftErrors(draft)

  const change = (workItemId, patch) =>
    setEdited((current) => {
      const base = current ?? draftFrom(items)
      return { ...base, [workItemId]: { ...(base[workItemId] ?? { from: '', to: '', materials: false }), ...patch } }
    })

  const fillTypical = () => setEdited((current) => fillWithTypical(current ?? draftFrom(items), items))

  const save = async () => {
    if (Object.keys(errors).length > 0) {
      setShowErrors(true)
      return false
    }
    if (!isChanged(draft, items)) return true
    saving.reset()
    const result = await saveMutation(toPayload(draft))
    if (result.error) return false
    setEdited(null)
    setShowErrors(false)
    return true
  }

  return {
    query,
    items,
    draft,
    errors: showErrors ? errors : {},
    hasErrors: showErrors && Object.keys(errors).length > 0,
    changed: edited !== null && isChanged(draft, items),
    change,
    fillTypical,
    save,
    saving,
  }
}
