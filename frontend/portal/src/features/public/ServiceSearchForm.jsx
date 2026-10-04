import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { useNavigate } from 'react-router'
import Button from '@/components/ui/Button/Button'
import { SelectField } from '@/components/ui/Field/Field'
import Icon from '@/components/ui/Icon/Icon'
import { useCategories, useCities } from '@/features/catalog/catalogApi'
import { useLocalizedPath } from '@/i18n/hooks'
import { searchPath } from './searchLinks'
import styles from './ServiceSearchForm.module.scss'

/** "What do you need, and where?" — opens the search page for that service and city. */
export default function ServiceSearchForm() {
  const { t } = useTranslation()
  const navigate = useNavigate()
  const path = useLocalizedPath()
  const categories = useCategories()
  const cities = useCities()
  const [category, setCategory] = useState('')
  const [city, setCity] = useState('')

  const options = (categories.data ?? []).flatMap((parent) => [
    { value: parent.slug, label: parent.name },
    ...parent.children.map((child) => ({ value: child.slug, label: `— ${child.name}` })),
  ])

  const submit = (event) => {
    event.preventDefault()
    navigate(searchPath(path, { category, city }))
  }

  return (
    <form className={styles['service-search']} onSubmit={submit} role="search" aria-label={t('public.search.label')}>
      <SelectField
        label={t('public.search.what')}
        placeholder={t('public.search.anyService')}
        options={options}
        value={category}
        onChange={(event) => setCategory(event.target.value)}
        className={styles['service-search__field']}
      />
      <SelectField
        label={t('public.search.where')}
        placeholder={t('public.search.anyCity')}
        options={(cities.data ?? []).map((item) => ({ value: item.slug, label: item.name }))}
        value={city}
        onChange={(event) => setCity(event.target.value)}
        className={styles['service-search__field']}
      />
      <Button type="submit" variant="accent" size="lg" icon={<Icon name="search" />} className={styles['service-search__submit']}>
        {t('public.search.submit')}
      </Button>
    </form>
  )
}
