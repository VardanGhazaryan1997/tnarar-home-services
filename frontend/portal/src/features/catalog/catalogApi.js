import { useTranslation } from 'react-i18next'
import { baseApi } from '@/api/baseApi'

/** Service categories (a tree) and cities with districts, named in the UI language. */
export const catalogApi = baseApi.injectEndpoints({
  endpoints: (build) => ({
    // The language is part of the cache key, so switching language loads translated names.
    getCategories: build.query({ query: () => '/categories', providesTags: ['Catalog'], keepUnusedDataFor: 60 * 30 }),
    getCities: build.query({ query: () => '/cities', providesTags: ['Catalog'], keepUnusedDataFor: 60 * 30 }),
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

/** Categories as select options: parents, then their children indented under them. */
export function categoryOptions(categories = []) {
  return categories.flatMap((parent) => [
    { value: parent.id, label: parent.name },
    ...parent.children.map((child) => ({ value: child.id, label: `— ${child.name}` })),
  ])
}
