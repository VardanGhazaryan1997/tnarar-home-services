import { useTranslation } from 'react-i18next'
import { Link } from 'react-router'
import BrandLogo from '@/components/BrandLogo/BrandLogo'
import { usePages } from '@/features/public/publicApi'
import { useLocalizedPath } from '@/i18n/hooks'
import styles from './SiteFooter.module.scss'

const YEAR = new Date().getFullYear()
const LINKS = [
  { key: 'services', to: '/services' },
  { key: 'search', to: '/search' },
  { key: 'how', to: '/how-it-works' },
  { key: 'partners', to: '/how-it-works?for=partners' },
]

/** The site footer: main public links, the information pages marked for the footer, and the copyright. */
export default function SiteFooter() {
  const { t } = useTranslation()
  const path = useLocalizedPath()
  const pages = (usePages().data ?? []).filter((page) => page.showInFooter)

  return (
    <footer className={styles['site-footer']}>
      <div className={styles['site-footer__inner']}>
        <div className={styles['site-footer__brand']}>
          <BrandLogo size={28} />
          <p className={styles['site-footer__tagline']}>{t('layout.tagline')}</p>
        </div>
        <nav className={styles['site-footer__nav']} aria-label={t('layout.footerNavigation')}>
          <ul className={styles['site-footer__links']}>
            {LINKS.map((link) => (
              <li key={link.key}>
                <Link to={path(link.to)} className={styles['site-footer__link']}>
                  {t(`layout.links.${link.key}`)}
                </Link>
              </li>
            ))}
          </ul>
          {pages.length > 0 && (
            <ul className={styles['site-footer__links']}>
              {pages.map((page) => (
                <li key={page.slug}>
                  <Link to={path(`/pages/${page.slug}`)} className={styles['site-footer__link']}>
                    {page.title}
                  </Link>
                </li>
              ))}
            </ul>
          )}
        </nav>
      </div>
      <p className={styles['site-footer__copyright']}>{t('layout.footer', { year: YEAR })}</p>
    </footer>
  )
}
