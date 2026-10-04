import { bem } from '@/shared/bem'
import styles from './Card.module.scss'

const b = bem(styles)

/** A white panel. `title` and `actions` make a header row; `as` changes the element (section by default). */
export default function Card({ as: Element = 'section', title, actions, flush = false, className, children, ...rest }) {
  return (
    <Element className={b('card', { flush }, className)} {...rest}>
      {(title || actions) && (
        <header className={styles['card__header']}>
          {title && <h2 className={styles['card__title']}>{title}</h2>}
          {actions && <div className={styles['card__actions']}>{actions}</div>}
        </header>
      )}
      {children}
    </Element>
  )
}
