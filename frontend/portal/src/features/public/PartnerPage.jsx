import { useTranslation } from 'react-i18next'
import { Link, useParams } from 'react-router'
import Avatar from '@/components/Avatar/Avatar'
import QueryState from '@/components/QueryState/QueryState'
import Button from '@/components/ui/Button/Button'
import Card from '@/components/ui/Card/Card'
import EmptyState from '@/components/ui/EmptyState/EmptyState'
import Icon from '@/components/ui/Icon/Icon'
import Tag from '@/components/ui/Tag/Tag'
import { useLocalizedPath } from '@/i18n/hooks'
import { formatDate } from '@/shared/format'
import Gallery from './Gallery'
import { Rating } from './PartnerCard'
import PartnerReviews from './PartnerReviews'
import { usePartner } from './publicApi'
import styles from './PartnerPage.module.scss'

/** Areas grouped by city: "Yerevan: Kentron, Arabkir" or "Yerevan (whole city)". */
function groupAreas(areas) {
  const cities = new Map()
  areas.forEach((area) => {
    const city = cities.get(area.citySlug) ?? { name: area.cityName, whole: false, districts: [] }
    if (area.districtSlug) city.districts.push(area.districtName)
    else city.whole = true
    cities.set(area.citySlug, city)
  })
  return [...cities.entries()].map(([slug, city]) => ({ slug, ...city }))
}

/**
 * An approved partner's public profile: who they are, services, areas, about and work examples, with a
 * button to send them a request. Phone numbers stay private until an order is agreed.
 */
export default function PartnerPage() {
  const { t, i18n } = useTranslation()
  const path = useLocalizedPath()
  const { slug } = useParams()
  const query = usePartner(slug)

  const notFound = (
    <EmptyState
      icon="search"
      title={t('public.profile.notFound')}
      description={t('public.profile.notFoundText')}
      action={<Button to={path('/search')}>{t('home.allPartners')}</Button>}
    />
  )

  return (
    <QueryState query={query} notFound={notFound}>
      {(partner) => {
        const requestTo = path(`/requests/new?partner=${partner.id}&name=${encodeURIComponent(partner.displayName)}`)
        return (
          <div className={styles['partner-page']}>
            <title>{t('public.profile.documentTitle', { name: partner.displayName })}</title>
            <section className={styles['partner-page__hero']}>
              <div className={styles['partner-page__banner']} />
              <div className={styles['partner-page__identity']}>
                <Avatar name={partner.displayName} src={partner.avatar?.thumbnailUrl ?? partner.avatar?.url} size="xl" className={styles['partner-page__avatar']} />
                <div className={styles['partner-page__who']}>
                  <h1 className={styles['partner-page__name']}>{partner.displayName}</h1>
                  <p className={styles['partner-page__facts']}>
                    <Tag tone={partner.type === 'Company' ? 'info' : 'neutral'}>{t(`public.partnerType.${partner.type}`)}</Tag>
                    {partner.yearsOfExperience != null && <span>{t('public.years', { n: partner.yearsOfExperience })}</span>}
                    {partner.memberSince && (
                      <span className={styles['partner-page__fact']}>
                        <Icon name="shield" size={16} />
                        {t('public.profile.memberSince', { date: formatDate(partner.memberSince, i18n.language, { year: 'numeric' }) })}
                      </span>
                    )}
                  </p>
                  {partner.reviewCount > 0 && <Rating rating={partner.rating} count={partner.reviewCount} className={styles['partner-page__rating']} />}
                </div>
                <div className={styles['partner-page__actions']}>
                  <Button to={requestTo} variant="accent" size="lg" icon={<Icon name="send" />}>
                    {t('public.profile.request')}
                  </Button>
                </div>
              </div>
            </section>

            <div className={styles['partner-page__layout']}>
              <div className={styles['partner-page__main']}>
                {partner.about && (
                  <Card title={t('public.profile.about')}>
                    <p className={styles['partner-page__about']}>{partner.about}</p>
                  </Card>
                )}
                <Card title={t('public.profile.work', { n: partner.workExamples.length })}>
                  {partner.workExamples.length ? (
                    <Gallery items={partner.workExamples} label={t('public.profile.workList')} />
                  ) : (
                    <p className={styles['partner-page__muted']}>{t('public.profile.noWork')}</p>
                  )}
                </Card>
                <PartnerReviews partner={partner} />
              </div>

              <aside className={styles['partner-page__side']}>
                <Card title={t('public.profile.services')}>
                  <ul className={styles['partner-page__tags']}>
                    {partner.categories.map((category) => (
                      <li key={category.slug}>
                        <Link to={path(`/services/${category.slug}`)} className={styles['partner-page__service']}>
                          {category.name}
                        </Link>
                      </li>
                    ))}
                  </ul>
                </Card>
                <Card title={t('public.profile.areas')}>
                  <ul className={styles['partner-page__areas']}>
                    {groupAreas(partner.areas).map((city) => (
                      <li key={city.slug} className={styles['partner-page__area']}>
                        <Icon name="pin" size={18} className={styles['partner-page__area-icon']} />
                        <span>
                          <strong>{city.name}</strong>
                          <br />
                          <span className={styles['partner-page__muted']}>{city.whole ? t('public.profile.wholeCity') : city.districts.join(', ')}</span>
                        </span>
                      </li>
                    ))}
                  </ul>
                </Card>
                <Card className={styles['partner-page__contact']}>
                  <p className={styles['partner-page__contact-title']}>{t('public.profile.contactTitle')}</p>
                  <p className={styles['partner-page__muted']}>{t('public.profile.contactText')}</p>
                  <Button to={requestTo} variant="primary" block icon={<Icon name="send" />}>
                    {t('public.profile.request')}
                  </Button>
                </Card>
              </aside>
            </div>
          </div>
        )
      }}
    </QueryState>
  )
}
