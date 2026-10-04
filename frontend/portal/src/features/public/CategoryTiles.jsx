import { Link } from 'react-router'
import Icon from '@/components/ui/Icon/Icon'
import { categoryIcon } from '@/components/ui/Icon/icons'
import { useLocalizedPath } from '@/i18n/hooks'
import styles from './CategoryTiles.module.scss'

/** The main service categories as large tiles that open their search page. */
export default function CategoryTiles({ categories, label }) {
  const path = useLocalizedPath()
  return (
    <ul className={styles['category-tiles']} aria-label={label}>
      {categories.map((category) => (
        <li key={category.id}>
          <Link to={path(`/services/${category.slug}`)} className={styles['category-tiles__tile']}>
            <span className={styles['category-tiles__icon']}>
              <Icon name={categoryIcon(category.icon)} size={26} />
            </span>
            <span className={styles['category-tiles__name']}>{category.name}</span>
          </Link>
        </li>
      ))}
    </ul>
  )
}
