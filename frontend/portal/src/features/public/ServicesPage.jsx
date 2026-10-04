import { useTranslation } from 'react-i18next'
import { Link } from 'react-router'
import PageHeader from '@/components/PageHeader/PageHeader'
import QueryState from '@/components/QueryState/QueryState'
import Button from '@/components/ui/Button/Button'
import Icon from '@/components/ui/Icon/Icon'
import { categoryIcon } from '@/components/ui/Icon/icons'
import { useCategories } from '@/features/catalog/catalogApi'
import { useLocalizedPath } from '@/i18n/hooks'
import styles from './ServicesPage.module.scss'

/** Every service category with its subcategories; each opens the search page for it. */
export default function ServicesPage() {
  const { t } = useTranslation()
  const path = useLocalizedPath()
  const categories = useCategories()

  return (
    <div className={styles['services-page']}>
      <title>{t('public.services.documentTitle')}</title>
      <PageHeader
        title={t('public.services.title')}
        subtitle={t('public.services.subtitle')}
        actions={
          <Button to={path('/search')} variant="secondary" icon={<Icon name="search" />}>
            {t('public.services.everyone')}
          </Button>
        }
      />
      <QueryState query={categories}>
        {(data) => (
          <ul className={styles['services-page__list']}>
            {data.map((parent) => (
              <li key={parent.id} className={styles['services-page__group']}>
                <Link to={path(`/services/${parent.slug}`)} className={styles['services-page__parent']}>
                  <span className={styles['services-page__icon']}>
                    <Icon name={categoryIcon(parent.icon)} size={26} />
                  </span>
                  <span className={styles['services-page__name']}>{parent.name}</span>
                  <Icon name="chevronRight" className={styles['services-page__chevron']} />
                </Link>
                {parent.children.length > 0 && (
                  <ul className={styles['services-page__children']}>
                    {parent.children.map((child) => (
                      <li key={child.id}>
                        <Link to={path(`/services/${child.slug}`)} className={styles['services-page__child']}>
                          {child.name}
                        </Link>
                      </li>
                    ))}
                  </ul>
                )}
              </li>
            ))}
          </ul>
        )}
      </QueryState>
    </div>
  )
}
