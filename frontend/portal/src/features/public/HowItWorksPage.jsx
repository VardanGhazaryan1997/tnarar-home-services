import { useTranslation } from 'react-i18next'
import { useSearchParams } from 'react-router'
import PageHeader from '@/components/PageHeader/PageHeader'
import Button from '@/components/ui/Button/Button'
import Icon from '@/components/ui/Icon/Icon'
import Segmented from '@/components/ui/Segmented/Segmented'
import { useLocalizedPath } from '@/i18n/hooks'
import FaqList from './FaqList'
import { useFaqs } from './publicApi'
import styles from './HowItWorksPage.module.scss'

const CONTENT = {
  customers: {
    audience: 'Customers',
    steps: ['describe', 'offers', 'choose', 'done'],
    benefits: [
      { key: 'checked', icon: 'shield' },
      { key: 'free', icon: 'money' },
      { key: 'chat', icon: 'messages' },
    ],
    cta: { to: '/requests/new', icon: 'plus' },
  },
  partners: {
    audience: 'Partners',
    steps: ['profile', 'review', 'requests', 'offers'],
    benefits: [
      { key: 'clients', icon: 'users' },
      { key: 'showcase', icon: 'image' },
      { key: 'tools', icon: 'tools' },
    ],
    cta: { to: '/partner', icon: 'edit' },
  },
}

/** How Tnarar works, for customers or for partners (`?for=partners`), with the questions people ask. */
export default function HowItWorksPage() {
  const { t } = useTranslation()
  const path = useLocalizedPath()
  const [params, setParams] = useSearchParams()
  const side = params.get('for') === 'partners' ? 'partners' : 'customers'
  const content = CONTENT[side]
  const faqs = (useFaqs().data ?? []).filter((faq) => faq.audience === 'General' || faq.audience === content.audience)

  return (
    <div className={styles['how-page']}>
      <title>{t('public.how.documentTitle')}</title>
      <PageHeader title={t('public.how.title')} subtitle={t(`public.how.${side}.subtitle`)} />
      <Segmented
        label={t('public.how.forWhom')}
        options={['customers', 'partners'].map((value) => ({ value, label: t(`public.how.${value}.tab`) }))}
        value={side}
        onChange={(value) => setParams(value === 'partners' ? { for: 'partners' } : {}, { replace: true })}
        className={styles['how-page__switch']}
      />

      <ol className={styles['how-page__steps']}>
        {content.steps.map((step, index) => (
          <li key={step} className={styles['how-page__step']}>
            <span className={styles['how-page__number']}>{index + 1}</span>
            <div>
              <h2 className={styles['how-page__step-title']}>{t(`public.how.${side}.steps.${step}.title`)}</h2>
              <p className={styles['how-page__text']}>{t(`public.how.${side}.steps.${step}.text`)}</p>
            </div>
          </li>
        ))}
      </ol>

      <ul className={styles['how-page__benefits']}>
        {content.benefits.map((benefit) => (
          <li key={benefit.key} className={styles['how-page__benefit']}>
            <span className={styles['how-page__benefit-icon']}>
              <Icon name={benefit.icon} />
            </span>
            <span className={styles['how-page__benefit-title']}>{t(`public.how.${side}.benefits.${benefit.key}.title`)}</span>
            <span className={styles['how-page__text']}>{t(`public.how.${side}.benefits.${benefit.key}.text`)}</span>
          </li>
        ))}
      </ul>

      <div className={styles['how-page__cta']}>
        <p className={styles['how-page__cta-text']}>{t(`public.how.${side}.ctaText`)}</p>
        <Button to={path(content.cta.to)} variant="accent" size="lg" icon={<Icon name={content.cta.icon} />}>
          {t(`public.how.${side}.cta`)}
        </Button>
      </div>

      {faqs.length > 0 && (
        <section className={styles['how-page__faq']} aria-labelledby="faq-title">
          <h2 id="faq-title" className={styles['how-page__faq-title']}>
            {t('public.how.faq')}
          </h2>
          <FaqList items={faqs} />
        </section>
      )}
    </div>
  )
}
