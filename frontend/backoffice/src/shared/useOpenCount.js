import { useState } from 'react'

/**
 * Counts how many times a dialog has opened. Used as the `key` of a form inside an Ant Design Modal, so each opening
 * starts with a fresh form: the Modal keeps showing its old content while it closes, so the form never unmounts
 * when it closes quickly.
 */
export function useOpenCount(open) {
  const [count, setCount] = useState(0)
  const [wasOpen, setWasOpen] = useState(false)
  if (open !== wasOpen) {
    setWasOpen(open)
    if (open) setCount(count + 1)
  }
  return count
}
