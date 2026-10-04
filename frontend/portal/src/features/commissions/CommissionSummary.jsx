import { useTranslation } from 'react-i18next'
import Alert from '@/components/ui/Alert/Alert'
import { bem } from '@/shared/bem'
import { formatDate, formatMoney } from '@/shared/format'
import styles from './commissions.module.scss'

const b = bem(styles)

/** The partner's totals, and why new requests stopped when they are paused for an unpaid statement. */
export default function CommissionSummary({ summary }) {
  const { t, i18n } = useTranslation()
  const lng = i18n.language
  const tiles = [
    { key: 'outstanding', value: formatMoney(summary.outstanding, lng) },
    { key: 'overdue', value: formatMoney(summary.overdue, lng), danger: summary.overdue > 0 },
    { key: 'nextDue', value: summary.nextDueOn ? formatDate(summary.nextDueOn, lng) : '—' },
    { key: 'unbilled', value: formatMoney(summary.unbilled, lng) },
  ]

  return (
    <>
      {summary.pausedSince && (
        <Alert tone="danger" title={t('commissions.paused.title')}>
          {t('commissions.paused.text', { date: formatDate(summary.pausedSince, lng), days: summary.pauseAfterOverdueDays })}
        </Alert>
      )}
      <dl className={styles['commission-summary']}>
        {tiles.map((tile) => (
          <div key={tile.key} className={b('commission-summary__tile', { danger: tile.danger })}>
            <dt className={styles['commission-summary__label']}>{t(`commissions.summary.${tile.key}`)}</dt>
            <dd className={styles['commission-summary__value']}>{tile.value}</dd>
            <dd className={styles['commission-summary__hint']}>{t(`commissions.summary.${tile.key}Hint`)}</dd>
          </div>
        ))}
      </dl>
    </>
  )
}
