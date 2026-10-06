import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { TextField, CheckboxField } from '@/components/ui/Field/Field'
import Button from '@/components/ui/Button/Button'
import EmptyState from '@/components/ui/EmptyState/EmptyState'
import Icon from '@/components/ui/Icon/Icon'
import { bem } from '@/shared/bem'
import { formatMoney } from '@/shared/format'
import { groupItems, pricedCount, rowOf } from './priceList'
import styles from './prices.module.scss'

const b = bem(styles)

/**
 * Edits a price list: the partner's work grouped by category, each with the usual market price as a hint, a price
 * (or a from–to range) and whether it includes materials. Controlled: `draft` rows come from usePriceList.
 */
export default function PriceListEditor({ items, draft, errors, onChange, onFillTypical, servicesLink }) {
  const { t } = useTranslation()
  const [search, setSearch] = useState('')
  const groups = groupItems(items, search)

  if (items.length === 0) {
    return (
      <EmptyState
        icon="money"
        title={t('partner.prices.emptyTitle')}
        description={t('partner.prices.emptyText')}
        action={
          servicesLink && (
            <Button to={servicesLink} variant="secondary">
              {t('partner.prices.chooseServices')}
            </Button>
          )
        }
      />
    )
  }

  return (
    <div className={styles['price-list']}>
      <div className={styles['price-list__toolbar']}>
        <TextField
          className={styles['price-list__search']}
          type="search"
          label={t('partner.prices.search')}
          value={search}
          onChange={(event) => setSearch(event.target.value)}
        />
        <p className={styles['price-list__count']} aria-live="polite">
          {t('partner.prices.count', { priced: pricedCount(draft), total: items.length })}
        </p>
        <Button variant="secondary" size="sm" icon={<Icon name="sparkle" />} onClick={onFillTypical}>
          {t('partner.prices.fillTypical')}
        </Button>
      </div>

      {groups.length === 0 && <p className={styles['price-list__none']}>{t('partner.prices.noMatches')}</p>}

      {groups.map((main) => (
        <section key={main.id} className={styles['price-list__group']} aria-labelledby={`price-group-${main.id}`}>
          <h3 id={`price-group-${main.id}`} className={styles['price-list__main']}>
            {main.name}
          </h3>
          {main.subcategories.map((sub) => (
            <div key={sub.id} className={styles['price-list__sub']}>
              <h4 className={styles['price-list__sub-title']}>{sub.name}</h4>
              <ul className={styles['price-list__rows']}>
                {sub.items.map((item) => (
                  <PriceRow
                    key={item.workItemId}
                    item={item}
                    row={rowOf(draft, item.workItemId)}
                    error={errors[item.workItemId]}
                    onChange={(patch) => onChange(item.workItemId, patch)}
                  />
                ))}
              </ul>
            </div>
          ))}
        </section>
      ))}
    </div>
  )
}

function PriceRow({ item, row, error, onChange }) {
  const { t, i18n } = useTranslation()
  const lng = i18n.language
  const unit = t(`partner.prices.per.${item.unit}`)
  const priced = row.from.trim() !== '' || row.to.trim() !== ''
  const market =
    item.marketTypical != null
      ? item.marketMin === item.marketMax
        ? formatMoney(item.marketTypical, lng)
        : `${formatMoney(item.marketMin, lng)} – ${formatMoney(item.marketMax, lng)}`
      : null

  return (
    <li className={b('price-row', { priced, invalid: Boolean(error) })} aria-label={item.name}>
      <div className={styles['price-row__head']}>
        <span className={styles['price-row__name']}>{item.name}</span>
        <span className={styles['price-row__unit']}>{unit}</span>
      </div>
      <p className={styles['price-row__market']}>
        {market ? t('partner.prices.market', { range: market }) : t('partner.prices.noMarket')}
      </p>
      <div className={styles['price-row__inputs']}>
        <TextField
          className={styles['price-row__field']}
          label={t('partner.prices.from')}
          inputMode="numeric"
          autoComplete="off"
          value={row.from}
          error={error ? t(error) : undefined}
          onChange={(event) => onChange({ from: event.target.value })}
        />
        <TextField
          className={styles['price-row__field']}
          label={t('partner.prices.to')}
          optionalText={t('common.optional')}
          inputMode="numeric"
          autoComplete="off"
          value={row.to}
          onChange={(event) => onChange({ to: event.target.value })}
        />
      </div>
      <div className={styles['price-row__actions']}>
        <CheckboxField
          label={t('partner.prices.materials')}
          checked={row.materials}
          onChange={(event) => onChange({ materials: event.target.checked })}
        />
        {item.marketTypical != null && (
          <Button variant="ghost" size="sm" onClick={() => onChange({ from: String(item.marketTypical), to: '' })}>
            {t('partner.prices.useTypical')}
          </Button>
        )}
        {priced && (
          <Button variant="ghost" size="sm" onClick={() => onChange({ from: '', to: '', materials: false })}>
            {t('partner.prices.clear')}
          </Button>
        )}
      </div>
    </li>
  )
}
