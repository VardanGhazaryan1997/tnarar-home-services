import { useTranslation } from 'react-i18next'
import Icon from '@/components/ui/Icon/Icon'
import { bem } from '@/shared/bem'
import styles from './orders.module.scss'

const b = bem(styles)
const STEPS = ['Confirmed', 'InProgress', 'CompletionRequested', 'Completed']

/** Where the order is on its way: confirmed → in progress → awaiting confirmation → completed. */
export default function OrderProgress({ status }) {
  const { t } = useTranslation()
  const current = STEPS.indexOf(status)

  return (
    <ol className={styles['order-progress']} aria-label={t('orders.progressLabel')}>
      {STEPS.map((step, index) => {
        const state = index < current || status === 'Completed' ? 'done' : index === current ? 'current' : 'next'
        return (
          <li key={step} className={b('order-progress__step', { [state]: true })} aria-current={state === 'current' ? 'step' : undefined}>
            <span className={styles['order-progress__dot']}>{state === 'done' ? <Icon name="check" size={14} /> : index + 1}</span>
            <span className={styles['order-progress__label']}>{t(`orders.status.${step}`)}</span>
          </li>
        )
      })}
    </ol>
  )
}
