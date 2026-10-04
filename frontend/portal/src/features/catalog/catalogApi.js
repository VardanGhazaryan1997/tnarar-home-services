import { useTranslation } from 'react-i18next'
import { baseApi } from '@/api/baseApi'

/** Service categories (a tree), regions, and towns and villages with districts, named in the UI language. */
export const catalogApi = baseApi.injectEndpoints({
  endpoints: (build) => ({
    // The language is part of the cache key, so switching language loads translated names.
    getCategories: build.query({ query: () => '/categories', providesTags: ['Catalog'], keepUnusedDataFor: 60 * 30 }),
    getCities: build.query({ query: () => '/cities', providesTags: ['Catalog'], keepUnusedDataFor: 60 * 30 }),
    getRegions: build.query({ query: () => '/regions', providesTags: ['Catalog'], keepUnusedDataFor: 60 * 30 }),
  }),
})

export function useCategories() {
  const { i18n } = useTranslation()
  return catalogApi.useGetCategoriesQuery(i18n.language)
}

export function useCities() {
  const { i18n } = useTranslation()
  return catalogApi.useGetCitiesQuery(i18n.language)
}

export function useRegions() {
  const { i18n } = useTranslation()
  return catalogApi.useGetRegionsQuery(i18n.language)
}

/**
 * Towns and villages as select option groups, one per region (in the regions' order). Without regions, a flat list.
 * `value` picks what the option's value is (the id by default; the slug for search links).
 */
export function cityOptionGroups(cities = [], regions = [], value = (city) => city.id) {
  const option = (city) => ({ value: value(city), label: city.name })
  if (!regions.length) return cities.map(option)
  const known = new Set(regions.map((region) => region.id))
  const groups = regions
    .map((region) => ({ label: region.name, options: cities.filter((city) => city.regionId === region.id).map(option) }))
    .filter((group) => group.options.length)
  const other = cities.filter((city) => !known.has(city.regionId)).map(option)
  return other.length ? [...groups, { label: '—', options: other }] : groups
}

/** Categories as select options: parents, then their children indented under them. */
export function categoryOptions(categories = []) {
  return categories.flatMap((parent) => [
    { value: parent.id, label: parent.name },
    ...parent.children.map((child) => ({ value: child.id, label: `— ${child.name}` })),
  ])
}
