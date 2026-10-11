import { useMemo, useState } from 'react'
import { useTranslation } from 'react-i18next'
import PageHeader from '@/components/PageHeader/PageHeader'
import QueryState from '@/components/QueryState/QueryState'
import Button from '@/components/ui/Button/Button'
import Card from '@/components/ui/Card/Card'
import EmptyState from '@/components/ui/EmptyState/EmptyState'
import { SelectField, TextField } from '@/components/ui/Field/Field'
import Icon from '@/components/ui/Icon/Icon'
import { useCategories, useRegions } from '@/features/catalog/catalogApi'
import { useWorkItems } from '@/features/estimator/estimatorApi'
import { useLocalizedPath } from '@/i18n/hooks'
import { formatMoney } from '@/shared/format'
import { priceGuide } from './priceGuide'
import styles from './PriceGuidePage.module.scss'

/**
 * "Renovation prices in Armenia" (A6): the usual labour price of every kind of work, by service, from what
 * specialists on Tnarar charge (or our own estimate until enough of them priced it). Choosing a region shows its own
 * prices where enough specialists there priced the work (C2). Public, for search engines too.
 */
export default function PriceGuidePage() {
  const { t, i18n } = useTranslation()
  const lng = i18n.language
  const path = useLocalizedPath()
  const categories = useCategories()
  const regions = useRegions()
  const [search, setSearch] = useState('')
  const [category, setCategory] = useState('')
  const [region, setRegion] = useState('')
  const workItems = useWorkItems({ region })
  const guide = useMemo(
    () => (categories.data && workItems.data ? priceGuide(categories.data, workItems.data, { search, category }) : []),
    [categories.data, workItems.data, search, category],
  )
  const money = (amount) => formatMoney(amount, lng)

  return (
    <div className={styles['price-guide']}>
      <title>{t('public.priceGuide.documentTitle')}</title>
      <meta name="description" content={t('public.priceGuide.description')} />
      <PageHeader
        title={t('public.priceGuide.title')}
        subtitle={t('public.priceGuide.subtitle')}
        actions={
          <Button to={path('/estimates/new')} variant="accent" icon={<Icon name="money" />}>
            {t('public.priceGuide.estimate')}
          </Button>
        }
      />
      <p className={styles['price-guide__note']}>{t('public.priceGuide.note')}</p>
      <div className={styles['price-guide__filters']}>
        <TextField label={t('public.priceGuide.search')} type="search" autoComplete="off" value={search} onChange={(event) => setSearch(event.target.value)} />
        <SelectField
          label={t('public.priceGuide.service')}
          placeholder={t('public.priceGuide.allServices')}
          value={category}
          onChange={(event) => setCategory(event.target.value)}
          options={(categories.data ?? []).map((main) => ({ value: main.id, label: main.name }))}
        />
        <SelectField
          label={t('public.priceGuide.region')}
          placeholder={t('public.priceGuide.allRegions')}
          value={region}
          onChange={(event) => setRegion(event.target.value)}
          options={(regions.data ?? []).map((item) => ({ value: item.slug, label: item.name }))}
        />
      </div>
      <QueryState query={workItems.isError ? workItems : categories}>
        {() =>
          guide.length === 0 ? (
            workItems.isLoading ? null : <EmptyState icon="search" title={t('public.priceGuide.none')} />
          ) : (
            guide.map((main) => (
              <Card key={main.id} title={main.name} className={styles['price-guide__main']}>
                {main.groups.map((group) => (
                  <section key={group.id} className={styles['price-guide__group']} aria-label={group.name}>
                    <h3 className={styles['price-guide__group-title']}>{group.name}</h3>
                    <ul className={styles['price-guide__items']}>
                      {group.items.map((item) => (
                        <li key={item.id} className={styles['price-guide__item']}>
                          <span className={styles['price-guide__name']}>
                            {item.name}
                            <span className={styles['price-guide__source']}>
                              {item.regional
                                ? t('public.priceGuide.fromLocalSpecialists', { n: item.partnerCount })
                                : item.partnerCount
                                  ? t('public.priceGuide.fromSpecialists', { n: item.partnerCount })
                                  : t('public.priceGuide.ourEstimate')}
                            </span>
                          </span>
                          <span className={styles['price-guide__price']}>
                            <strong>
                              {money(item.priceMin)} – {money(item.priceMax)}
                            </strong>
                            <span className={styles['price-guide__source']}>
                              {t('public.priceGuide.usually', { amount: money(item.priceTypical), unit: t(`estimator.units.${item.unit}`) })}
                            </span>
                          </span>
                        </li>
                      ))}
                    </ul>
                  </section>
                ))}
              </Card>
            ))
          )
        }
      </QueryState>
      <Card className={styles['price-guide__cta']}>
        <p className={styles['price-guide__cta-title']}>{t('public.priceGuide.ctaTitle')}</p>
        <p className={styles['price-guide__note']}>{t('public.priceGuide.ctaText')}</p>
        <div className={styles['price-guide__cta-actions']}>
          <Button to={path('/estimates/new')} variant="accent">
            {t('public.priceGuide.estimate')}
          </Button>
          <Button to={path('/requests/new')} variant="secondary">
            {t('public.priceGuide.request')}
          </Button>
        </div>
      </Card>
    </div>
  )
}
