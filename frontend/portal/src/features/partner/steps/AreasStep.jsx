import { useTranslation } from 'react-i18next'
import Alert from '@/components/ui/Alert/Alert'
import { CheckboxField } from '@/components/ui/Field/Field'
import { hasArea, LIMITS } from '../profileForm'
import styles from '../partner.module.scss'

/**
 * Step 3: where the partner works. Per city: the whole city, or some districts. Choosing the whole city
 * replaces its districts (the whole city already matches every district).
 */
export default function AreasStep({ form, set, cities, errorFor }) {
  const { t } = useTranslation()
  const full = form.areas.length >= LIMITS.areas

  const toggleCity = (city) => {
    const others = form.areas.filter((area) => area.cityId !== city.id)
    set('areas', hasArea(form.areas, city.id) ? others : [...others, { cityId: city.id, districtId: null }])
  }

  const toggleDistrict = (city, districtId) => {
    const without = form.areas.filter((area) => !(area.cityId === city.id && area.districtId === districtId))
    set('areas', hasArea(form.areas, city.id, districtId) ? without : [...without, { cityId: city.id, districtId }])
  }

  return (
    <div className={styles['partner-wizard__fields']}>
      <p className={styles['partner-wizard__intro']}>{t('partner.areasIntro')}</p>
      <p className={styles['partner-wizard__count']} aria-live="polite">
        {t('partner.selected', { n: form.areas.length, max: LIMITS.areas })}
      </p>
      <ul className={styles['area-picker']}>
        {cities.map((city) => {
          const whole = hasArea(form.areas, city.id)
          return (
            <li key={city.id} className={styles['area-picker__city']}>
              <CheckboxField
                label={city.districts.length ? t('partner.wholeCity', { city: city.name }) : city.name}
                checked={whole}
                disabled={full && !whole}
                onChange={() => toggleCity(city)}
                className={styles['area-picker__whole']}
              />
              {city.districts.length > 0 && !whole && (
                <fieldset className={styles['area-picker__districts']}>
                  <legend className={styles['area-picker__legend']}>{t('partner.orDistricts', { city: city.name })}</legend>
                  {city.districts.map((district) => {
                    const checked = hasArea(form.areas, city.id, district.id)
                    return (
                      <CheckboxField
                        key={district.id}
                        label={district.name}
                        checked={checked}
                        disabled={full && !checked}
                        onChange={() => toggleDistrict(city, district.id)}
                      />
                    )
                  })}
                </fieldset>
              )}
            </li>
          )
        })}
      </ul>
      {errorFor('areas') && <Alert tone="danger" title={errorFor('areas')} />}
    </div>
  )
}
