import { useMemo, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Link, useSearchParams } from 'react-router'
import PageHeader from '@/components/PageHeader/PageHeader'
import QueryState from '@/components/QueryState/QueryState'
import Button from '@/components/ui/Button/Button'
import Card from '@/components/ui/Card/Card'
import EmptyState from '@/components/ui/EmptyState/EmptyState'
import { SelectField, TextField } from '@/components/ui/Field/Field'
import Tag from '@/components/ui/Tag/Tag'
import { useRegions } from '@/features/catalog/catalogApi'
import { useLocalizedPath } from '@/i18n/hooks'
import { formatUnitPrice } from '@/shared/format'
import { GRADES, filterMaterials } from './materialPrices'
import { useMaterialPrices } from './storesApi'
import styles from './MaterialPrices.module.scss'

/** The three usual prices of a material, per unit: budget, standard and premium, each marked when the stores set it. */
export function GradePrices({ prices, unit, lng }) {
  const { t } = useTranslation()
  return (
    <dl className={styles['material-prices__grades']}>
      {GRADES.map((grade) => (
        <div key={grade} className={styles['material-prices__grade']}>
          <dt>{t(`materialPrices.grades.${grade}`)}</dt>
          <dd>
            <strong>{formatUnitPrice(prices[grade.toLowerCase()].price, lng)}</strong>
            <span className={styles['material-prices__muted']}>
              {t('materialPrices.perUnit', { unit: t(`estimator.materials.units.${unit}`) })}
              {prices[grade.toLowerCase()].source === 'Stores' ? ` · ${t('materialPrices.fromStoresShort')}` : ''}
            </span>
          </dd>
        </div>
      ))}
    </dl>
  )
}

/**
 * "Building material prices" (E2): every material with its usual price per unit for budget, standard and premium
 * products, how many stores sell it and the lowest price now. The region is kept in the address (?region=) and carried
 * to the comparison. Public, for search engines too.
 */
export default function MaterialPricesPage() {
  const { t, i18n } = useTranslation()
  const lng = i18n.language
  const path = useLocalizedPath()
  const regions = useRegions()
  const [params, setParams] = useSearchParams()
  const region = params.get('region') ?? ''
  const [search, setSearch] = useState('')
  const query = useMaterialPrices({ region })
  const materials = useMemo(() => filterMaterials(query.data, search), [query.data, search])

  const changeRegion = (value) => {
    const next = new URLSearchParams(params)
    if (value) next.set('region', value)
    else next.delete('region')
    setParams(next, { replace: true })
  }

  return (
    <div className={styles['material-prices']}>
      <title>{t('materialPrices.documentTitle')}</title>
      <meta name="description" content={t('materialPrices.description')} />
      <PageHeader
        title={t('materialPrices.title')}
        subtitle={t('materialPrices.subtitle')}
        actions={
          <Button to={path('/estimates/new')} variant="accent">
            {t('materialPrices.estimate')}
          </Button>
        }
      />
      <p className={styles['material-prices__note']}>{t('materialPrices.note')}</p>
      <div className={styles['material-prices__filters']}>
        <TextField label={t('materialPrices.search')} type="search" autoComplete="off" value={search} onChange={(event) => setSearch(event.target.value)} />
        <SelectField
          label={t('materialPrices.region')}
          placeholder={t('materialPrices.allRegions')}
          value={region}
          onChange={(event) => changeRegion(event.target.value)}
          options={(regions.data ?? []).map((item) => ({ value: item.slug, label: item.name }))}
        />
      </div>
      <QueryState query={query}>
        {() =>
          materials.length === 0 ? (
            <EmptyState icon="search" title={t('materialPrices.none')} />
          ) : (
            <ul className={styles['material-prices__list']}>
              {materials.map((material) => (
                <li key={material.slug}>
                  <Card as="article" aria-label={material.name}>
                    <div className={styles['material-prices__head']}>
                      <h2 className={styles['material-prices__name']}>
                        <Link to={path(`/material-prices/${material.slug}${region ? `?region=${region}` : ''}`)}>{material.name}</Link>
                      </h2>
                      <Tag>{t(`estimator.materials.units.${material.unit}`)}</Tag>
                    </div>
                    <GradePrices prices={material.prices} unit={material.unit} lng={lng} />
                    <p className={styles['material-prices__muted']}>
                      {material.storeCount > 0
                        ? `${t('materialPrices.fromStores', { n: material.storeCount })}${
                            material.lowestUnitPrice == null
                              ? ''
                              : ` · ${t('materialPrices.lowest', {
                                  amount: formatUnitPrice(material.lowestUnitPrice, lng),
                                  unit: t(`estimator.materials.units.${material.unit}`),
                                })}`
                          }`
                        : t('materialPrices.noStores')}
                    </p>
                  </Card>
                </li>
              ))}
            </ul>
          )
        }
      </QueryState>
    </div>
  )
}

