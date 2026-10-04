import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { useNavigate, useParams } from 'react-router'
import { errorMessage, fieldErrors } from '@/api/errors'
import PageHeader from '@/components/PageHeader/PageHeader'
import QueryState from '@/components/QueryState/QueryState'
import Alert from '@/components/ui/Alert/Alert'
import Button from '@/components/ui/Button/Button'
import Card from '@/components/ui/Card/Card'
import { CheckboxField, SelectField, TextField } from '@/components/ui/Field/Field'
import Icon from '@/components/ui/Icon/Icon'
import Segmented from '@/components/ui/Segmented/Segmented'
import { placeText } from '@/features/requests/place'
import { useGetInboxRequestQuery } from '@/features/requests/requestsApi'
import { useLocalizedPath } from '@/i18n/hooks'
import { bem } from '@/shared/bem'
import { formatMoney, todayIso } from '@/shared/format'
import { useSendOfferMutation } from './offersApi'
import styles from './offers.module.scss'

const b = bem(styles)
const SUMMARY_MIN = 10
const PURPOSES = ['Deposit', 'Stage', 'Final']
const VALID_DAYS = ['3', '7', '14', '30']

let nextKey = 0
const key = () => `row-${(nextKey += 1)}`
const toInt = (value) => (value === '' || value == null ? null : Math.round(Number(value)))

/**
 * The partner's offer on a received request. A work offer: what's included and excluded, the price,
 * materials, dates and an optional payment plan whose stages add up to the price. A visit offer: a time
 * and a fee (0 = free) for an assessment visit before the work offer.
 */
