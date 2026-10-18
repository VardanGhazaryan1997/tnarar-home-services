import { useEffect, useRef, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { useDispatch, useSelector } from 'react-redux'
import { Link } from 'react-router'
import Icon from '@/components/ui/Icon/Icon'
import { useLocalizedPath } from '@/i18n/hooks'
import { initials } from '../initials'
import { alertDismissed, selectMessageAlerts } from './alertsSlice'
import styles from './MessageAlerts.module.scss'

/** How long an alert stays, unless the pointer or keyboard focus is on it. */
export const ALERT_MS = 6000

function Alert({ alert }) {
  const { t } = useTranslation()
  const dispatch = useDispatch()
  const path = useLocalizedPath()
  const [paused, setPaused] = useState(false)
  const name = alert.name ?? t(`chat.role.${alert.senderRole}`)
  const preview = alert.body ?? t('chat.files', { n: alert.files })
  const dismiss = () => dispatch(alertDismissed(alert.id))

  useEffect(() => {
    if (paused) return undefined
    const timer = setTimeout(() => dispatch(alertDismissed(alert.id)), ALERT_MS)
    return () => clearTimeout(timer)
  }, [paused, alert.id, dispatch])

  return (
    <li
      className={styles['message-alerts__item']}
      role="status"
      onMouseEnter={() => setPaused(true)}
      onMouseLeave={() => setPaused(false)}
      onFocus={() => setPaused(true)}
      onBlur={() => setPaused(false)}
    >
      <span className={styles['message-alerts__avatar']} aria-hidden="true">
        {initials(name)}
      </span>
      <div className={styles['message-alerts__text']}>
        <p className={styles['message-alerts__title']}>
          <Icon name="messages" size={16} />
          {t('chat.alerts.title', { name })}
        </p>
        <p className={styles['message-alerts__preview']}>{preview}</p>
      </div>
      <Link to={path(`/messages/${alert.conversationId}`)} className={styles['message-alerts__open']} onClick={dismiss}>
        {t('chat.alerts.open')}
      </Link>
      <button type="button" className={styles['message-alerts__close']} onClick={dismiss} aria-label={t('chat.alerts.dismiss')}>
        <Icon name="close" size={18} />
      </button>
    </li>
  )
}

/**
 * New-message alerts, stacked at the top of the screen. While the tab is in the background, the tab title also
 * counts the new messages, until the user comes back.
 */
export default function MessageAlerts() {
  const { t } = useTranslation()
  const alerts = useSelector(selectMessageAlerts)
  const unseen = useHiddenTabCount(alerts)

  useEffect(() => {
    if (unseen === 0) return undefined
    const original = document.title
    document.title = `${t('chat.alerts.tabTitle', { n: unseen })} · ${original}`
    return () => {
      document.title = original
    }
  }, [unseen, t])

  return (
    <section className={styles['message-alerts']} aria-label={t('chat.alerts.region')}>
      {alerts.length > 0 && (
        <ol className={styles['message-alerts__list']}>
          {alerts.map((alert) => (
            <Alert key={alert.id} alert={alert} />
          ))}
        </ol>
      )}
    </section>
  )
}

/** Counts alerts that arrived while the tab was hidden; back to 0 when the user returns. */
function useHiddenTabCount(alerts) {
  const [count, setCount] = useState(0)
  const seen = useRef(new Set(alerts.map((alert) => alert.id)))

  useEffect(() => {
    const fresh = alerts.filter((alert) => !seen.current.has(alert.id))
    fresh.forEach((alert) => seen.current.add(alert.id))
    if (fresh.length > 0 && document.visibilityState === 'hidden') setCount((n) => n + fresh.length)
  }, [alerts])

  useEffect(() => {
    const onVisible = () => document.visibilityState === 'visible' && setCount(0)
    document.addEventListener('visibilitychange', onVisible)
    return () => document.removeEventListener('visibilitychange', onVisible)
  }, [])

  return count
}
