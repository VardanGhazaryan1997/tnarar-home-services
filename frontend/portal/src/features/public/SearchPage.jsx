import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Link, useParams, useSearchParams } from 'react-router'
import PageHeader from '@/components/PageHeader/PageHeader'
import QueryState from '@/components/QueryState/QueryState'
import Button from '@/components/ui/Button/Button'
import EmptyState from '@/components/ui/EmptyState/EmptyState'
import { SelectField, TextField } from '@/components/ui/Field/Field'
import Icon from '@/components/ui/Icon/Icon'
import Pagination from '@/components/ui/Pagination/Pagination'
import Spinner from '@/components/ui/Spinner/Spinner'
import { useCategories, useCities } from '@/features/catalog/catalogApi'
import { useLocalizedPath } from '@/i18n/hooks'
import { bem } from '@/shared/bem'
import { findCategory } from './categories'
import PartnerCard from './PartnerCard'
import { SEARCH_PAGE_SIZE, useSearchPartners } from './publicApi'
import styles from './SearchPage.module.scss'

const b = bem(styles)
const TYPES = ['', 'Specialist', 'Company']
const FILTERS = ['city', 'district', 'type', 'q']

/**
 * Specialists and companies: every one (/search) or one service (/services/:category), filtered by city,
 * district, type and name. Filters live in the address so results can be shared and the back button works.
 */
export default function SearchPage() {
  const { t } = useTranslation()
  const path = useLocalizedPath()
  const { category: categorySlug } = useParams()
  const [params, setParams] = useSearchParams()
  const categories = useCategories()
  const cities = useCities()
  const [filtersOpen, setFiltersOpen] = useState(false)
  const city = params.get('city') ?? ''
  const district = params.get('district') ?? ''
  const type = params.get('type') ?? ''
  const q = params.get('q') ?? ''
  const page = Number(params.get('page')) || 1
  const [text, setText] = useState(q)

  const { category, parent } = findCategory(categories.data, categorySlug)
  const unknownCategory = Boolean(categorySlug && categories.data && !category)
  const results = useSearchPartners(
    { category: categorySlug, city, district: city ? district : '', type, search: q, page, pageSize: SEARCH_PAGE_SIZE },
    { skip: unknownCategory },
  )
  const districts = cities.data?.find((item) => item.slug === city)?.districts ?? []
  const activeFilters = FILTERS.filter((name) => params.get(name)).length

  const update = (changes) => {
    const next = new URLSearchParams(params)
    Object.entries(changes).forEach(([key, value]) => (value ? next.set(key, value) : next.delete(key)))
    if (!('page' in changes)) next.delete('page')
    setParams(next)
  }

  const keepFilters = () => {
    const kept = new URLSearchParams(params)
    kept.delete('page')
    const query = kept.toString()
    return query ? `?${query}` : ''
  }

  const title = category?.name ?? t('public.search.title')
  const chips = category ? (parent ? [parent, ...parent.children] : category.children.length ? [category, ...category.children] : []) : []

  if (unknownCategory) {
    return (
      <EmptyState
        icon="search"
        title={t('public.search.unknownCategory')}
        action={<Button to={path('/services')}>{t('public.services.title')}</Button>}
      />
    )
  }

  return (
    <div className={styles['search-page']}>
      <title>{t('public.search.documentTitle', { title })}</title>
      <PageHeader
        title={title}
        subtitle={results.data ? t('public.search.found', { n: results.data.totalCount }) : undefined}
        back={category ? { to: path('/services'), label: t('public.services.title') } : undefined}
      />

      {chips.length > 0 && (
        <nav className={styles['search-page__chips']} aria-label={t('public.search.subcategories')}>
          {chips.map((item, index) => (
            <Link
              key={item.id}
              to={`${path(`/services/${item.slug}`)}${keepFilters()}`}
              aria-current={item.slug === categorySlug ? 'page' : undefined}
              className={b('search-page__chip', { active: item.slug === categorySlug })}
            >
              {index === 0 ? t('public.search.allIn', { name: item.name }) : item.name}
            </Link>
          ))}
        </nav>
      )}

      <div className={styles['search-page__layout']}>
        <aside className={styles['search-page__aside']}>
          <form
            className={styles['search-page__search']}
            role="search"
            onSubmit={(event) => {
              event.preventDefault()
              update({ q: text.trim() })
            }}
          >
            <TextField
              label={t('public.search.name')}
              type="search"
              maxLength={100}
              placeholder={t('public.search.namePlaceholder')}
              value={text}
              onChange={(event) => setText(event.target.value)}
              className={styles['search-page__search-field']}
            />
            <Button type="submit" variant="secondary" icon={<Icon name="search" />} aria-label={t('public.search.submit')} />
          </form>
          <Button
            variant="secondary"
            block
            icon={<Icon name="filter" />}
            aria-expanded={filtersOpen}
            aria-controls="search-filters"
            onClick={() => setFiltersOpen(!filtersOpen)}
            className={styles['search-page__toggle']}
          >
            {activeFilters ? t('public.search.filtersCount', { n: activeFilters }) : t('public.search.filters')}
          </Button>
          <div id="search-filters" className={b('search-page__filters', { open: filtersOpen })}>
            <SelectField
              label={t('public.search.city')}
              placeholder={t('public.search.anyCity')}
              options={(cities.data ?? []).map((item) => ({ value: item.slug, label: item.name }))}
              value={city}
              onChange={(event) => update({ city: event.target.value, district: '' })}
            />
            <SelectField
              label={t('public.search.district')}
              placeholder={t('public.search.anyDistrict')}
              options={districts.map((item) => ({ value: item.slug, label: item.name }))}
              value={district}
              disabled={!districts.length}
              onChange={(event) => update({ district: event.target.value })}
            />
            <SelectField
              label={t('public.search.type')}
              options={TYPES.map((value) => ({ value, label: value ? t(`public.partnerTypePlural.${value}`) : t('public.search.anyType') }))}
              value={type}
              onChange={(event) => update({ type: event.target.value })}
            />
            {activeFilters > 0 && (
              <Button
                variant="ghost"
                size="sm"
                icon={<Icon name="close" />}
                onClick={() => {
                  setText('')
                  update({ city: '', district: '', type: '', q: '' })
                }}
              >
                {t('public.search.clear')}
              </Button>
            )}
          </div>
        </aside>

        <section className={styles['search-page__results']} aria-label={t('public.search.results')} aria-busy={results.isFetching}>
          <QueryState query={results}>
            {(data) =>
              data.items.length === 0 ? (
                <EmptyState
                  icon="search"
                  title={t('public.search.emptyTitle')}
                  description={t('public.search.emptyText')}
                  action={
                    <Button to={path(category ? `/requests/new?category=${category.id}` : '/requests/new')} variant="accent" icon={<Icon name="plus" />}>
                      {t('home.cta')}
                    </Button>
                  }
                />
              ) : (
                <>
                  {results.isFetching && (
                    <span className={styles['search-page__refreshing']}>
                      <Spinner size="sm" label={t('common.loading')} />
                    </span>
                  )}
                  <div className={styles['search-page__grid']}>
                    {data.items.map((partner) => (
                      <PartnerCard key={partner.slug} partner={partner} />
                    ))}
                  </div>
                  <Pagination page={data.page} pageSize={data.pageSize} totalCount={data.totalCount} onChange={(next) => update({ page: String(next) })} />
                </>
              )
            }
          </QueryState>
        </section>
      </div>
    </div>
  )
}
