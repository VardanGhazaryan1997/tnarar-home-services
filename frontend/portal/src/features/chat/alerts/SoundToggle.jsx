import { useTranslation } from 'react-i18next'
import Icon from '@/components/ui/Icon/Icon'
import { bem } from '@/shared/bem'
import { setMuted, useMuted } from './sound'
import styles from './SoundToggle.module.scss'

const b = bem(styles)

/** The header switch for the new-message sound: on by default, remembered on this device. */
export default function SoundToggle() {
  const { t } = useTranslation()
  const muted = useMuted()
  const label = muted ? t('chat.sound.off') : t('chat.sound.on')

  return (
    <button
      type="button"
      className={b('sound-toggle', { muted })}
      aria-pressed={!muted}
      aria-label={label}
      title={label}
      onClick={() => setMuted(!muted)}
    >
      <Icon name={muted ? 'volumeOff' : 'volume'} size={20} />
    </button>
  )
}
