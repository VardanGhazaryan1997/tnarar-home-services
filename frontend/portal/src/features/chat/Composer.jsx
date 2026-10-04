import { useRef, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { errorMessage } from '@/api/errors'
import Button from '@/components/ui/Button/Button'
import Icon from '@/components/ui/Icon/Icon'
import Spinner from '@/components/ui/Spinner/Spinner'
import { CHAT_TYPES, useFileUploads } from '@/features/files/useFileUploads'
import { bem } from '@/shared/bem'
import { useSendMessageMutation } from './chatApi'
import styles from './chat.module.scss'

const b = bem(styles)

/** Writing a message: text (Enter sends, Shift+Enter starts a new line) and up to 5 photos, videos or PDFs. */
export default function Composer({ conversationId }) {
  const { t } = useTranslation()
  const [text, setText] = useState('')
  const [send, sending] = useSendMessageMutation()
  const uploads = useFileUploads({ max: 5, types: CHAT_TYPES })
  const fileInput = useRef(null)
  const canSend = (text.trim() || uploads.fileIds.length > 0) && !uploads.busy && !sending.isLoading

  const submit = async (event) => {
    event?.preventDefault()
    if (!canSend) return
    const result = await send({ id: conversationId, body: text.trim(), fileIds: uploads.fileIds })
    if (result.data) {
      setText('')
      uploads.clear()
    }
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
      {sending.error && <p className={styles['composer__error']} role="alert">{errorMessage(t, sending.error)}</p>}
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
        <Button type="submit" variant="accent" icon={<Icon name="send" />} aria-label={t('chat.send')} disabled={!canSend} loading={sending.isLoading} />
      </div>
    </form>
  )
}
