import { initials } from '@/features/chat/initials'
import { bem } from '@/shared/bem'
import styles from './Avatar.module.scss'

const b = bem(styles)

/** A round picture of a person or company, or their initials when there's none. `size`: sm | md | lg | xl. */
export default function Avatar({ name, src, size = 'md', className }) {
  return (
    <span className={b('avatar', { size }, className)} aria-hidden="true">
      {src ? <img src={src} alt="" className={styles['avatar__image']} /> : initials(name)}
    </span>
  )
}
