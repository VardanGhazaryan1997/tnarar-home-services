import { useTranslation } from 'react-i18next'
import { Link } from 'react-router'
import Alert from '@/components/ui/Alert/Alert'
import { useLocalizedPath } from '@/i18n/hooks'
import { formatDate } from '@/shared/format'
import { useGetCommissionSummaryQuery } from './commissionsApi'

/** In the inbox: says why no new requests arrive while the partner is paused for an unpaid statement. */
export default function DebtPauseNotice() {
  const { t, i18n } = useTranslation()
  const path = useLocalizedPath()
  const { data } = useGetCommissionSummaryQuery()
  if (!data?.pausedSince) return null

  return (
    <Alert tone="danger" title={t('commissions.paused.title')}>
      {t('commissions.paused.text', { date: formatDate(data.pausedSince, i18n.language), days: data.pauseAfterOverdueDays })}{' '}
      <Link to={path('/commissions')}>{t('commissions.paused.open')}</Link>
    </Alert>
  )
}
