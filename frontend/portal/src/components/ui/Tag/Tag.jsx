import { bem } from '@/shared/bem'
import styles from './Tag.module.scss'

const b = bem(styles)

/** A small status label. `tone`: neutral | info | success | warning | danger | accent. */
export default function Tag({ tone = 'neutral', className, children }) {
  return <span className={b('tag', { tone }, className)}>{children}</span>
}
