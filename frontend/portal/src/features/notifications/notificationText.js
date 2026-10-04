import { formatDate, formatMoney } from '@/shared/format'

/**
 * A notification's text in the user's language: `notifications.types.<Type>` with its values. Amounts are
 * formatted; `by` (who acted) becomes the party's name in words; a missing name falls back to the party.
 */
export function notificationText(t, notification, lng) {
  const values = { ...notification.params }
  const party = values.by ? t(`orders.party.${values.by}`) : ''
  values.name = values.name || party
  for (const key of ['amount', 'price', 'outstanding']) {
    if (values[key] != null) values[key] = formatMoney(Number(values[key]), lng)
  }
  if (values.dueOn) values.dueOn = formatDate(values.dueOn, lng)
  if (values.status) values.status = t(`notifications.status.${values.status}`, { defaultValue: values.status })
  if (values.kind) values.kind = t(`notifications.kind.${values.kind}`, { defaultValue: values.kind })
  const key = notification.type === 'OrderCompleted' && values.auto === 'true' ? 'OrderAutoCompleted' : notification.type
  return t(`notifications.types.${key}`, { ...values, defaultValue: t('notifications.types.Other') })
}

const ICONS = {
  RequestReceived: 'inbox',
  OfferReceived: 'send',
  OfferAccepted: 'check',
  OfferRejected: 'close',
  OrderCancelled: 'close',
  PaymentRecorded: 'money',
  PaymentAnswered: 'money',
  PaymentResolved: 'money',
  ReviewReceived: 'star',
  ReviewReplied: 'star',
  PartnerApproved: 'shield',
  PartnerNeedsChanges: 'edit',
  PartnerRejected: 'shield',
  ChangeProposed: 'edit',
  ChangeAnswered: 'edit',
  CommissionStatementIssued: 'money',
  CommissionOverdue: 'money',
  CommissionSettled: 'money',
  PartnerPausedForDebt: 'shield',
  PartnerResumed: 'shield',
}

export const notificationIcon = (type) => ICONS[type] ?? 'orders'
