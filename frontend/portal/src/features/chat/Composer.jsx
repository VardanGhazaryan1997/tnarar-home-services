import { useRef, useState } from 'react'
import { useTranslation } from 'react-i18next'
import Button from '@/components/ui/Button/Button'
import Icon from '@/components/ui/Icon/Icon'
import Spinner from '@/components/ui/Spinner/Spinner'
import { CHAT_TYPES, useFileUploads } from '@/features/files/useFileUploads'
import { bem } from '@/shared/bem'
import { newClientId, useOutboxSender } from './outbox'
import styles from './chat.module.scss'

const b = bem(styles)

/**
 * Writing a message: text (Enter sends, Shift+Enter starts a new line) and up to 5 photos, videos or PDFs. A sent
 * message shows in the thread at once as sending; if it fails it stays there, with its text, to try again.
 */
export default function Composer({ conversationId, onSent }) {
  const { t } = useTranslation()
  const [text, setText] = useState('')
  const outbox = useOutboxSender(conversationId)
  const uploads = useFileUploads({ max: 5, types: CHAT_TYPES })
  const fileInput = useRef(null)
  const canSend = (text.trim() || uploads.fileIds.length > 0) && !uploads.busy

  const submit = (event) => {
    event?.preventDefault()
    if (!canSend) return
    const files = uploads.items.filter((item) => item.fileId).map((item) => ({ name: item.name }))
    outbox.send({ clientId: newClientId(), body: text.trim(), fileIds: uploads.fileIds, files, at: new Date().toISOString() })
    setText('')
    uploads.clear()
    onSent?.()
  }

  const onKeyDown = (event) => {
    if (event.key === 'Enter' && !event.shiftKey) submit(event)
  }

  return (
    <form className={styles.composer} onSubmit={submit}>
      {uploads.items.length > 0 && (
        <ul className={styles['composer__files']}>
          {uploads.items.map((item) => (
            <li key={item.key} className={b('composer__file', { error: item.status === 'error' })}>
              {item.status === 'uploading' && <Spinner size="sm" />}
              <span className={styles['composer__file-name']}>{item.error ? `${item.name}: ${t(`files.${item.error}`)}` : item.name}</span>
              <button type="button" className={styles['composer__remove']} onClick={() => uploads.remove(item.key)} aria-label={t('files.remove', { name: item.name })}>
                <Icon name="close" size={14} />
              </button>
            </li>
          ))}
        </ul>
      )}
      <div className={styles['composer__row']}>
        <Button variant="ghost" icon={<Icon name="plus" />} aria-label={t('chat.attach')} disabled={uploads.full} onClick={() => fileInput.current.click()} />
        <input
          ref={fileInput}
          type="file"
          multiple
          tabIndex={-1}
          aria-label={t('chat.attach')}
          accept={Object.keys(CHAT_TYPES).join(',')}
          className={styles['composer__input']}
          onChange={(event) => {
            uploads.add(event.target.files)
            event.target.value = ''
          }}
        />
        <textarea
          className={styles['composer__text']}
          rows={1}
          maxLength={4000}
          placeholder={t('chat.placeholder')}
          aria-label={t('chat.placeholder')}
          value={text}
          onChange={(event) => setText(event.target.value)}
          onKeyDown={onKeyDown}
        />
        <Button type="submit" variant="accent" icon={<Icon name="send" />} aria-label={t('chat.send')} disabled={!canSend} />
      </div>
    </form>
  )
}
