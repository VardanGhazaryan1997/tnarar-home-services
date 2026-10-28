import { Link } from 'react-router'
import { useTranslation } from 'react-i18next'
import Card from '@/components/ui/Card/Card'
import { useLocalizedPath } from '@/i18n/hooks'
import { intlLocale } from '@/i18n/languages'
import { formatMoney } from '@/shared/format'
import ShoppingList from './ShoppingList'
import { topStores } from './storeOffers'
import useWhereToBuyFor from './useWhereToBuyFor'
import styles from './estimator.module.scss'

/** "Cheapest: 2 × 25 kg bag at Store X — 18,000 ֏", linking to the material's price comparison. */
export function CheapestLine({ offer, unit }) {
  const { t, i18n } = useTranslation()
  const path = useLocalizedPath()
  const number = (value) => new Intl.NumberFormat(intlLocale(i18n.language), { maximumFractionDigits: 3 }).format(value)
  const pack = offer.packLabel || `${number(offer.packSize)} ${t(`estimator.materials.units.${unit}`)}`

  return (
    <Link to={path(`/material-prices/${offer.materialSlug}`)} className={styles['where-to-buy__cheapest']}>
      {t('whereToBuy.cheapest', { packs: offer.packs, pack, store: offer.storeName, cost: formatMoney(offer.cost, i18n.language) })}
    </Link>
  )
}

/** The best stores to buy everything at: "Store X has 9 of 11 materials, 145,000 ֏", and the shopping list (R5). */
export default function WhereToBuy({ measurement }) {
  const { t, i18n } = useTranslation()
  const path = useLocalizedPath()
  const data = useWhereToBuyFor(measurement)
  const stores = topStores(data)
  if (stores.length === 0) return null

  return (
    <Card title={t('whereToBuy.title')} aria-label={t('whereToBuy.title')}>
      <ul className={styles['quick-estimate__lines']}>
        {stores.map((store) => {
          const count = store.covered + store.missing
          return (
            <li key={store.storeSlug} className={styles['quick-estimate__line']}>
              <span>
                <Link to={path(`/stores/${store.storeSlug}`)}>{store.storeName}</Link>
                <span className={styles['quick-estimate__quantity']}>
                  {' · '}
                  {t(store.missing === 0 ? 'whereToBuy.hasAll' : 'whereToBuy.hasSome', { covered: store.covered, count })}
                </span>
              </span>
              <span className={styles['quick-estimate__line-price']}>{formatMoney(store.total, i18n.language)}</span>
            </li>
          )
        })}
      </ul>
      <p className={styles['quick-estimate__note']}>{t('whereToBuy.note')}</p>
      <div className={styles['where-to-buy__actions']}>
        <ShoppingList data={data} measurement={measurement} />
        <Link to={path('/material-prices')}>{t('whereToBuy.compare')}</Link>
      </div>
    </Card>
  )
}
