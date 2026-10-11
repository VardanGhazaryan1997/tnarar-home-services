import { useTranslation } from 'react-i18next'
import Card from '@/components/ui/Card/Card'
import { intlLocale } from '@/i18n/languages'
import { formatMoney } from '@/shared/format'
import styles from './RequestLines.module.scss'

/**
 * The materials the request's estimate counted (E1), as a card: each material's amount and, for the customer
 * (`showEstimate`), its estimated cost and the total. Partners see the amounts so they can quote the materials. Nothing
 * when the estimate was labour only.
 */
export default function RequestMaterials({ request, showEstimate = false, hint }) {
  const { t, i18n } = useTranslation()
  const materials = request.materials ?? []
  if (!request.materialGrade || materials.length === 0) return null
  const lng = i18n.language
  const number = (value) => new Intl.NumberFormat(intlLocale(lng), { maximumFractionDigits: 1 }).format(value)
  const title = t('estimator.materials.title', { grade: t(`estimator.materials.grades.${request.materialGrade}`) })
  const total = materials.reduce((sum, material) => sum + (material.estimateCost ?? 0), 0)

  return (
    <Card title={title} aria-label={title}>
      {hint && <p className={styles['request-lines__hint']}>{hint}</p>}
      <ul className={styles['request-lines__list']}>
        {materials.map((material) => (
          <li key={material.materialId} className={styles['request-lines__line']}>
            <span>{material.name}</span>
            <span className={styles['request-lines__amount']}>
              {number(material.quantity)} {t(`estimator.materials.units.${material.unit}`)}
            </span>
            {showEstimate && <span className={styles['request-lines__estimate']}>{formatMoney(material.estimateCost ?? 0, lng)}</span>}
          </li>
        ))}
      </ul>
      {showEstimate && <p className={styles['request-lines__total']}>{t('estimator.materials.total', { amount: formatMoney(total, lng) })}</p>}
    </Card>
  )
}
