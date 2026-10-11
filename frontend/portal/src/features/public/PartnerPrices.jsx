import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import Button from '@/components/ui/Button/Button'
import Card from '@/components/ui/Card/Card'
import Tag from '@/components/ui/Tag/Tag'
import { formatMoney } from '@/shared/format'
import { usePartnerPrices } from './publicApi'
import styles from './PartnerPage.module.scss'

/** How many prices show before "Show all". */
const SHOWN = 8
const TONES = { Below: 'success', Typical: 'neutral', Above: 'warning' }

/** A partner's prices on their public profile, each next to the market range (A7). Hidden while they have none. */
export default function PartnerPrices({ slug }) {
  const { t, i18n } = useTranslation()
  const lng = i18n.language
  const { data: prices = [] } = usePartnerPrices(slug)
  const [all, setAll] = useState(false)
  if (prices.length === 0) return null

  const money = (amount) => formatMoney(amount, lng)
  const shown = all ? prices : prices.slice(0, SHOWN)
  const groups = []
  for (const price of shown) {
    const group = groups.at(-1)?.id === price.mainCategoryId ? groups.at(-1) : groups[groups.push({ id: price.mainCategoryId, name: price.mainCategoryName, items: [] }) - 1]
    group.items.push(price)
  }

  return (
    <Card title={t('public.prices.title', { n: prices.length })}>
      <p className={styles['partner-page__muted']}>{t('public.prices.hint')}</p>
      {groups.map((group) => (
        <section key={group.id} className={styles['partner-page__prices']} aria-label={group.name}>
          <h3 className={styles['partner-page__prices-title']}>{group.name}</h3>
          <ul className={styles['partner-page__price-list']}>
            {group.items.map((price) => (
              <li key={price.workItemId} className={styles['partner-page__price']}>
                <span className={styles['partner-page__price-name']}>
                  {price.name}
                  {price.marketTypical != null && (
                    <span className={styles['partner-page__muted']}>
                      {t('public.prices.market', { range: `${money(price.marketMin)} – ${money(price.marketMax)}` })}
                    </span>
                  )}
                </span>
                <span className={styles['partner-page__price-value']}>
                  <strong>
                    {price.priceTo != null ? `${money(price.priceFrom)} – ${money(price.priceTo)}` : money(price.priceFrom)}
                    {' / '}
                    {t(`estimator.units.${price.unit}`)}
                  </strong>
                  {price.comparison && <Tag tone={TONES[price.comparison]}>{t(`public.prices.comparison.${price.comparison}`)}</Tag>}
                  {price.includesMaterials && <Tag tone="info">{t('public.prices.withMaterials')}</Tag>}
                </span>
              </li>
            ))}
          </ul>
        </section>
      ))}
      {prices.length > SHOWN && (
        <Button variant="ghost" size="sm" onClick={() => setAll(!all)}>
          {all ? t('public.prices.fewer') : t('public.prices.all', { n: prices.length })}
        </Button>
      )}
    </Card>
  )
}
