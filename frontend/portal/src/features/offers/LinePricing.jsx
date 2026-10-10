import { useTranslation } from 'react-i18next'
import { CheckboxField, TextField } from '@/components/ui/Field/Field'
import { groupByRoom } from '@/features/requests/components/requestLines'
import { formatMoney } from '@/shared/format'
import { rowAmount } from './linePricing'
import styles from './offers.module.scss'

/**
 * The request's work priced line by line: for each line whether it's included, the amount (prefilled from the
 * request) and the price per unit (prefilled from the partner's price list, else the market's usual price).
 */
export default function LinePricing({ rows, onChange, showErrors }) {
  const { t, i18n } = useTranslation()
  const lng = i18n.language
  const edit = (requestLineId, patch) => onChange(rows.map((row) => (row.requestLineId === requestLineId ? { ...row, ...patch } : row)))
  const rooms = groupByRoom(rows.map((row) => ({ ...row, roomName: row.room })))

  return (
    <div className={styles['line-pricing']}>
      {rooms.map((room) => (
        <section key={room.name} className={styles['line-pricing__room']} aria-label={room.name}>
          <h3 className={styles['line-pricing__title']}>{room.name}</h3>
          <ul className={styles['line-pricing__rows']}>
            {room.lines.map((row) => {
              const unit = t(`estimator.units.${row.unit}`)
              const amount = rowAmount(row)
              const missing = showErrors && row.included && amount === null
              return (
                <li key={row.requestLineId} className={styles['line-pricing__row']}>
                  <CheckboxField
                    className={styles['line-pricing__name']}
                    label={row.name}
                    checked={row.included}
                    onChange={(event) => edit(row.requestLineId, { included: event.target.checked })}
                  />
                  {row.included ? (
                    <>
                      <TextField
                        label={t('offers.linePricing.quantity', { unit })}
                        inputMode="decimal"
                        autoComplete="off"
                        value={row.quantity}
                        error={missing && !(Number(row.quantity) > 0) ? t('offers.linePricing.quantityRequired') : undefined}
                        onChange={(event) => edit(row.requestLineId, { quantity: event.target.value })}
                      />
                      <TextField
                        label={t('offers.linePricing.unitPrice', { unit })}
                        hint={row.source ? t(`offers.linePricing.source.${row.source}`) : undefined}
                        inputMode="numeric"
                        autoComplete="off"
                        value={row.unitPrice}
                        error={missing && row.unitPrice === '' ? t('offers.linePricing.priceRequired') : undefined}
                        onChange={(event) => edit(row.requestLineId, { unitPrice: event.target.value, source: null })}
                      />
                      <span className={styles['line-pricing__amount']}>{amount !== null ? formatMoney(amount, lng) : '—'}</span>
                    </>
                  ) : (
                    <span className={styles['line-pricing__excluded']}>{t('offers.linePricing.notIncluded')}</span>
                  )}
                </li>
              )
            })}
          </ul>
        </section>
      ))}
    </div>
  )
}
