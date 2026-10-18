import { useTranslation } from 'react-i18next'
import { Link, useParams } from 'react-router'
import QueryState from '@/components/QueryState/QueryState'
import Button from '@/components/ui/Button/Button'
import Card from '@/components/ui/Card/Card'
import EmptyState from '@/components/ui/EmptyState/EmptyState'
import Icon from '@/components/ui/Icon/Icon'
import { useLocalizedPath } from '@/i18n/hooks'
import { formatDate } from '@/shared/format'
import { ProductPrice } from './OfferCard'
import PhotoViewer from './PhotoViewer'
import { useStore } from './storesApi'
import styles from './MaterialPrices.module.scss'

/**
 * A store (E2): who it is, where its branches are and the prices of what it sells under each material. The website
 * and phone are plain information; nothing is tracked about what visitors click.
 */
export default function StorePage() {
  const { t, i18n } = useTranslation()
  const path = useLocalizedPath()
  const { slug } = useParams()
  const query = useStore(slug)

  const notFound = (
    <EmptyState
      icon="search"
      title={t('storePage.notFound')}
      description={t('storePage.notFoundText')}
      action={<Button to={path('/material-prices')}>{t('materialPrices.all')}</Button>}
    />
  )

  return (
    <QueryState query={query} notFound={notFound}>
      {(store) => (
        <div className={styles['material-prices']}>
          <title>{t('storePage.documentTitle', { name: store.name })}</title>
          <meta name="description" content={store.description ?? t('storePage.description', { name: store.name })} />
          <header className={styles['store__header']}>
            {store.logoUrl ? (
              <img className={styles['store__logo']} src={store.logoUrl} alt="" width="72" height="72" />
            ) : (
              <span className={styles['store__logo']} aria-hidden="true">
                <Icon name="home" size={28} />
              </span>
            )}
            <div>
              <h1 className={styles['store__name']}>{store.name}</h1>
              {store.description && <p className={styles['material-prices__note']}>{store.description}</p>}
              <p className={styles['store__contacts']}>
                {store.website && (
                  <a href={store.website} target="_blank" rel="noopener noreferrer nofollow">
                    {t('storePage.website')}
                  </a>
                )}
                {store.phone && <a href={`tel:${store.phone.replace(/\s+/g, '')}`}>{store.phone}</a>}
              </p>
            </div>
          </header>
          <Card title={t('storePage.branches')}>
            {store.branches.length === 0 ? (
              <p className={styles['material-prices__muted']}>{t('storePage.noBranches')}</p>
            ) : (
              <ul className={styles['store__branches']}>
                {store.branches.map((branch) => (
                  <li key={`${branch.name}-${branch.address}`}>
                    <strong>{branch.name}</strong>
                    <span className={styles['material-prices__muted']}>
                      {[branch.city, branch.district, branch.address].filter(Boolean).join(', ')}
                    </span>
                    {branch.phone && <span className={styles['material-prices__muted']}>{branch.phone}</span>}
                    {branch.openingHours && <span className={styles['material-prices__muted']}>{branch.openingHours}</span>}
                  </li>
                ))}
              </ul>
            )}
          </Card>
          <h2 className={styles['material-prices__section']}>{t('storePage.products')}</h2>
          {store.materials.length === 0 ? (
            <EmptyState icon="search" title={t('storePage.noProducts')} />
          ) : (
            store.materials.map((material) => (
              <Card key={material.slug} title={<Link to={path(`/material-prices/${material.slug}`)}>{material.name}</Link>}>
                <ul className={styles['material-prices__list']}>
                  {material.products.map((product) => (
                    <li key={product.id} className={styles['offer']}>
                      <PhotoViewer product={product} />
                      <div className={styles['offer__body']}>
                        <h3 className={styles['offer__name']}>{product.name}</h3>
                        <p className={styles['material-prices__muted']}>
                          {[t(`materialPrices.grades.${product.grade}`), product.brand, product.packLabel].filter(Boolean).join(' · ')}
                        </p>
                        <p className={styles['material-prices__muted']}>{t('materialPrices.offer.checked', { date: formatDate(product.priceUpdatedAt, i18n.language) })}</p>
                      </div>
                      <ProductPrice product={product} unit={material.unit} />
                    </li>
                  ))}
                </ul>
              </Card>
            ))
          )}
        </div>
      )}
    </QueryState>
  )
}