export default function OfferBuilderPage() {
  const { t, i18n } = useTranslation()
  const { id } = useParams()
  const navigate = useNavigate()
  const path = useLocalizedPath()
  const request = useGetInboxRequestQuery(id)
  const [sendOffer, sending] = useSendOfferMutation()
  const [kind, setKind] = useState('Work')
  const [form, setForm] = useState({ summary: '', price: '', materialsIncluded: false, materialsNote: '', startDate: '', durationDays: '', visitAt: '', validDays: '7' })
  const [lines, setLines] = useState([])
  const [stages, setStages] = useState([])
  const [showErrors, setShowErrors] = useState(false)

  const set = (field) => (event) =>
    setForm((current) => ({ ...current, [field]: event.target.type === 'checkbox' ? event.target.checked : event.target.value }))
  const editRow = (setRows, rowKey, changes) => setRows((rows) => rows.map((row) => (row.key === rowKey ? { ...row, ...changes } : row)))
  const removeRow = (setRows, rowKey) => setRows((rows) => rows.filter((row) => row.key !== rowKey))

  const price = toInt(form.price) ?? 0
  const stagesTotal = stages.reduce((sum, stage) => sum + (toInt(stage.amount) ?? 0), 0)
  const apiErrors = fieldErrors(t, sending.error)
  const local = {
    summary: form.summary.trim().length < SUMMARY_MIN && t('offers.errors.summaryShort', { min: SUMMARY_MIN }),
    price: (kind === 'Work' ? price < 1 : form.price === '' || price < 0) && t('offers.errors.priceRequired'),
    visitAt: kind === 'Visit' && Number.isNaN(Date.parse(form.visitAt)) && t('offers.errors.visitAtRequired'),
    stages: kind === 'Work' && stages.length > 0 && stagesTotal !== price && t('errors.stages.sum_mismatch'),
    lines: kind === 'Work' && lines.some((line) => !line.title.trim()) && t('errors.lines.invalid'),
  }
  const errorFor = (field) => apiErrors[field] ?? (showErrors ? local[field] || undefined : undefined)

  const submit = async (event) => {
    event.preventDefault()
    if (Object.values(local).some(Boolean)) return setShowErrors(true)

    const work = kind === 'Work'
    const result = await sendOffer({
      requestId: id,
      kind,
      summary: form.summary.trim(),
      lines: work ? lines.map((line) => ({ title: line.title.trim(), included: line.included })) : [],
      price,
      materialsIncluded: work && form.materialsIncluded,
      materialsNote: (work && form.materialsNote.trim()) || null,
      startDate: (work && form.startDate) || null,
      durationDays: work ? toInt(form.durationDays) : null,
      visitAt: work ? null : new Date(form.visitAt).toISOString(),
      stages: work ? stages.map((stage) => ({ title: stage.title.trim() || null, purpose: stage.purpose, amount: toInt(stage.amount) ?? 0 })) : [],
      validDays: Number(form.validDays),
    })
    if (result.data) navigate(path(`/inbox/${id}`), { state: { offerSent: true } })
    return undefined
  }

  const generalError = sending.error && !Object.keys(apiErrors).length ? errorMessage(t, sending.error) : null

  return (
    <QueryState query={request}>
      {(data) => (
        <div>
          <PageHeader title={t('offers.newTitle')} subtitle={placeText(data.place, t)} back={{ to: path(`/inbox/${id}`), label: t('offers.backToRequest') }} />
          <form className={styles['offer-builder']} onSubmit={submit} noValidate>
            <p className={styles['offer-builder__context']}>{data.description}</p>
            <Segmented
              label={t('offers.kindLabel')}
              options={['Work', 'Visit'].map((value) => ({ value, label: t(`offers.kindChoice.${value}`) }))}
              value={kind}
              onChange={setKind}
            />

            <Card title={kind === 'Work' ? t('offers.sections.scope') : t('offers.sections.visit')}>
              <div className={styles['offer-builder__fields']}>
                <TextField
                  label={kind === 'Work' ? t('offers.fields.summary') : t('offers.fields.visitSummary')}
                  hint={kind === 'Work' ? t('offers.summaryHint') : t('offers.visitSummaryHint')}
                  multiline
                  required
                  maxLength={4000}
                  value={form.summary}
                  onChange={set('summary')}
                  error={errorFor('summary')}
                />

                {kind === 'Work' && (
                  <div className={styles['offer-builder__fields']}>
                    <p className={styles['offer-builder__group-title']}>{t('offers.fields.lines')}</p>
                    {lines.length > 0 && (
                      <ul className={styles['offer-builder__editor']}>
                        {lines.map((line, index) => (
                          <li key={line.key} className={b('offer-builder__editor-row', { line: true })}>
                            <TextField
                              label={t('offers.fields.lineTitle', { n: index + 1 })}
                              maxLength={200}
                              value={line.title}
                              onChange={(event) => editRow(setLines, line.key, { title: event.target.value })}
                            />
                            <SelectField
                              label={t('offers.fields.lineKind', { n: index + 1 })}
                              options={[
                                { value: 'in', label: t('offers.lineIncluded') },
                                { value: 'out', label: t('offers.lineExcluded') },
                              ]}
                              value={line.included ? 'in' : 'out'}
                              onChange={(event) => editRow(setLines, line.key, { included: event.target.value === 'in' })}
                            />
                            <Button variant="ghost" icon={<Icon name="trash" />} aria-label={t('offers.removeLine', { n: index + 1 })} onClick={() => removeRow(setLines, line.key)} />
                          </li>
                        ))}
                      </ul>
                    )}
                    {errorFor('lines') && <Alert tone="danger" title={errorFor('lines')} />}
                    <div className={styles['offer-builder__actions']}>
                      <Button variant="secondary" size="sm" icon={<Icon name="plus" />} onClick={() => setLines((rows) => [...rows, { key: key(), title: '', included: true }])}>
                        {t('offers.addIncluded')}
                      </Button>
                      <Button variant="secondary" size="sm" icon={<Icon name="plus" />} onClick={() => setLines((rows) => [...rows, { key: key(), title: '', included: false }])}>
                        {t('offers.addExcluded')}
                      </Button>
                    </div>
                    <CheckboxField label={t('offers.fields.materialsIncluded')} checked={form.materialsIncluded} onChange={set('materialsIncluded')} />
                    <TextField
                      label={t('offers.fields.materialsNote')}
                      optionalText={t('common.optional')}
                      maxLength={1000}
                      value={form.materialsNote}
                      onChange={set('materialsNote')}
                      error={errorFor('materialsNote')}
                    />
                  </div>
                )}
              </div>
            </Card>

            <Card title={t('offers.sections.priceAndTime')}>
              <div className={styles['offer-builder__fields']}>
                <div className={styles['offer-builder__row']}>
                  <TextField
                    label={kind === 'Work' ? t('offers.fields.price') : t('offers.fields.fee')}
                    hint={kind === 'Visit' ? t('offers.feeHint') : undefined}
                    type="number"
                    inputMode="numeric"
                    min={0}
                    required
                    value={form.price}
                    onChange={set('price')}
                    error={errorFor('price')}
                  />
                  {kind === 'Visit' ? (
                    <TextField
                      label={t('offers.fields.visitAt')}
                      type="datetime-local"
                      required
                      min={`${todayIso()}T00:00`}
                      value={form.visitAt}
                      onChange={set('visitAt')}
                      error={errorFor('visitAt')}
                    />
                  ) : (
                    <TextField
                      label={t('offers.fields.durationDays')}
                      optionalText={t('common.optional')}
                      type="number"
                      inputMode="numeric"
                      min={1}
                      value={form.durationDays}
                      onChange={set('durationDays')}
                      error={errorFor('durationDays')}
                    />
                  )}
                </div>
                {kind === 'Work' && (
                  <TextField
                    label={t('offers.fields.startDate')}
                    optionalText={t('common.optional')}
                    type="date"
                    min={todayIso()}
                    value={form.startDate}
                    onChange={set('startDate')}
                    error={errorFor('startDate')}
                  />
                )}
                <SelectField
                  label={t('offers.fields.validDays')}
                  options={VALID_DAYS.map((value) => ({ value, label: t('offers.days', { n: Number(value) }) }))}
                  value={form.validDays}
                  onChange={set('validDays')}
                  error={errorFor('validDays')}
                />
              </div>
            </Card>

            {kind === 'Work' && (
              <Card title={t('offers.sections.payments')}>
                <div className={styles['offer-builder__fields']}>
                  <p className={styles['offer-builder__context']}>{t('offers.paymentsHint')}</p>
                  {stages.length > 0 && (
                    <ul className={styles['offer-builder__editor']}>
                      {stages.map((stage, index) => (
                        <li key={stage.key} className={styles['offer-builder__editor-row']}>
                          <TextField
                            label={t('offers.fields.stageTitle', { n: index + 1 })}
                            optionalText={t('common.optional')}
                            maxLength={100}
                            value={stage.title}
                            onChange={(event) => editRow(setStages, stage.key, { title: event.target.value })}
                          />
                          <SelectField
                            label={t('offers.fields.stagePurpose', { n: index + 1 })}
                            options={PURPOSES.map((value) => ({ value, label: t(`offers.purpose.${value}`) }))}
                            value={stage.purpose}
                            onChange={(event) => editRow(setStages, stage.key, { purpose: event.target.value })}
                          />
                          <TextField
                            label={t('offers.fields.stageAmount', { n: index + 1 })}
                            type="number"
                            inputMode="numeric"
                            min={1}
                            value={stage.amount}
                            onChange={(event) => editRow(setStages, stage.key, { amount: event.target.value })}
                          />
                          <Button variant="ghost" icon={<Icon name="trash" />} aria-label={t('offers.removeStage', { n: index + 1 })} onClick={() => removeRow(setStages, stage.key)} />
                        </li>
                      ))}
                    </ul>
                  )}
                  {stages.length > 0 && (
                    <p className={b('offer-builder__sum', { wrong: stagesTotal !== price })}>
                      {t('offers.stagesTotal', { total: formatMoney(stagesTotal, i18n.language), price: formatMoney(price, i18n.language) })}
                    </p>
                  )}
                  {errorFor('stages') && <Alert tone="danger" title={errorFor('stages')} />}
                  <div className={styles['offer-builder__actions']}>
                    <Button
                      variant="secondary"
                      size="sm"
                      icon={<Icon name="plus" />}
                      onClick={() => setStages((rows) => [...rows, { key: key(), title: '', purpose: rows.length === 0 ? 'Deposit' : 'Final', amount: '' }])}
                    >
                      {t('offers.addStage')}
                    </Button>
                  </div>
                </div>
              </Card>
            )}

            {generalError && <Alert tone="danger" title={generalError} />}
            <div className={styles['offer-builder__actions']}>
              <Button type="submit" variant="accent" size="lg" loading={sending.isLoading} icon={<Icon name="send" />}>
                {t('offers.send')}
              </Button>
            </div>
          </form>
        </div>
      )}
    </QueryState>
  )
}
