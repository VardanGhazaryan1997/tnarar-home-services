import { useTranslation } from 'react-i18next'
import { Link } from 'react-router'
import Icon from '@/components/ui/Icon/Icon'
import Tag from '@/components/ui/Tag/Tag'
import { useLocalizedPath } from '@/i18n/hooks'
import { formatDate, formatMoney, formatUnitPrice } from '@/shared/format'
import { branchPlaces } from './materialPrices'
import styles from './MaterialPrices.module.scss'

/**
 * A product's price: what a pack costs now (the sale price struck-through usual price and the day the sale ends), the
 * price per unit of the material, and whether it is in stock.
 */
export function ProductPrice({ product, unit, lowest = false }) {
  const { t, i18n } = useTranslation()
  const lng = i18n.language
  return (
    <div className={styles['offer__prices']}>
      <p className={styles['offer__price']}>
        <strong>{formatMoney(product.currentPrice, lng)}</strong>
        {product.salePrice != null && <s className={styles['offer__was']}>{formatMoney(product.price, lng)}</s>}
      </p>
      {product.salePrice != null && (
        <p className={styles['material-prices__muted']}>{t('materialPrices.offer.saleUntil', { date: formatDate(product.saleEndsOn, lng) })}</p>
      )}
      {product.priceBefore != null && (
        <p className={`${styles['offer__trend']} ${styles[product.price > product.priceBefore ? 'offer__trend--up' : 'offer__trend--down']}`}>
          <span aria-hidden="true">{product.price > product.priceBefore ? '↑' : '↓'} </span>
          {t('materialPrices.offer.wasBefore', { price: formatMoney(product.priceBefore, lng) })}
        </p>
      )}
      <p className={styles['offer__unit']}>
        {t('materialPrices.offer.perUnit', { amount: formatUnitPrice(product.unitPrice, lng), unit: t(`estimator.materials.units.${unit}`) })}
      </p>
      <p className={styles['offer__tags']}>
        {lowest && <Tag tone="success">{t('materialPrices.offer.lowest')}</Tag>}
        <Tag tone={product.inStock ? 'info' : 'warning'}>{t(product.inStock ? 'materialPrices.offer.inStock' : 'materialPrices.offer.outOfStock')}</Tag>
      </p>
    </div>
  )
}

/** One store's offer for a material: photo, product, brand, store and its branches, pack and price. */
export default function OfferCard({ offer, unit, lowest }) {
  const { t, i18n } = useTranslation()
  const path = useLocalizedPath()
  const { product, store } = offer
  const places = branchPlaces(store.branches)

  return (
    <article className={styles['offer']} aria-label={`${product.name} — ${store.name}`}>
      {product.photoUrl ? (
        <img className={styles['offer__photo']} src={product.photoUrl} alt="" loading="lazy" width="72" height="72" />
      ) : (
        <span className={styles['offer__photo']} aria-hidden="true">
          <Icon name="image" size={28} />
        </span>
      )}
      <div className={styles['offer__body']}>
        <h3 className={styles['offer__name']}>{product.name}</h3>
        <p className={styles['material-prices__muted']}>
          {[product.brand, product.packLabel ?? null].filter(Boolean).join(' · ')}
        </p>
        <p className={styles['offer__store']}>
          <Link to={path(`/stores/${store.slug}`)}>{store.name}</Link>
          {places && <span className={styles['material-prices__muted']}> · {t('materialPrices.offer.branches', { places })}</span>}
        </p>
        <p className={styles['material-prices__muted']}>{t('materialPrices.offer.checked', { date: formatDate(product.priceUpdatedAt, i18n.language) })}</p>
      </div>
      <ProductPrice product={product} unit={unit} lowest={lowest} />
    </article>
  )
}
