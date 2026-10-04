import { useMemo, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { useNavigate, useSearchParams } from 'react-router'
import { errorMessage, fieldErrors } from '@/api/errors'
import PageHeader from '@/components/PageHeader/PageHeader'
import Alert from '@/components/ui/Alert/Alert'
import Button from '@/components/ui/Button/Button'
import Card from '@/components/ui/Card/Card'
import { SelectField, TextField } from '@/components/ui/Field/Field'
import Icon from '@/components/ui/Icon/Icon'
import { categoryOptions, useCategories, useCities } from '@/features/catalog/catalogApi'
import PhotoPicker from '@/features/files/PhotoPicker'
import { useFileUploads } from '@/features/files/useFileUploads'
import { useLocalizedPath } from '@/i18n/hooks'
import { bem } from '@/shared/bem'
import { todayIso } from '@/shared/format'
import { useCreateRequestMutation } from './requestsApi'
import styles from './requests.module.scss'

const b = bem(styles)
const STEPS = ['what', 'details', 'when']
const DESCRIPTION_MIN = 20
const DESCRIPTION_MAX = 4000

/** Which step shows the field an API error is about. */
const STEP_OF_FIELD = {
  categoryId: 0,
  cityId: 0,
  districtId: 0,
  partnerId: 0,
  description: 1,
  mediaFileIds: 1,
  preferredDate: 2,
  timeNote: 2,
  budgetMin: 2,
  budgetMax: 2,
}

const toNumber = (value) => (value === '' ? null : Number(value))

/**
 * A new request in three short steps (phones show one at a time): what and where; the description and
 * photos; when and the budget. `?partner=<id>&name=<name>` sends it to that partner only (a direct request).
 */
export default function NewRequestPage() {
  const { t } = useTranslation()
  const navigate = useNavigate()
  const path = useLocalizedPath()
  const [params] = useSearchParams()
  const partnerId = params.get('partner')
  const partnerName = params.get('name')
  const categories = useCategories()
  const cities = useCities()
  const uploads = useFileUploads()
  const [createRequest, creating] = useCreateRequestMutation()
  const [step, setStep] = useState(0)
  const [showErrors, setShowErrors] = useState(false)
  const [form, setForm] = useState({
    categoryId: params.get('category') ?? '',
    cityId: '',
    districtId: '',
    description: '',
    preferredDate: '',
    timeNote: '',
    budgetMin: '',
    budgetMax: '',
  })

  const set = (field) => (event) => setForm((current) => ({ ...current, [field]: event.target.value }))
  const districts = useMemo(() => cities.data?.find((city) => city.id === form.cityId)?.districts ?? [], [cities.data, form.cityId])
  const apiErrors = fieldErrors(t, creating.error)

  // Simple checks before moving on; the API checks everything again.
  const localErrors = {
    categoryId: !form.categoryId && t('requests.errors.categoryRequired'),
    cityId: !form.cityId && t('requests.errors.cityRequired'),
    description:
      form.description.trim().length < DESCRIPTION_MIN && t('requests.errors.descriptionShort', { min: DESCRIPTION_MIN }),
    budgetMax:
      form.budgetMin !== '' && form.budgetMax !== '' && Number(form.budgetMax) < Number(form.budgetMin) && t('errors.budget.range_invalid'),
  }
  const errorFor = (field) => apiErrors[field] ?? (showErrors ? localErrors[field] || undefined : undefined)
  const stepValid = [!localErrors.categoryId && !localErrors.cityId, !localErrors.description, !localErrors.budgetMax]

  const next = () => {
    if (!stepValid[step]) return setShowErrors(true)
    setShowErrors(false)
    return setStep(step + 1)
  }

  const submit = async (event) => {
    event.preventDefault()
    if (step < STEPS.length - 1) return next()
    if (!stepValid.every(Boolean)) {
      setShowErrors(true)
      return setStep(stepValid.findIndex((valid) => !valid))
    }

    const result = await createRequest({
      kind: partnerId ? 'Direct' : 'Open',
      partnerId: partnerId || null,
      categoryId: form.categoryId,
      cityId: form.cityId,
      districtId: form.districtId || null,
      description: form.description.trim(),
      preferredDate: form.preferredDate || null,
      timeNote: form.timeNote.trim() || null,
      budgetMin: toNumber(form.budgetMin),
      budgetMax: toNumber(form.budgetMax),
      mediaFileIds: uploads.fileIds,
    })
    if (result.data) return navigate(path(`/requests/${result.data.id}`), { replace: true, state: { created: true } })

    const fields = Object.keys(fieldErrors(t, result.error))
    if (fields.length) setStep(Math.min(...fields.map((field) => STEP_OF_FIELD[field] ?? STEPS.length - 1)))
    return undefined
  }

  const generalError = creating.error && !Object.keys(apiErrors).length ? errorMessage(t, creating.error) : null

  return (
    <div>
      <PageHeader
        title={partnerId ? t('requests.newDirectTitle') : t('requests.newTitle')}
        subtitle={t('requests.newSubtitle')}
        back={{ to: path('/requests'), label: t('requests.mineTitle') }}
      />
      <form className={styles['request-form']} onSubmit={submit} noValidate>
        <div>
          <ol className={styles['request-form__steps']} aria-hidden="true">
            {STEPS.map((name, index) => (
              <li key={name} className={b('request-form__step', { done: index <= step })} />
            ))}
          </ol>
          <p className={styles['request-form__step-label']}>
            {t('requests.stepOf', { step: step + 1, total: STEPS.length })} — {t(`requests.steps.${STEPS[step]}`)}
          </p>
        </div>

        {partnerId && (
          <p className={styles['request-form__to']}>
            <Icon name="account" />
            {t('requests.directTo', { name: partnerName ?? '' })}
          </p>
        )}

        <Card>
          {step === 0 && (
            <div className={styles['request-form__fields']}>
              <SelectField
                label={t('requests.fields.category')}
                required
                placeholder={t('requests.choose')}
                options={categoryOptions(categories.data)}
                value={form.categoryId}
                onChange={set('categoryId')}
                error={errorFor('categoryId')}
              />
              <div className={styles['request-form__row']}>
                <SelectField
                  label={t('requests.fields.city')}
                  required
                  placeholder={t('requests.choose')}
                  options={(cities.data ?? []).map((city) => ({ value: city.id, label: city.name }))}
                  value={form.cityId}
                  onChange={(event) => setForm((current) => ({ ...current, cityId: event.target.value, districtId: '' }))}
                  error={errorFor('cityId')}
                />
                <SelectField
                  label={t('requests.fields.district')}
                  optionalText={t('common.optional')}
                  placeholder={t('requests.anyDistrict')}
                  options={districts.map((district) => ({ value: district.id, label: district.name }))}
                  value={form.districtId}
                  onChange={set('districtId')}
                  disabled={!districts.length}
                  error={errorFor('districtId')}
                />
              </div>
              {errorFor('partnerId') && <Alert tone="danger" title={errorFor('partnerId')} />}
            </div>
          )}

          {step === 1 && (
            <div className={styles['request-form__fields']}>
              <TextField
                label={t('requests.fields.description')}
                hint={t('requests.descriptionHint', { n: form.description.trim().length, max: DESCRIPTION_MAX })}
                multiline
                rows={6}
                required
                maxLength={DESCRIPTION_MAX}
                value={form.description}
                onChange={set('description')}
                error={errorFor('description')}
              />
              <PhotoPicker uploads={uploads} label={t('requests.fields.photos')} hint={t('requests.photosHint')} />
              {errorFor('mediaFileIds') && <Alert tone="danger" title={errorFor('mediaFileIds')} />}
            </div>
          )}

          {step === 2 && (
            <div className={styles['request-form__fields']}>
              <div className={styles['request-form__row']}>
                <TextField
                  label={t('requests.fields.preferredDate')}
                  optionalText={t('common.optional')}
                  type="date"
                  min={todayIso()}
                  value={form.preferredDate}
                  onChange={set('preferredDate')}
                  error={errorFor('preferredDate')}
                />
                <TextField
                  label={t('requests.fields.timeNote')}
                  optionalText={t('common.optional')}
                  placeholder={t('requests.timeNotePlaceholder')}
                  maxLength={200}
                  value={form.timeNote}
                  onChange={set('timeNote')}
                  error={errorFor('timeNote')}
                />
              </div>
              <div className={styles['request-form__row']}>
                <TextField
                  label={t('requests.fields.budgetMin')}
                  optionalText={t('common.optional')}
                  type="number"
                  inputMode="numeric"
                  min={0}
                  value={form.budgetMin}
                  onChange={set('budgetMin')}
                  error={errorFor('budgetMin')}
                />
                <TextField
                  label={t('requests.fields.budgetMax')}
                  optionalText={t('common.optional')}
                  type="number"
                  inputMode="numeric"
                  min={0}
                  value={form.budgetMax}
                  onChange={set('budgetMax')}
                  error={errorFor('budgetMax')}
                />
              </div>
              <p className={styles['request-form__step-label']}>{t('requests.budgetPrivate')}</p>
            </div>
          )}
        </Card>

        {generalError && <Alert tone="danger" title={generalError} />}

        <div className={styles['request-form__nav']}>
          {step > 0 ? (
            <Button variant="secondary" icon={<Icon name="chevronLeft" />} onClick={() => setStep(step - 1)}>
              {t('common.back')}
            </Button>
          ) : (
            <span />
          )}
          {step < STEPS.length - 1 ? (
            <Button type="submit">{t('common.next')}</Button>
          ) : (
            <Button type="submit" variant="accent" loading={creating.isLoading} disabled={uploads.busy} icon={<Icon name="send" />}>
              {t('requests.submit')}
            </Button>
          )}
        </div>
      </form>
    </div>
  )
}
