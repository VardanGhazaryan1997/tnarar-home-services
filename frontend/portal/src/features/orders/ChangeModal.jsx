import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { errorMessage, fieldErrors } from '@/api/errors'
import Alert from '@/components/ui/Alert/Alert'
import Button from '@/components/ui/Button/Button'
import { TextField } from '@/components/ui/Field/Field'
import Modal from '@/components/ui/Modal/Modal'
import Segmented from '@/components/ui/Segmented/Segmented'
import { todayIso } from '@/shared/format'
import { useProposeChangeMutation } from './ordersApi'
import styles from './orders.module.scss'

const EMPTY = {
  title: '',
  amount: '',
  description: '',
  newStartDate: '',
  newDurationDays: '',
  newVisitAt: '',
}

/**
 * Proposes a change to the other side: extra work (what and for how much) or a new schedule (start date
 * and duration for work, a new time for a visit). A visit has only the schedule.
 */
export default function ChangeModal({ order, open, onClose }) {
  const { t } = useTranslation()
  const visit = order.kind === 'Visit'
  const [kind, setKind] = useState(visit ? 'Schedule' : 'ExtraWork')
  const [form, setForm] = useState(EMPTY)
  const [tried, setTried] = useState(false)
  const [propose, proposing] = useProposeChangeMutation()
  const set = (field) => (event) => setForm((current) => ({ ...current, [field]: event.target.value }))

  const local = {
    title: kind === 'ExtraWork' && !form.title.trim() && t('orders.change.errors.title'),
    amount: kind === 'ExtraWork' && !(Number(form.amount) > 0) && t('orders.change.errors.amount'),
    newVisitAt: kind === 'Schedule' && visit && !form.newVisitAt && t('orders.change.errors.visitAt'),
    newStartDate: kind === 'Schedule' && !visit && !form.newStartDate && !form.newDurationDays && t('orders.change.errors.schedule'),
  }
  const api = fieldErrors(t, proposing.error)
  const errorFor = (field) => api[field] ?? (tried ? local[field] || undefined : undefined)
  const general = proposing.error && !Object.keys(api).length ? errorMessage(t, proposing.error) : null

  const close = () => {
    setForm(EMPTY)
    setTried(false)
    proposing.reset()
    onClose()
  }

  const submit = async (event) => {
    event.preventDefault()
    setTried(true)
    if (Object.values(local).some(Boolean)) return
    const result = await propose({
      id: order.id,
      kind,
      title: kind === 'ExtraWork' ? form.title.trim() : null,
      amount: kind === 'ExtraWork' ? Number(form.amount) : null,
      description: form.description.trim() || null,
      newStartDate: kind === 'Schedule' && !visit ? form.newStartDate || null : null,
      newDurationDays: kind === 'Schedule' && !visit && form.newDurationDays ? Number(form.newDurationDays) : null,
      newVisitAt: kind === 'Schedule' && visit ? new Date(form.newVisitAt).toISOString() : null,
    })
    if (!result.error) close()
  }

  return (
    <Modal
      open={open}
      title={t('orders.change.proposeTitle')}
      onClose={close}
      footer={
        <>
          <Button variant="secondary" onClick={close}>
            {t('common.back')}
          </Button>
          <Button type="submit" form="order-change-form" variant="accent" loading={proposing.isLoading}>
            {t('orders.change.send')}
          </Button>
        </>
      }
    >
      <form id="order-change-form" className={styles['change-form']} onSubmit={submit} noValidate>
        <p className={styles['change-form__intro']}>{t('orders.change.intro')}</p>
        {!visit && (
          <Segmented
            label={t('orders.change.kindLabel')}
            options={['ExtraWork', 'Schedule'].map((value) => ({
              value,
              label: t(`orders.change.kind.${value}`),
            }))}
            value={kind}
            onChange={(value) => {
              setKind(value)
              setTried(false)
            }}
          />
        )}
        {kind === 'ExtraWork' && (
          <>
            <TextField label={t('orders.change.fields.title')} required maxLength={100} value={form.title} onChange={set('title')} error={errorFor('title')} />
            <TextField
              label={t('orders.change.fields.amount')}
              hint={t('orders.change.amountHint')}
              required
              type="number"
              inputMode="numeric"
              min={1}
              value={form.amount}
              onChange={set('amount')}
              error={errorFor('amount')}
            />
          </>
        )}
        {kind === 'Schedule' && visit && (
          <TextField
            label={t('orders.change.fields.newVisitAt')}
            required
            type="datetime-local"
            value={form.newVisitAt}
            onChange={set('newVisitAt')}
            error={errorFor('newVisitAt')}
          />
        )}
        {kind === 'Schedule' && !visit && (
          <div className={styles['change-form__row']}>
            <TextField
              label={t('orders.change.fields.newStartDate')}
              optionalText={t('common.optional')}
              type="date"
              min={todayIso()}
              value={form.newStartDate}
              onChange={set('newStartDate')}
              error={errorFor('newStartDate')}
            />
            <TextField
              label={t('orders.change.fields.newDurationDays')}
              optionalText={t('common.optional')}
              type="number"
              inputMode="numeric"
              min={1}
              max={365}
              value={form.newDurationDays}
              onChange={set('newDurationDays')}
              error={errorFor('newDurationDays')}
            />
          </div>
        )}
        <TextField
          label={t('orders.change.fields.description')}
          optionalText={t('common.optional')}
          multiline
          maxLength={1000}
          value={form.description}
          onChange={set('description')}
          error={errorFor('description')}
        />
        {general && <Alert tone="danger" title={general} />}
      </form>
    </Modal>
  )
}
