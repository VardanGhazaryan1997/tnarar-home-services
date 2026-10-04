import { useTranslation } from 'react-i18next'
import { useParams } from 'react-router'
import EmptyState from '@/components/ui/EmptyState/EmptyState'
import { bem } from '@/shared/bem'
import ConversationList from './ConversationList'
import Thread from './Thread'
import styles from './chat.module.scss'

const b = bem(styles)

/** Messages: the conversation list and the open conversation side by side; phones show one at a time. */
export default function MessagesPage() {
  const { t } = useTranslation()
  const { id } = useParams()

  return (
    <div className={b('messages-page', { open: Boolean(id) })}>
      <aside className={styles['messages-page__list']} aria-label={t('chat.listLabel')}>
        <h1 className={styles['messages-page__title']}>{t('chat.title')}</h1>
        <ConversationList activeId={id} />
      </aside>
      <section className={styles['messages-page__thread']}>
        {id ? <Thread key={id} id={id} /> : <EmptyState icon="messages" title={t('chat.pickTitle')} description={t('chat.pickText')} />}
      </section>
    </div>
  )
}
