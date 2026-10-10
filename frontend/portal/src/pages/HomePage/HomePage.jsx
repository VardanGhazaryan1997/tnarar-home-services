import { useTranslation } from 'react-i18next'
import { Link } from 'react-router'
import Button from '@/components/ui/Button/Button'
import Icon from '@/components/ui/Icon/Icon'
import { useCategories } from '@/features/catalog/catalogApi'
import QuickEstimate from '@/features/estimator/QuickEstimate'
import CategoryTiles from '@/features/public/CategoryTiles'
import PartnerCard from '@/features/public/PartnerCard'
import { useSearchPartners } from '@/features/public/publicApi'
import ServiceSearchForm from '@/features/public/ServiceSearchForm'
import { useLocalizedPath } from '@/i18n/hooks'
import styles from './HomePage.module.scss'

const STEPS = ['describe', 'offers', 'choose']
const PROMISES = [
  { key: 'checked', icon: 'shield' },
  { key: 'free', icon: 'money' },
  { key: 'private', icon: 'account' },
]

/**
 * The landing page: search by service and city, the main categories, how a request works, specialists who
 * joined recently and an invitation for specialists to join.
 */
export default function HomePage() {
  const { t } = useTranslation()
  const path = useLocalizedPath()
  const categories = useCategories()
  const recent = useSearchPartners({ pageSize: 4 })
  const partners = recent.data?.items ?? []

  return (
    <div className={styles['home-page']}>
      <title>{t('home.documentTitle')}</title>
      <section className={styles['home-page__hero']}>
        <h1 className={styles['home-page__title']}>{t('home.title')}</h1>
        <p className={styles['home-page__subtitle']}>{t('home.subtitle')}</p>
        <ServiceSearchForm />
        <p className={styles['home-page__or']}>
          {t('home.or')}{' '}
          <Link to={path('/requests/new')} className={styles['home-page__cta']}>
            {t('home.ctaInline')}
          </Link>
        </p>
      </section>

      <ul className={styles['home-page__promises']}>
        {PROMISES.map((item) => (
          <li key={item.key} className={styles['home-page__promise']}>
            <Icon name={item.icon} className={styles['home-page__promise-icon']} />
            {t(`home.promises.${item.key}`)}
          </li>
        ))}
      </ul>

      {categories.data?.length > 0 && (
        <section className={styles['home-page__section']} aria-labelledby="services-title">
          <div className={styles['home-page__section-head']}>
            <h2 id="services-title" className={styles['home-page__section-title']}>
              {t('home.servicesTitle')}
            </h2>
            <Button to={path('/services')} variant="secondary" size="sm">
              {t('home.allServices')}
            </Button>
          </div>
          <CategoryTiles categories={categories.data} label={t('home.servicesTitle')} />
        </section>
      )}

      <QuickEstimate />

      <section className={styles['home-page__section']} aria-labelledby="how-title">
        <h2 id="how-title" className={styles['home-page__section-title']}>
          {t('home.howTitle')}
        </h2>
        <ol className={styles['home-page__steps']}>
          {STEPS.map((step, index) => (
            <li key={step} className={styles['home-page__step']}>
              <span className={styles['home-page__step-number']}>{index + 1}</span>
              <span className={styles['home-page__step-title']}>{t(`home.steps.${step}.title`)}</span>
              <span className={styles['home-page__step-text']}>{t(`home.steps.${step}.text`)}</span>
            </li>
          ))}
        </ol>
      </section>

      {partners.length > 0 && (
        <section className={styles['home-page__section']} aria-labelledby="recent-title">
          <div className={styles['home-page__section-head']}>
            <h2 id="recent-title" className={styles['home-page__section-title']}>
              {t('home.recentTitle')}
            </h2>
            <Button to={path('/search')} variant="secondary" size="sm">
              {t('home.allPartners')}
            </Button>
          </div>
          <div className={styles['home-page__partners']}>
            {partners.map((partner) => (
              <PartnerCard key={partner.slug} partner={partner} />
            ))}
          </div>
        </section>
      )}

      <section className={styles['home-page__join']} aria-labelledby="join-title">
        <div className={styles['home-page__join-text']}>
          <h2 id="join-title" className={styles['home-page__section-title']}>
            {t('home.join.title')}
          </h2>
          <p>{t('home.join.text')}</p>
        </div>
        <Button to={path('/how-it-works?for=partners')} variant="accent" size="lg">
          {t('home.join.cta')}
        </Button>
      </section>
    </div>
  )
}
