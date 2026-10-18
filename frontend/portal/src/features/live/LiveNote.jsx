import { useTranslation } from 'react-i18next'
import { Link } from 'react-router'
import Icon from '@/components/ui/Icon/Icon'
import { useLocalizedPath } from '@/i18n/hooks'
import { bem } from '@/shared/bem'
import { LIVE_KINDS } from './useLiveChange'
import styles from './LiveNote.module.scss'

const b = bem(styles)

/** A change, in words with an icon (announced to screen readers), and a link to the order an acceptance created. */
export default function LiveNote({ change, side, className }) {
  const { t } = useTranslation()
  const path = useLocalizedPath()
  const { tone, icon } = LIVE_KINDS[change.kind]

  return (
    <p className={b('live-note', { [tone]: true }, className)} role="status">
      <Icon name={icon} size={16} />
      <span>{t(`live.${side}.${change.kind}`)}</span>
      {change.orderId && (
        <Link to={path(`/orders/${change.orderId}`)} className={styles['live-note__link']}>
          {t('live.openOrder')}
        </Link>
      )}
    </p>
  )
}
