import { useTranslation } from 'react-i18next'
import { groupByRoom } from '@/features/requests/components/requestLines'
import { formatMoney } from '@/shared/format'
import { compareOffers } from './offerComparison'
import styles from './offers.module.scss'

/**
 * Work offers priced line by line, side by side with the estimate: one row per line of the request, the cheapest
 * price of each row marked. Shown when at least one offer answers the request's lines.
 */
export default function OfferComparison({ request, offers }) {
  const { t, i18n } = useTranslation()
  const lng = i18n.language
  const table = compareOffers(request.lines, offers)
  if (!table) return null
  const money = (amount) => formatMoney(amount, lng)
  const showEstimate = request.lines.some((line) => line.estimateMax != null)

  const cell = (entry, best) => {
    if (!entry) return <span className={styles['offer-compare__muted']}>—</span>
    if (!entry.included) return <span className={styles['offer-compare__muted']}>{t('offers.compare.notIncluded')}</span>
    if (entry.amount == null) return <span className={styles['offer-compare__muted']}>{t('offers.compare.inPrice')}</span>
    return <span className={entry.amount === best ? styles['offer-compare__best'] : undefined}>{money(entry.amount)}</span>
  }

  return (
    <section className={styles['offer-compare']} aria-label={t('offers.compare.title')}>
      <table className={styles['offer-compare__table']}>
        <thead>
          <tr>
            <th scope="col">{t('offers.compare.work')}</th>
            {showEstimate && <th scope="col">{t('offers.compare.estimate')}</th>}
            {table.offers.map((offer) => (
              <th key={offer.id} scope="col">
                {offer.partner.displayName}
              </th>
            ))}
          </tr>
        </thead>
        <tbody>
          {groupByRoom(table.rows.map((row) => ({ ...row.line, row }))).map((room) => (
            <RoomRows key={room.name} room={room} columns={table.offers.length + (showEstimate ? 2 : 1)}>
              {room.lines.map(({ row }) => (
                <tr key={row.line.id}>
                  <td>{row.line.name}</td>
                  {showEstimate && (
                    <td className={styles['offer-compare__muted']}>
                      {row.line.estimateMax != null ? `${money(row.line.estimateMin)} – ${money(row.line.estimateMax)}` : '—'}
                    </td>
                  )}
                  {row.entries.map((entry, index) => (
                    <td key={table.offers[index].id}>{cell(entry, row.best)}</td>
                  ))}
                </tr>
              ))}
            </RoomRows>
          ))}
          {table.extras.some((amount) => amount > 0) && (
            <tr>
              <td>{t('offers.compare.other')}</td>
              {showEstimate && <td />}
              {table.extras.map((amount, index) => (
                <td key={table.offers[index].id}>{amount > 0 ? money(amount) : '—'}</td>
              ))}
            </tr>
          )}
          <tr className={styles['offer-compare__total']}>
            <td>{t('offers.compare.total')}</td>
            {showEstimate && <td>{table.estimate ? `${money(table.estimate.min)} – ${money(table.estimate.max)}` : '—'}</td>}
            {table.offers.map((offer) => (
              <td key={offer.id}>{money(offer.price)}</td>
            ))}
          </tr>
        </tbody>
      </table>
    </section>
  )
}

function RoomRows({ room, columns, children }) {
  return (
    <>
      <tr className={styles['offer-compare__room']}>
        <th scope="rowgroup" colSpan={columns}>
          {room.name}
        </th>
      </tr>
      {children}
    </>
  )
}
