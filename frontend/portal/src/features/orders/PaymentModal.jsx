import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { errorMessage, fieldErrors } from '@/api/errors'
import Alert from '@/components/ui/Alert/Alert'
import Button from '@/components/ui/Button/Button'
import { SelectField, TextField } from '@/components/ui/Field/Field'
import Modal from '@/components/ui/Modal/Modal'
import { formatMoney, todayIso } from '@/shared/format'
import { useRecordPaymentMutation } from './ordersApi'
import styles from './orders.module.scss'

const METHODS = ['Cash', 'BankTransfer', 'Card', 'Other']

/**
 * Records a payment made directly between the two sides: the customer says "I paid", the partner "I received".
 * The other side then confirms it. `remaining`: the most that can still be recorded.
 */
export default function PaymentModal({ order, remaining, open, onClose }) {
  const { t, i18n } = useTranslation()
  const empty = { amount: '', method: 'Cash', paidOn: todayIso(), stageId: '', note: '' }
  const [form, setForm] = useState(empty)
  const [tried, setTried] = useState(false)
  const [record, recording] = useRecordPaymentMutation()
  const set = (field) => (event) => setForm((current) => ({ ...current, [field]: event.target.value }))
  const role = order.myRole

  const amount = Number(form.amount)
  const local = {
    amount: !(amount > 0) ? t('orders.pay.errors.amount') : amount > remaining ? t('orders.pay.errors.tooMuch', { max: formatMoney(remaining, i18n.language) }) : null,
    paidOn: !form.paidOn ? t('orders.pay.errors.date') : null,
  }
  const api = fieldErrors(t, recording.error)
  const errorFor = (field) => api[field] ?? (tried ? local[field] || undefined : undefined)
  const general = recording.error && !Object.keys(api).length ? errorMessage(t, recording.error) : null

  const close = () => {
    setForm(empty)
    setTried(false)
    recording.reset()
    onClose()
  }

  const submit = async (event) => {
    event.preventDefault()
    setTried(true)
    if (Object.values(local).some(Boolean)) return
    const result = await record({
      id: order.id,
      amount,
      method: form.method,
      paidOn: form.paidOn,
      stageId: form.stageId || null,
      note: form.note.trim() || null,
    })
    if (!result.error) close()
  }

  return (
    <Modal
      open={open}
      title={t(`orders.pay.title.${role}`)}
      onClose={close}
      footer={
        <>
          <Button variant="secondary" onClick={close}>
            {t('common.back')}
          </Button>
          <Button type="submit" form="payment-form" variant="accent" loading={recording.isLoading}>
            {t('orders.pay.save')}
          </Button>
        </>
      }
    >
      <form id="payment-form" className={styles['change-form']} onSubmit={submit} noValidate>
        <p className={styles['change-form__intro']}>{t(`orders.pay.intro.${role}`)}</p>
        <div className={styles['change-form__row']}>
          <TextField
            label={t('orders.pay.amount')}
            hint={t('orders.pay.remaining', { amount: formatMoney(remaining, i18n.language) })}
            required
            type="number"
            inputMode="numeric"
            min={1}
            max={remaining}
            value={form.amount}
            onChange={set('amount')}
            error={errorFor('amount')}
          />
          <TextField label={t('orders.pay.paidOn')} required type="date" max={todayIso()} value={form.paidOn} onChange={set('paidOn')} error={errorFor('paidOn')} />
        </div>
        <SelectField
          label={t('orders.pay.method')}
          required
          value={form.method}
          onChange={set('method')}
          options={METHODS.map((value) => ({ value, label: t(`orders.pay.methods.${value}`) }))}
        />
        {order.stages.length > 1 && (
          <SelectField
            label={t('orders.pay.stage')}
            optionalText={t('common.optional')}
            placeholder={t('orders.pay.noStage')}
            value={form.stageId}
            onChange={set('stageId')}
            options={order.stages.map((stage) => ({
              value: stage.id,
              label: `${stage.title ?? t(`offers.purpose.${stage.purpose}`)} · ${formatMoney(stage.amount, i18n.language)}`,
            }))}
          />
        )}
        <TextField label={t('orders.pay.note')} optionalText={t('common.optional')} multiline maxLength={500} value={form.note} onChange={set('note')} />
        {general && <Alert tone="danger" title={general} />}
      </form>
    </Modal>
  )
}
