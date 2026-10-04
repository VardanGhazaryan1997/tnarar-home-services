import { useTranslation } from 'react-i18next'
import Alert from '@/components/ui/Alert/Alert'
import { CheckboxField } from '@/components/ui/Field/Field'
import Icon from '@/components/ui/Icon/Icon'
import { categoryIcon } from '@/components/ui/Icon/icons'
import { LIMITS } from '../profileForm'
import styles from '../partner.module.scss'

/** Step 2: the services offered, as the category tree with checkboxes (a parent covers its subcategories in search). */
export default function ServicesStep({ form, set, categories, errorFor }) {
  const { t } = useTranslation()
  const selected = new Set(form.categoryIds)
  const full = selected.size >= LIMITS.services

  const toggle = (id) => {
    const next = new Set(selected)
    if (next.has(id)) next.delete(id)
    else next.add(id)
    set('categoryIds', [...next])
  }

  return (
    <div className={styles['partner-wizard__fields']}>
      <p className={styles['partner-wizard__intro']}>{t('partner.servicesIntro')}</p>
      <p className={styles['partner-wizard__count']} aria-live="polite">
        {t('partner.selected', { n: selected.size, max: LIMITS.services })}
      </p>
      <ul className={styles['check-tree']}>
        {categories.map((parent) => (
          <li key={parent.id} className={styles['check-tree__group']}>
            <div className={styles['check-tree__parent']}>
              <Icon name={categoryIcon(parent.icon)} className={styles['check-tree__icon']} />
              <CheckboxField
                label={parent.name}
                checked={selected.has(parent.id)}
                disabled={full && !selected.has(parent.id)}
                onChange={() => toggle(parent.id)}
              />
            </div>
            {parent.children.length > 0 && (
              <ul className={styles['check-tree__children']}>
                {parent.children.map((child) => (
                  <li key={child.id}>
                    <CheckboxField
                      label={child.name}
                      checked={selected.has(child.id)}
                      disabled={full && !selected.has(child.id)}
                      onChange={() => toggle(child.id)}
                    />
                  </li>
                ))}
              </ul>
            )}
          </li>
        ))}
      </ul>
      {errorFor('categoryIds') && <Alert tone="danger" title={errorFor('categoryIds')} />}
    </div>
  )
}
