import { DeleteOutlined, UploadOutlined } from '@ant-design/icons'
import { Button, Flex, Image, Typography } from 'antd'
import { useRef } from 'react'
import { useTranslation } from 'react-i18next'
import { IMAGE_ACCEPT, useImageUpload } from './useImageUpload'

/**
 * Picks one picture and uploads it. A form control: `value` is `{ fileId, url }` (url: a link to show it) or empty;
 * `onChange` gets the same shape, `{ uploading: true, … }` while the upload runs (so a form can wait for it), or null
 * when the picture is removed.
 */
export default function ImagePicker({ value, onChange, label }) {
  const { t } = useTranslation()
  const { upload, busy, error } = useImageUpload()
  const input = useRef(null)

  const choose = async (event) => {
    const [file] = event.target.files
    event.target.value = ''
    if (!file) return
    onChange?.({ ...value, uploading: true })
    const done = await upload(file)
    onChange?.(done ? { fileId: done.id, url: done.thumbnailUrl ?? done.url ?? null } : (value ?? null))
  }

  return (
    <Flex align="center" gap="small" wrap>
      {value?.url && <Image width={72} height={72} style={{ objectFit: 'cover' }} src={value.url} alt={t('files.preview')} />}
      <input ref={input} type="file" hidden accept={IMAGE_ACCEPT} aria-label={label ?? t('files.choose')} onChange={choose} />
      <Button icon={<UploadOutlined />} loading={busy} onClick={() => input.current?.click()}>
        {value?.fileId ? t('files.replace') : t('files.choose')}
      </Button>
      {value?.fileId && !busy && <Button type="text" danger icon={<DeleteOutlined />} aria-label={t('files.remove')} onClick={() => onChange?.(null)} />}
      {error && <Typography.Text type="danger">{t(`files.${error}`)}</Typography.Text>}
    </Flex>
  )
}
