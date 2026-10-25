import { useTranslation } from 'react-i18next'
import { Link } from 'react-router'
import Tag from '@/components/ui/Tag/Tag'
import { useLocalizedPath } from '@/i18n/hooks'
import { formatDate, formatMoney, formatUnitPrice } from '@/shared/format'
import { discountPercent } from './materialPrices'
import PhotoViewer from './PhotoViewer'
import styles from './MaterialPrices.module.scss'

/** One product on sale: photo, material (linking to its comparison), product, store, usual and sale price, the last day and the discount. */
export default function SaleCard({ sale, region }) {
  const { t, i18n } = useTranslation()
  const lng = i18n.language
  const path = useLocalizedPath()
  const { product, store } = sale

  return (
    <article className={styles['sale']} aria-label={`${product.name} — ${store.name}`}>
      <PhotoViewer product={product} />
      <div className={styles['sale__body']}>
        <h3 className={styles['sale__material']}>
          <Link to={path(`/material-prices/${sale.materialSlug}${region ? `?region=${region}` : ''}`)}>{sale.materialName}</Link>
        </h3>
        <p className={styles['sale__product']}>{product.name}</p>
        <p className={styles['material-prices__muted']}>
          <Link to={path(`/stores/${store.slug}`)}>{store.name}</Link>
        </p>
        <p className={styles['offer__price']}>
          <strong>{formatMoney(product.currentPrice, lng)}</strong>
          <s className={styles['offer__was']}>{formatMoney(product.price, lng)}</s>
          <Tag tone="success">{t('materialPrices.sales.discount', { percent: discountPercent(product) })}</Tag>
        </p>
        <p className={styles['material-prices__muted']}>
          {t('materialPrices.sales.until', { date: formatDate(product.saleEndsOn, lng) })}
          {' · '}
          {t('materialPrices.offer.perUnit', { amount: formatUnitPrice(product.unitPrice, lng), unit: t(`estimator.materials.units.${sale.unit}`) })}
        </p>
      </div>
    </article>
  )
}
