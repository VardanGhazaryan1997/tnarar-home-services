import { useCallback, useState } from 'react'
import { useCompleteUploadMutation, useRequestUploadMutation } from './uploadsApi'

const MB = 1024 * 1024

/** The picture types the API accepts, with the size limit of each. */
export const IMAGE_TYPES = {
  'image/jpeg': 10 * MB,
  'image/png': 10 * MB,
  'image/webp': 10 * MB,
}

export const IMAGE_ACCEPT = Object.keys(IMAGE_TYPES).join(',')

/**
 * Uploads one picture straight to storage: ask the API for a signed URL, PUT the bytes there, then confirm.
 * `upload(file)` resolves to the file (`{ id, url, thumbnailUrl, … }`) or null when it failed; `error` is then a
 * translation key under "files." (`typeNotAllowed`, `tooLarge`, `uploadFailed`).
 */
export function useImageUpload() {
  const [state, setState] = useState({ busy: false, error: null })
  const [requestUpload] = useRequestUploadMutation()
  const [completeUpload] = useCompleteUploadMutation()

  const upload = useCallback(
    async (file) => {
      const fail = (error) => {
        setState({ busy: false, error })
        return null
      }
      const limit = IMAGE_TYPES[file.type]
      if (!limit) return fail('typeNotAllowed')
      if (file.size > limit) return fail('tooLarge')

      setState({ busy: true, error: null })
      const ticket = await requestUpload({ fileName: file.name, contentType: file.type, size: file.size })
      if (!ticket.data) return fail('uploadFailed')
      try {
        const response = await fetch(ticket.data.uploadUrl, { method: ticket.data.method, headers: ticket.data.headers, body: file })
        if (!response.ok) throw new Error(String(response.status))
      } catch {
        return fail('uploadFailed')
      }
      const done = await completeUpload(ticket.data.fileId)
      if (!done.data) return fail('uploadFailed')
      setState({ busy: false, error: null })
      return done.data
    },
    [requestUpload, completeUpload],
  )

  return { upload, busy: state.busy, error: state.error }
}
