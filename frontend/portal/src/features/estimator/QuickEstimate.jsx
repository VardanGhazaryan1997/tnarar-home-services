import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { errorMessage } from '@/api/errors'
import { CheckboxField, SelectField, TextField } from '@/components/ui/Field/Field'
import Alert from '@/components/ui/Alert/Alert'
import Button from '@/components/ui/Button/Button'
import Icon from '@/components/ui/Icon/Icon'
import { intlLocale } from '@/i18n/languages'
import { useLocalizedPath } from '@/i18n/hooks'
import { formatMoney } from '@/shared/format'
import { MAX_AREA, parseArea, ROOM_TYPES, useQuickEstimateMutation } from './estimatorApi'
import styles from './estimator.module.scss'

/**
 * "How much will it cost?": pick a room and its floor area and get a labour price range for the usual work in such a
 * room, from market prices. A first idea only: the final price comes after a specialist sees the place.
 */
export default function QuickEstimate() {
  const { t, i18n } = useTranslation()
  const path = useLocalizedPath()
  const [roomType, setRoomType] = useState('Bedroom')
  const [area, setArea] = useState('')
  const [oldBuilding, setOldBuilding] = useState(false)
  const [areaError, setAreaError] = useState(null)
  const [estimate, result] = useQuickEstimateMutation()
  const lng = i18n.language
  const quantity = (value) => new Intl.NumberFormat(intlLocale(lng), { maximumFractionDigits: 2 }).format(value)

  const submit = (event) => {
    event.preventDefault()
    const value = parseArea(area)
    if (Number.isNaN(value) || value < 1 || value > MAX_AREA) {
      setAreaError(t('estimator.errors.area', { max: MAX_AREA }))
      return
    }

    setAreaError(null)
    estimate({ roomType, area: value, oldBuilding })
  }

  const data = result.data
  const lines = data?.rooms?.[0]?.lines.filter((line) => line.priceTypical != null) ?? []

  return (
    <section className={styles['quick-estimate']} aria-labelledby="quick-estimate-title">
      <div className={styles['quick-estimate__intro']}>
        <h2 id="quick-estimate-title" className={styles['quick-estimate__title']}>
          {t('estimator.quick.title')}
        </h2>
        <p className={styles['quick-estimate__subtitle']}>{t('estimator.quick.subtitle')}</p>
      </div>

      <form className={styles['quick-estimate__form']} onSubmit={submit} noValidate>
        <SelectField
          label={t('estimator.roomType')}
          value={roomType}
          onChange={(event) => setRoomType(event.target.value)}
          options={ROOM_TYPES.map((type) => ({ value: type, label: t(`estimator.roomTypes.${type}`) }))}
        />
        <TextField
          label={t('estimator.area')}
          inputMode="decimal"
          autoComplete="off"
          value={area}
          error={areaError}
          onChange={(event) => setArea(event.target.value)}
        />
        <CheckboxField
          className={styles['quick-estimate__check']}
          label={t('estimator.oldBuilding')}
          checked={oldBuilding}
          onChange={(event) => setOldBuilding(event.target.checked)}
        />
        <Button type="submit" variant="accent" icon={<Icon name="money" />} loading={result.isLoading}>
          {t('estimator.quick.calculate')}
        </Button>
      </form>

      {result.isError && <Alert tone="danger" title={errorMessage(t, result.error)} />}

      {data && !result.isLoading && (
        <div className={styles['quick-estimate__result']} aria-live="polite">
          {data.totalTypical > 0 ? (
            <>
              <p className={styles['quick-estimate__label']}>{t('estimator.quick.resultLabel')}</p>
              <p className={styles['quick-estimate__range']}>
                {formatMoney(data.totalMin, lng)} – {formatMoney(data.totalMax, lng)}
              </p>
              <p className={styles['quick-estimate__typical']}>{t('estimator.quick.usually', { amount: formatMoney(data.totalTypical, lng) })}</p>
              <p className={styles['quick-estimate__note']}>{t('estimator.quick.note')}</p>
              <details className={styles['quick-estimate__details']}>
                <summary>{t('estimator.quick.included', { count: lines.length })}</summary>
                <ul className={styles['quick-estimate__lines']}>
                  {lines.map((line) => (
                    <li key={line.workItemId} className={styles['quick-estimate__line']}>
                      <span>
                        {line.name}
                        <span className={styles['quick-estimate__quantity']}>
                          {' · '}
                          {quantity(line.quantity)} {t(`estimator.units.${line.unit}`)}
                        </span>
                      </span>
                      <span className={styles['quick-estimate__line-price']}>
                        {formatMoney(line.priceMin, lng)} – {formatMoney(line.priceMax, lng)}
                      </span>
                    </li>
                  ))}
                </ul>
              </details>
            </>
          ) : (
            <p>{t('estimator.quick.noPrices')}</p>
          )}
          <div className={styles['quick-estimate__actions']}>
            <Button to={path('/requests/new')} variant="primary">
              {t('estimator.quick.cta')}
            </Button>
            <Button to={path('/estimates/new')} variant="secondary">
              {t('estimator.quick.detailed')}
            </Button>
          </div>
        </div>
      )}
    </section>
  )
}
