import { useTranslation } from 'react-i18next'
import Card from '@/components/ui/Card/Card'
import { intlLocale } from '@/i18n/languages'
import { formatMoney } from '@/shared/format'
import styles from './estimator.module.scss'

/** The materials an estimate needs (E1): each with its amount and cost, the total, and a note on how rough it is. */
export default function MaterialsList({ measurement }) {
  const { t, i18n } = useTranslation()
  const lng = i18n.language
  const number = (value) => new Intl.NumberFormat(intlLocale(lng), { maximumFractionDigits: 1 }).format(value)
  const title = t('estimator.materials.title', { grade: t(`estimator.materials.grades.${measurement.materialGrade}`) })

  return (
    <Card title={title} aria-label={title}>
      <ul className={styles['quick-estimate__lines']}>
        {measurement.materials.map((material) => (
          <li key={material.materialId} className={styles['quick-estimate__line']}>
            <span>
              {material.name}
              <span className={styles['quick-estimate__quantity']}>
                {' · '}
                {number(material.quantity)} {t(`estimator.materials.units.${material.unit}`)}
              </span>
            </span>
            <span className={styles['quick-estimate__line-price']}>{formatMoney(material.cost, lng)}</span>
          </li>
        ))}
      </ul>
      <p className={styles['room__total']}>{t('estimator.materials.total', { amount: formatMoney(measurement.materialsTotal, lng) })}</p>
      <p className={styles['quick-estimate__note']}>{t('estimator.materials.note')}</p>
    </Card>
  )
}
