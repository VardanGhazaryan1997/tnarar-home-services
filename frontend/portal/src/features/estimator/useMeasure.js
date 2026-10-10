import { useEffect, useMemo, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { measureRequest } from './draft'
import { useMeasureEstimateMutation } from './estimatorApi'

/** How long typing must pause before the estimate is priced again (ms). */
export const MEASURE_DELAY = 400

/**
 * Prices the draft on the server a moment after it stops changing. Returns `{ total, room(index), loading, error }`:
 * `total` is the whole result (null until the first rooms can be measured), `room(key)` the measured room for the
 * draft's room with that key (null while its sizes aren't valid). While a new price is on its way the last one stays.
 */
export function useMeasure(draft) {
  const { i18n } = useTranslation()
  const [measure] = useMeasureEstimateMutation()
  const [state, setState] = useState({ key: null, result: null, error: null })
  // The request as text: the same estimate typed again doesn't price it again.
  const key = useMemo(() => {
    const request = measureRequest(draft)
    return request && JSON.stringify({ body: request.body, keys: request.indexes.map((index) => draft.rooms[index].key) })
  }, [draft])

  useEffect(() => {
    if (!key) return undefined
    let cancelled = false
    const { body, keys } = JSON.parse(key)
    const timer = setTimeout(async () => {
      try {
        const result = await measure(body).unwrap()
        if (!cancelled) setState({ key, result: { ...result, keys }, error: null })
      } catch (error) {
        if (!cancelled) setState({ key, result: null, error })
      }
    }, MEASURE_DELAY)
    return () => {
      cancelled = true
      clearTimeout(timer)
    }
  }, [key, measure, i18n.language])

  const result = key ? state.result : null
  return {
    total: result,
    room: (roomKey) => {
      const at = result?.keys.indexOf(roomKey) ?? -1
      return at >= 0 ? result.rooms[at] : null
    },
    loading: Boolean(key) && state.key !== key,
    error: key && state.key === key ? state.error : null,
  }
}
