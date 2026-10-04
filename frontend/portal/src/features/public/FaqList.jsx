import styles from './FaqList.module.scss'

/** Questions that open to show their answer (native details/summary, so it works without script). */
export default function FaqList({ items }) {
  return (
    <div className={styles['faq-list']}>
      {items.map((item) => (
        <details key={item.id} className={styles['faq-list__item']}>
          <summary className={styles['faq-list__question']}>{item.question}</summary>
          <p className={styles['faq-list__answer']}>{item.answer}</p>
        </details>
      ))}
    </div>
  )
}
