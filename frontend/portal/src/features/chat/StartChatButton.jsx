import { useTranslation } from 'react-i18next'
import { useNavigate } from 'react-router'
import Button from '@/components/ui/Button/Button'
import Icon from '@/components/ui/Icon/Icon'
import { useLocalizedPath } from '@/i18n/hooks'
import { useOpenConversationMutation } from './chatApi'

/** Opens the conversation about a request (with `partnerId` when the customer starts it) and goes to it. */
export default function StartChatButton({ requestId, partnerId, label, variant = 'secondary', size, block }) {
  const { t } = useTranslation()
  const navigate = useNavigate()
  const path = useLocalizedPath()
  const [open, opening] = useOpenConversationMutation()

  const start = async () => {
    const result = await open({ requestId, partnerId })
    if (result.data) navigate(path(`/messages/${result.data.id}`))
  }

  return (
    <Button variant={variant} size={size} block={block} icon={<Icon name="messages" />} loading={opening.isLoading} onClick={start}>
      {opening.isError ? t('chat.openFailed') : (label ?? t('chat.message'))}
    </Button>
  )
}
