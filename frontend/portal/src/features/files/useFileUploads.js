import { useCallback, useState } from 'react'
import { useCompleteUploadMutation, useRequestUploadMutation } from './uploadsApi'

const MB = 1024 * 1024

/** What the API accepts for request photos and videos, with the size limit of each kind. */
export const MEDIA_TYPES = {
  'image/jpeg': 10 * MB,
  'image/png': 10 * MB,
  'image/webp': 10 * MB,
  'video/mp4': 200 * MB,
  'video/quicktime': 200 * MB,
}

/** Chat also takes PDFs (quotes, plans). */
export const CHAT_TYPES = { ...MEDIA_TYPES, 'application/pdf': 10 * MB }

let nextKey = 0

/**
 * Uploads photos and videos straight to storage: ask the API for a signed URL, PUT the bytes there,
 * then confirm. Each item: { key, name, kind, previewUrl, status: uploading | done | error, fileId, error }.
 * `error` is a translation key under "files.".
 */
export function useFileUploads({ max = 10, types = MEDIA_TYPES } = {}) {
  const [items, setItems] = useState([])
  const [requestUpload] = useRequestUploadMutation()
  const [completeUpload] = useCompleteUploadMutation()

  const update = (key, changes) => setItems((current) => current.map((item) => (item.key === key ? { ...item, ...changes } : item)))

  const uploadOne = useCallback(
    async (item, file) => {
      const ticket = await requestUpload({ fileName: file.name, contentType: file.type, size: file.size })
      if (!ticket.data) return update(item.key, { status: 'error', error: 'uploadFailed' })
      try {
        const response = await fetch(ticket.data.uploadUrl, { method: ticket.data.method, headers: ticket.data.headers, body: file })
        if (!response.ok) throw new Error(String(response.status))
      } catch {
        return update(item.key, { status: 'error', error: 'uploadFailed' })
      }
      const done = await completeUpload(ticket.data.fileId)
      return update(item.key, done.data ? { status: 'done', fileId: done.data.id } : { status: 'error', error: 'uploadFailed' })
    },
    [requestUpload, completeUpload],
  )

  const add = (fileList) => {
    const files = [...fileList]
    const room = Math.max(0, max - items.length)
    const accepted = files.slice(0, room).map((file) => {
      const limit = types[file.type]
      const error = !limit ? 'typeNotAllowed' : file.size > limit ? 'tooLarge' : null
      const item = {
        key: `upload-${(nextKey += 1)}`,
        name: file.name,
        kind: file.type.startsWith('video/') ? 'video' : 'image',
        previewUrl: !error && file.type.startsWith('image/') ? (URL.createObjectURL?.(file) ?? null) : null,
        status: error ? 'error' : 'uploading',
        fileId: null,
        error,
      }
      return { item, file }
    })
    setItems((current) => [...current, ...accepted.map((a) => a.item)])
    accepted.filter((a) => !a.item.error).forEach((a) => uploadOne(a.item, a.file))
    return files.length - accepted.length
  }

  const remove = (key) => setItems((current) => current.filter((item) => item.key !== key))
  const clear = () => setItems([])

  return {
    items,
    add,
    remove,
    clear,
    fileIds: items.filter((item) => item.status === 'done').map((item) => item.fileId),
    busy: items.some((item) => item.status === 'uploading'),
    full: items.length >= max,
  }
}
