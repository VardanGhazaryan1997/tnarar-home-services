import { useMemo, useState } from 'react'
import { useTranslation } from 'react-i18next'
import Button from '@/components/ui/Button/Button'
import { TextField } from '@/components/ui/Field/Field'
import Icon from '@/components/ui/Icon/Icon'
import Modal from '@/components/ui/Modal/Modal'
import { useCategories } from '@/features/catalog/catalogApi'
import { formatMoney } from '@/shared/format'
import styles from './estimator.module.scss'

/** How many matches to list at once; typing narrows them down. */
const SHOWN = 60

/**
 * Picks more work for a room: search by name, grouped by service. Work already in the room isn't offered again.
 * `onPick(item)` adds one item; the dialog stays open so several can be added.
 */
export default function WorkPicker({ open, roomName, workItems, taken, onPick, onClose }) {
  const { t, i18n } = useTranslation()
  const { data: categories = [] } = useCategories()
  const [search, setSearch] = useState('')
  const lng = i18n.language

  // Subcategory id → "Main › Sub", for group headings.
  const groupNames = useMemo(() => {
    const names = new Map()
    for (const main of categories) {
      names.set(main.id, main.name)
      for (const sub of main.children ?? []) names.set(sub.id, `${main.name} › ${sub.name}`)
    }
    return names
  }, [categories])

  const term = search.trim().toLocaleLowerCase()
  const matches = workItems.filter((item) => !taken.has(item.id) && (!term || item.name.toLocaleLowerCase().includes(term)))
  const groups = []
  for (const item of matches.slice(0, SHOWN)) {
    const name = groupNames.get(item.categoryId) ?? ''
    const group = groups.at(-1)?.name === name ? groups.at(-1) : groups[groups.push({ name, items: [] }) - 1]
    group.items.push(item)
  }

  const close = () => {
    setSearch('')
    onClose()
  }

  return (
    <Modal open={open} title={t('estimator.picker.title', { room: roomName })} onClose={close} footer={<Button onClick={close}>{t('estimator.picker.done')}</Button>}>
      <div className={styles['work-picker']}>
        <TextField label={t('estimator.picker.search')} type="search" autoComplete="off" value={search} onChange={(event) => setSearch(event.target.value)} />
        {matches.length === 0 && <p className={styles['work-picker__none']}>{t('estimator.picker.none')}</p>}
        {groups.map((group) => (
          <section key={group.name} className={styles['work-picker__group']} aria-label={group.name}>
            {group.name && <h3 className={styles['work-picker__heading']}>{group.name}</h3>}
            <ul className={styles['work-picker__list']}>
              {group.items.map((item) => (
                <li key={item.id} className={styles['work-picker__item']}>
                  <span className={styles['work-picker__name']}>
                    {item.name}
                    <span className={styles['work-picker__price']}>
                      {item.priceTypical != null
                        ? t('estimator.picker.price', { amount: formatMoney(item.priceTypical, lng), unit: t(`estimator.units.${item.unit}`) })
                        : t('estimator.room.noPrice')}
                    </span>
                  </span>
                  <Button variant="secondary" size="sm" icon={<Icon name="plus" />} aria-label={t('estimator.picker.add', { name: item.name })} onClick={() => onPick(item)} />
                </li>
              ))}
            </ul>
          </section>
        ))}
        {matches.length > SHOWN && <p className={styles['work-picker__none']}>{t('estimator.picker.more', { number: matches.length - SHOWN })}</p>}
      </div>
    </Modal>
  )
}
