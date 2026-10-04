import { useMemo, useState } from 'react'
import { useTranslation } from 'react-i18next'
import Alert from '@/components/ui/Alert/Alert'
import Button from '@/components/ui/Button/Button'
import { CheckboxField, TextField } from '@/components/ui/Field/Field'
import { groupPlaces, hasArea, hasRegion, LIMITS, withCity, withDistrict, withRegion } from '../profileForm'
import styles from '../partner.module.scss'

/**
 * Step 3: where the partner works. Places are grouped by region: a partner picks a whole region, single towns and
 * villages, or districts of a city (Yerevan). A whole region replaces the places chosen inside it, and a whole city
 * replaces its districts. Typing in the search box lists matching places across all regions.
 */
export default function AreasStep({ form, set, cities, regions = [], errorFor }) {
  const { t } = useTranslation()
  const [search, setSearch] = useState('')
  const groups = useMemo(() => groupPlaces(cities, regions), [cities, regions])
  const full = form.areas.length >= LIMITS.areas
  const term = search.trim().toLocaleLowerCase()
  const matches = term ? cities.filter((city) => city.name.toLocaleLowerCase().includes(term)) : []
  const regionName = (id) => regions.find((region) => region.id === id)?.name

  return (
    <div className={styles['partner-wizard__fields']}>
      <p className={styles['partner-wizard__intro']}>{t('partner.areasIntro')}</p>
      <p className={styles['partner-wizard__count']} aria-live="polite">
        {t('partner.selected', { n: form.areas.length, max: LIMITS.areas })}
      </p>
      <TextField
        label={t('partner.searchPlaces')}
        type="search"
        value={search}
        onChange={(event) => setSearch(event.target.value)}
        autoComplete="off"
      />
      {term ? (
        matches.length ? (
          <ul className={styles['area-picker__list']}>
            {matches.map((city) => (
              <li key={city.id}>
                <CityChoice
                  city={city}
                  form={form}
                  set={set}
                  full={full}
                  label={regionName(city.regionId) ? `${city.name} · ${regionName(city.regionId)}` : city.name}
                />
              </li>
            ))}
          </ul>
        ) : (
          <p className={styles['area-picker__empty']}>{t('partner.noPlaces')}</p>
        )
      ) : (
        <ul className={styles['area-picker']}>
          {groups.map((group) => (
            <RegionGroup key={group.region?.id ?? 'other'} group={group} form={form} set={set} full={full} />
          ))}
        </ul>
      )}
      {errorFor('areas') && <Alert tone="danger" title={errorFor('areas')} />}
    </div>
  )
}

/** One region: "All of …", then its towns and villages behind a toggle. A region with one place shows just that place. */
function RegionGroup({ group, form, set, full }) {
  const { t } = useTranslation()
  const { region, places } = group
  const single = !region || places.length === 1
  const whole = region ? hasRegion(form.areas, region.id) : false
  const chosenInside = places.some((city) => form.areas.some((area) => area.cityId === city.id))
  const [open, setOpen] = useState(single || chosenInside)
  const towns = places.filter((city) => city.kind !== 'Village')
  const villages = places.filter((city) => city.kind === 'Village')

  if (single && places.length === 1) {
    return (
      <li className={styles['area-picker__city']}>
        <CityChoice city={places[0]} form={form} set={set} full={full} withDistricts />
      </li>
    )
  }

  const title = region?.name ?? t('partner.otherPlaces')
  return (
    <li className={styles['area-picker__city']}>
      <div className={styles['area-picker__head']}>
        {region ? (
          <CheckboxField
            label={t('partner.wholeRegion', { region: region.name })}
            checked={whole}
            disabled={full && !whole}
            onChange={() => set('areas', withRegion(form.areas, region.id, places, !whole))}
            className={styles['area-picker__whole']}
          />
        ) : (
          <span className={styles['area-picker__whole']}>{title}</span>
        )}
        {!whole && (
          <Button variant="ghost" size="sm" aria-expanded={open} onClick={() => setOpen(!open)}>
            {open ? t('partner.hidePlaces') : t('partner.showPlaces', { count: places.length })}
          </Button>
        )}
      </div>
      {whole && <p className={styles['area-picker__legend']}>{t('partner.coversRegion', { region: region.name })}</p>}
      {!whole && open && (
        <div className={styles['area-picker__places']}>
          {[
            ['towns', towns],
            ['villages', villages],
          ].map(([kind, list]) =>
            list.length ? (
              <fieldset key={kind} className={styles['area-picker__districts']}>
                <legend className={styles['area-picker__legend']}>{t(`partner.${kind}`)}</legend>
                {list.map((city) => (
                  <CityChoice key={city.id} city={city} form={form} set={set} full={full} withDistricts />
                ))}
              </fieldset>
            ) : null,
          )}
        </div>
      )}
    </li>
  )
}

/** A town or village checkbox; with `withDistricts`, a city that has districts also offers them. */
function CityChoice({ city, form, set, full, label, withDistricts = false }) {
  const { t } = useTranslation()
  const whole = hasArea(form.areas, city.id)
  const districts = withDistricts ? city.districts : []
  return (
    <>
      <CheckboxField
        label={label ?? (districts.length ? t('partner.wholeCity', { city: city.name }) : city.name)}
        checked={whole}
        disabled={full && !whole}
        onChange={() => set('areas', withCity(form.areas, city.id, !whole))}
        className={districts.length ? styles['area-picker__whole'] : undefined}
      />
      {districts.length > 0 && !whole && (
        <fieldset className={styles['area-picker__districts']}>
          <legend className={styles['area-picker__legend']}>{t('partner.orDistricts', { city: city.name })}</legend>
          {districts.map((district) => {
            const checked = hasArea(form.areas, city.id, district.id)
            return (
              <CheckboxField
                key={district.id}
                label={district.name}
                checked={checked}
                disabled={full && !checked}
                onChange={() => set('areas', withDistrict(form.areas, city.id, district.id, !checked))}
              />
            )
          })}
        </fieldset>
      )}
    </>
  )
}
