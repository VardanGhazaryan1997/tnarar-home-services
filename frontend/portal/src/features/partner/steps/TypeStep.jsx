import { useTranslation } from 'react-i18next'
import { TextField } from '@/components/ui/Field/Field'
import Icon from '@/components/ui/Icon/Icon'
import { bem } from '@/shared/bem'
import { LIMITS, PARTNER_TYPES } from '../profileForm'
import styles from '../partner.module.scss'

const b = bem(styles)
const TYPE_ICONS = { Specialist: 'tools', Company: 'users', Supplier: 'orders' }

/** Step 1: specialist, company or supplier; the public name; years of experience. */
export default function TypeStep({ form, set, errorFor, typeLocked }) {
  const { t } = useTranslation()
  return (
    <div className={styles['partner-wizard__fields']}>
      <fieldset className={styles['type-choice']}>
        <legend className={styles['type-choice__legend']}>{t('partner.fields.type')}</legend>
        {typeLocked && <p className={styles['type-choice__hint']}>{t('partner.typeLocked')}</p>}
        <div className={styles['type-choice__options']}>
          {PARTNER_TYPES.map((type) => (
            <label key={type} className={b('type-choice__option', { selected: form.type === type, disabled: typeLocked })}>
              <input
                type="radio"
                name="partner-type"
                value={type}
                checked={form.type === type}
                disabled={typeLocked}
                onChange={() => set('type', type)}
                className={styles['type-choice__input']}
              />
              <span className={styles['type-choice__icon']}>
                <Icon name={TYPE_ICONS[type]} size={24} />
              </span>
              <span className={styles['type-choice__title']}>{t(`public.partnerType.${type}`)}</span>
              <span className={styles['type-choice__text']}>{t(`partner.typeText.${type}`)}</span>
            </label>
          ))}
        </div>
      </fieldset>
      <div className={styles['partner-wizard__row']}>
        <TextField
          label={form.type === 'Specialist' ? t('partner.fields.nameSpecialist') : t('partner.fields.nameCompany')}
          hint={t('partner.nameHint')}
          required
          maxLength={LIMITS.nameMax}
          value={form.displayName}
          onChange={(event) => set('displayName', event.target.value)}
          error={errorFor('displayName')}
        />
        <TextField
          label={t('partner.fields.years')}
          optionalText={t('common.optional')}
          type="number"
          inputMode="numeric"
          min={0}
          max={LIMITS.yearsMax}
          value={form.yearsOfExperience}
          onChange={(event) => set('yearsOfExperience', event.target.value)}
          error={errorFor('yearsOfExperience')}
        />
      </div>
    </div>
  )
}
