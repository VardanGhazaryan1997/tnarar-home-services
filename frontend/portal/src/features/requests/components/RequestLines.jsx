import { useTranslation } from 'react-i18next'
import { intlLocale } from '@/i18n/languages'
import { formatMoney } from '@/shared/format'
import { groupByRoom } from './requestLines'
import styles from './RequestLines.module.scss'

/**
 * The work of a request made from an estimate, room by room: each line's amount and unit and, for the customer
 * (`showEstimate`), the estimated labour range.
 */
export default function RequestLines({ lines, showEstimate = false }) {
  const { t, i18n } = useTranslation()
  if (!lines?.length) return null
  const lng = i18n.language
  const number = (value) => new Intl.NumberFormat(intlLocale(lng), { maximumFractionDigits: 2 }).format(value)

  return (
    <div className={styles['request-lines']}>
      {groupByRoom(lines).map((room) => (
        <section key={room.name} className={styles['request-lines__room']} aria-label={room.name}>
          <h3 className={styles['request-lines__title']}>{room.name}</h3>
          <ul className={styles['request-lines__list']}>
            {room.lines.map((line) => (
              <li key={line.id} className={styles['request-lines__line']}>
                <span>{line.name}</span>
                <span className={styles['request-lines__amount']}>
                  {line.quantity != null ? `${number(line.quantity)} ${t(`estimator.units.${line.unit}`)}` : t('requests.lines.toAgree')}
                </span>
                {showEstimate && (
                  <span className={styles['request-lines__estimate']}>
                    {line.estimateMax != null ? `${formatMoney(line.estimateMin, lng)} – ${formatMoney(line.estimateMax, lng)}` : '—'}
                  </span>
                )}
              </li>
            ))}
          </ul>
        </section>
      ))}
    </div>
  )
}
