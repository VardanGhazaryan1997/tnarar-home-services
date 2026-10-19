import { useEffect } from 'react'
import { useDispatch, useSelector } from 'react-redux'
import { changeSeen, customerChange, partnerChange, selectLiveEntry } from './liveSlice'

/** How long a change stays pointed out once the user can see the page. */
export const SEEN_AFTER_MS = 8000

/** Each change's tone, icon and words: never color alone. */
export const LIVE_KINDS = {
  cancelled: { tone: 'danger', icon: 'close' },
  accepted: { tone: 'success', icon: 'check' },
  offerAccepted: { tone: 'success', icon: 'check' },
  offer: { tone: 'info', icon: 'send' },
  offerRejected: { tone: 'danger', icon: 'close' },
  offerWithdrawn: { tone: 'neutral', icon: 'info' },
  closed: { tone: 'neutral', icon: 'info' },
  closedOther: { tone: 'neutral', icon: 'info' },
}

/**
 * The newest change on a request, as `side` ("customer" or "partner") should read it, while it is fresh. The
 * countdown to forgetting it runs only while the tab is visible, so a change in a background tab still shows
 * when the user comes back.
 */
export function useLiveChange(requestId, side) {
  const dispatch = useDispatch()
  const entry = useSelector(selectLiveEntry(requestId))
  const change = side === 'partner' ? partnerChange(entry) : customerChange(entry)
  const key = change ? `${change.kind}@${change.at}` : null

  useEffect(() => {
    if (!key) return undefined
    let timer = null
    const start = () => {
      if (timer || document.visibilityState !== 'visible') return
      timer = setTimeout(() => dispatch(changeSeen(requestId)), SEEN_AFTER_MS)
    }
    start()
    document.addEventListener('visibilitychange', start)
    return () => {
      clearTimeout(timer)
      document.removeEventListener('visibilitychange', start)
    }
  }, [key, requestId, dispatch])

  return change
}
