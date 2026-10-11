import { useTranslation } from 'react-i18next'
import { Link, useParams, useSearchParams } from 'react-router'
import PageHeader from '@/components/PageHeader/PageHeader'
import QueryState from '@/components/QueryState/QueryState'
import Button from '@/components/ui/Button/Button'
import Card from '@/components/ui/Card/Card'
import EmptyState from '@/components/ui/EmptyState/EmptyState'
import { CheckboxField, SelectField } from '@/components/ui/Field/Field'
import Segmented from '@/components/ui/Segmented/Segmented'
import { useRegions } from '@/features/catalog/catalogApi'
import { useLocalizedPath } from '@/i18n/hooks'
import { GradePrices } from './MaterialPricesPage'
import { GRADES, cheapestInStock } from './materialPrices'
import OfferCard from './OfferCard'
import { useMaterialOffers } from './storesApi'
import styles from './MaterialPrices.module.scss'

/**
 * One material (E2): its usual price per unit for each grade, then every store's offer for it, in stock first and
 * cheapest per unit first. Grade, region and "in stock only" are kept in the address (?grade=&region=&inStock=1).
 */
export default function MaterialPage() {
  const { t, i18n } = useTranslation()
  const lng = i18n.language
  const path = useLocalizedPath()
  const { slug } = useParams()
  const regions = useRegions()
  const [params, setParams] = useSearchParams()
  const grade = GRADES.includes(params.get('grade')) ? params.get('grade') : ''
  const region = params.get('region') ?? ''
  const inStock = params.get('inStock') === '1'
  const query = useMaterialOffers({ slug, region, grade, inStock })

  const change = (key, value) => {
    const next = new URLSearchParams(params)
    if (value) next.set(key, value)
    else next.delete(key)
    setParams(next, { replace: true })
  }

  const notFound = (
    <EmptyState
      icon="search"
      title={t('materialPrices.notFound')}
      description={t('materialPrices.notFoundText')}
      action={<Button to={path('/material-prices')}>{t('materialPrices.all')}</Button>}
    />
  )

  return (
    <QueryState query={query} notFound={notFound}>
      {(material) => {
        const unit = t(`estimator.materials.units.${material.unit}`)
        const lowest = cheapestInStock(material.offers)
        return (
          <div className={styles['material-prices']}>
            <title>{t('materialPrices.detail.documentTitle', { name: material.name })}</title>
            <meta name="description" content={t('materialPrices.detail.description', { name: material.name, unit })} />
            <PageHeader
              title={material.name}
              subtitle={t('materialPrices.detail.subtitle', { unit })}
              actions={
                <Button to={path(`/material-prices${region ? `?region=${region}` : ''}`)} variant="secondary">
                  {t('materialPrices.all')}
                </Button>
              }
            />
            <Card title={t('materialPrices.detail.usual', { unit })}>
              <GradePrices prices={material.prices} unit={material.unit} lng={lng} />
              <p className={styles['material-prices__note']}>{t('materialPrices.detail.usualNote')}</p>
            </Card>
            <div className={styles['material-prices__filters']}>
              <Segmented
                label={t('materialPrices.detail.grade')}
                value={grade}
                onChange={(value) => change('grade', value)}
                options={[{ value: '', label: t('materialPrices.detail.allGrades') }, ...GRADES.map((value) => ({ value, label: t(`materialPrices.grades.${value}`) }))]}
              />
              <SelectField
                label={t('materialPrices.region')}
                placeholder={t('materialPrices.allRegions')}
                value={region}
                onChange={(event) => change('region', event.target.value)}
                options={(regions.data ?? []).map((item) => ({ value: item.slug, label: item.name }))}
              />
              <CheckboxField label={t('materialPrices.detail.inStockOnly')} checked={inStock} onChange={(event) => change('inStock', event.target.checked ? '1' : '')} />
            </div>
            <h2 className={styles['material-prices__section']}>{t('materialPrices.detail.offers', { n: material.offers.length })}</h2>
            {material.offers.length === 0 ? (
              <EmptyState icon="search" title={t('materialPrices.detail.noOffers')} description={t('materialPrices.detail.noOffersText')} />
            ) : (
              <ul className={styles['material-prices__list']}>
                {material.offers.map((offer) => (
                  <li key={offer.product.id}>
                    <OfferCard offer={offer} unit={material.unit} lowest={offer.product.id === lowest} />
                  </li>
                ))}
              </ul>
            )}
            <p className={styles['material-prices__note']}>{t('materialPrices.detail.disclaimer')}</p>
            <Link to={path('/estimates/new')} className={styles['material-prices__link']}>
              {t('materialPrices.estimate')}
            </Link>
          </div>
        )
      }}
    </QueryState>
  )
}
