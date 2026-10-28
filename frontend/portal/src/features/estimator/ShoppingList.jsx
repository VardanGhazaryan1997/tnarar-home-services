import { useEffect, useState } from 'react'
import { createPortal } from 'react-dom'
import { useTranslation } from 'react-i18next'
import Button from '@/components/ui/Button/Button'
import { SelectField } from '@/components/ui/Field/Field'
import Icon from '@/components/ui/Icon/Icon'
import Modal from '@/components/ui/Modal/Modal'
import Segmented from '@/components/ui/Segmented/Segmented'
import { formatMoney } from '@/shared/format'
import { atOneStore, byCheapest, itemText, missingText, toText } from './shoppingListData'
import { topStores } from './storeOffers'
import styles from './ShoppingList.module.scss'

/** Marks the page while the list is open, so printing prints only the list. */
const PRINTING = 'shopping-list-printing'

/** The list itself: stores with their items and totals, the materials no store has, and the grand total. */
function ListView({ list, t, lng }) {
  return (
    <div className={styles['shopping-list__content']}>
      {list.groups.map((group) => (
        <section key={group.storeSlug} className={styles['shopping-list__store']} aria-label={group.storeName}>
          <h3 className={styles['shopping-list__store-name']}>{group.storeName}</h3>
          <ul className={styles['shopping-list__items']}>
            {group.items.map((item) => (
              <li key={item.materialId} className={styles['shopping-list__item']}>
                {itemText(item, t, lng)}
              </li>
            ))}
          </ul>
          <p className={styles['shopping-list__store-total']}>{t('whereToBuy.list.storeTotal', { amount: formatMoney(group.total, lng) })}</p>
        </section>
      ))}
      {list.missing.length > 0 && (
        <section className={styles['shopping-list__store']} aria-label={t('whereToBuy.list.missing')}>
          <h3 className={styles['shopping-list__store-name']}>{t('whereToBuy.list.missing')}</h3>
          <ul className={styles['shopping-list__items']}>
            {list.missing.map((material) => (
              <li key={material.name} className={styles['shopping-list__item']}>
                {missingText(material, t, lng)}
              </li>
            ))}
          </ul>
        </section>
      )}
      <p className={styles['shopping-list__total']}>{t('whereToBuy.list.total', { amount: formatMoney(list.total, lng) })}</p>
    </div>
  )
}

/**
 * The shopping list of an estimate's materials (R5): each material at its cheapest store, or everything at one store,
 * to print, copy or send on WhatsApp. `data`: the where-to-buy answer; `measurement`: the estimate.
 */
export default function ShoppingList({ data, measurement }) {
  const { t, i18n } = useTranslation()
  const lng = i18n.language
  const stores = topStores(data)
  const [open, setOpen] = useState(false)
  const [mode, setMode] = useState('cheapest')
  const [storeSlug, setStoreSlug] = useState(null)
  const [copied, setCopied] = useState(false)

  const chosenStore = storeSlug ?? stores[0]?.storeSlug
  const list = mode === 'cheapest' ? byCheapest(data, measurement) : atOneStore(data, measurement, chosenStore)
  const title = t('whereToBuy.list.title', { grade: t(`estimator.materials.grades.${measurement.materialGrade}`) })
  const text = toText(list, t, lng, title)
  const canCopy = typeof navigator !== 'undefined' && Boolean(navigator.clipboard?.writeText)

  // While open, printing shows only the list (see the print rules in styles/global.scss).
  useEffect(() => {
    if (!open) return undefined
    document.body.classList.add(PRINTING)
    return () => document.body.classList.remove(PRINTING)
  }, [open])

  const close = () => {
    setOpen(false)
    setCopied(false)
  }

  const copy = async () => {
    try {
      await navigator.clipboard.writeText(text)
      setCopied(true)
    } catch {
      setCopied(false)
    }
  }

  return (
    <>
      <Button variant="secondary" size="sm" icon={<Icon name="file" size={16} />} onClick={() => setOpen(true)}>
        {t('whereToBuy.list.open')}
      </Button>
      <Modal open={open} title={title} onClose={close}>
        <div className={styles['shopping-list']}>
          <Segmented
            label={t('whereToBuy.list.mode')}
            options={[
              { value: 'cheapest', label: t('whereToBuy.list.cheapest') },
              { value: 'store', label: t('whereToBuy.list.oneStore') },
            ]}
            value={mode}
            onChange={(value) => {
              setMode(value)
              setCopied(false)
            }}
          />
          {mode === 'store' && (
            <SelectField
              label={t('whereToBuy.list.store')}
              options={stores.map((store) => ({ value: store.storeSlug, label: store.storeName }))}
              value={chosenStore ?? ''}
              onChange={(event) => {
                setStoreSlug(event.target.value)
                setCopied(false)
              }}
            />
          )}
          <ListView list={list} t={t} lng={lng} />
          <div className={styles['shopping-list__actions']}>
            <Button variant="secondary" size="sm" onClick={() => window.print()}>
              {t('whereToBuy.list.print')}
            </Button>
            {canCopy && (
              <Button variant="secondary" size="sm" onClick={copy}>
                {t('whereToBuy.list.copy')}
              </Button>
            )}
            <a className={styles['shopping-list__whatsapp']} href={`https://wa.me/?text=${encodeURIComponent(text)}`} target="_blank" rel="noopener noreferrer">
              {t('whereToBuy.list.whatsapp')}
            </a>
          </div>
          {copied && (
            <p className={styles['shopping-list__copied']} role="status">
              {t('whereToBuy.list.copied')}
            </p>
          )}
        </div>
      </Modal>
      {open &&
        createPortal(
          <div className="shopping-list-sheet" aria-hidden="true">
            <h2 className={styles['shopping-list__sheet-title']}>{title}</h2>
            <ListView list={list} t={t} lng={lng} />
          </div>,
          document.body,
        )}
    </>
  )
}
