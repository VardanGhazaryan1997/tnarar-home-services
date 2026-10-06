import { useRef, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { useSearchParams } from 'react-router'
import { errorMessage, fieldErrors } from '@/api/errors'
import Alert from '@/components/ui/Alert/Alert'
import Button from '@/components/ui/Button/Button'
import Card from '@/components/ui/Card/Card'
import Icon from '@/components/ui/Icon/Icon'
import { bem } from '@/shared/bem'
import { useSavePartnerProfileMutation, useSubmitPartnerProfileMutation } from './partnerProfileApi'
import { fromProfile, STEP_OF_FIELD, STEPS, stepErrors, toPayload } from './profileForm'
import AboutStep from './steps/AboutStep'
import AreasStep from './steps/AreasStep'
import MediaStep from './steps/MediaStep'
import PricesStep from './steps/PricesStep'
import ReviewStep from './steps/ReviewStep'
import ServicesStep from './steps/ServicesStep'
import TypeStep from './steps/TypeStep'
import styles from './partner.module.scss'

const b = bem(styles)
const SAVED_STEPS = ['type', 'services', 'areas', 'about']

/**
 * The profile in seven steps. The first four save the profile when you continue (the first save creates it);
 * work examples attach as they upload; prices (optional) save when you continue; the last step sends the profile
 * for review. The step is in the
 * address (?step=areas), so a refresh or the back button keeps your place. Mount it with a `key` per profile.
 */
export default function PartnerWizard({ profile, categories, cities, regions = [], onDone }) {
  const { t } = useTranslation()
  const [params, setParams] = useSearchParams()
  const [form, setForm] = useState(() => fromProfile(profile))
  const [showErrors, setShowErrors] = useState(false)
  const [save, saving] = useSavePartnerProfileMutation()
  const [submit, submitting] = useSubmitPartnerProfileMutation()
  const prices = useRef(null)
  const [savingPrices, setSavingPrices] = useState(false)
  const requested = params.get('step')
  // Until the profile exists only the first step is open.
  const step = profile && STEPS.includes(requested) ? requested : 'type'
  const index = STEPS.indexOf(step)
  const approved = profile?.status === 'Approved'

  const set = (field, value) => setForm((current) => ({ ...current, [field]: value }))
  const apiErrors = fieldErrors(t, saving.error)
  const localErrors = stepErrors(step, form)
  const errorFor = (field) => apiErrors[field] ?? (showErrors && localErrors[field] ? t(localErrors[field]) : undefined)

  const goTo = (next) => {
    setShowErrors(false)
    saving.reset()
    setParams(next === 'type' ? {} : { step: next })
    window.scrollTo?.({ top: 0 })
  }

  const next = async () => {
    if (Object.keys(localErrors).length) return setShowErrors(true)
    if (step === 'work' && profile.workExamples.length === 0) return setShowErrors(true)
    if (step === 'prices' && prices.current) {
      setSavingPrices(true)
      const ok = await prices.current.save()
      setSavingPrices(false)
      if (!ok) return undefined
    }
    if (SAVED_STEPS.includes(step)) {
      const result = await save(toPayload(form))
      if (result.error) {
        const fields = Object.keys(fieldErrors(t, result.error))
        const target = fields.map((field) => STEP_OF_FIELD[field]).find(Boolean)
        if (target && target !== step) goTo(target)
        return undefined
      }
    }
    return goTo(STEPS[index + 1])
  }

  const send = async () => {
    const result = await submit()
    if (!result.error) onDone('submitted')
  }

  const generalError =
    (saving.error && !Object.keys(apiErrors).length && errorMessage(t, saving.error)) || (submitting.error && errorMessage(t, submitting.error))

  return (
    <div className={styles['partner-wizard']}>
      <nav className={styles['partner-wizard__steps']} aria-label={t('partner.stepsLabel')}>
        <p className={styles['partner-wizard__step-of']}>{t('partner.stepOf', { step: index + 1, total: STEPS.length })}</p>
        <ol className={styles['partner-wizard__step-list']}>
          {STEPS.map((name, position) => (
            <li key={name}>
              <button
                type="button"
                className={b('partner-wizard__step', { current: name === step, done: position < index })}
                aria-current={name === step ? 'step' : undefined}
                disabled={!profile}
                onClick={() => goTo(name)}
              >
                <span className={styles['partner-wizard__step-number']}>{position < index ? <Icon name="check" size={14} /> : position + 1}</span>
                <span className={styles['partner-wizard__step-name']}>{t(`partner.steps.${name}`)}</span>
              </button>
            </li>
          ))}
        </ol>
      </nav>

      <div className={styles['partner-wizard__body']}>
        {profile?.status === 'NeedsChanges' && (
          <Alert tone="warning" title={t('partner.needsChangesTitle')}>
            {profile.reviewComment && <p className={styles['partner-wizard__comment']}>{profile.reviewComment}</p>}
          </Alert>
        )}

        <Card title={t(`partner.steps.${step}`)}>
          {step === 'type' && <TypeStep form={form} set={set} errorFor={errorFor} typeLocked={approved} />}
          {step === 'services' && <ServicesStep form={form} set={set} categories={categories} errorFor={errorFor} />}
          {step === 'areas' && <AreasStep form={form} set={set} cities={cities} regions={regions} errorFor={errorFor} />}
          {step === 'about' && <AboutStep form={form} set={set} errorFor={errorFor} />}
          {step === 'work' && <MediaStep profile={profile} showErrors={showErrors} />}
          {step === 'prices' && <PricesStep ref={prices} goTo={goTo} />}
          {step === 'review' && <ReviewStep profile={profile} categories={categories} cities={cities} regions={regions} goTo={goTo} />}
        </Card>

        {generalError && <Alert tone="danger" title={generalError} />}

        <div className={styles['partner-wizard__nav']}>
          {index > 0 ? (
            <Button variant="secondary" icon={<Icon name="chevronLeft" />} onClick={() => goTo(STEPS[index - 1])} aria-label={t('common.back')} />
          ) : (
            <span />
          )}
          {step !== 'review' && (
            <Button onClick={next} loading={saving.isLoading || savingPrices}>
              {SAVED_STEPS.includes(step) ? t('partner.saveContinue') : t('common.next')}
            </Button>
          )}
          {step === 'review' && approved && (
            <Button variant="accent" icon={<Icon name="check" />} onClick={() => onDone('saved')}>
              {t('partner.done')}
            </Button>
          )}
          {step === 'review' && !approved && (
            <Button variant="accent" icon={<Icon name="send" />} onClick={send} loading={submitting.isLoading} disabled={!profile.canSubmit}>
              {profile.status === 'NeedsChanges' ? t('partner.resubmit') : t('partner.submit')}
            </Button>
          )}
        </div>
      </div>
    </div>
  )
}
