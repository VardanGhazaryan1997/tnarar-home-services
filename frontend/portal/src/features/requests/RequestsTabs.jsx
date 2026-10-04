import { useTranslation } from 'react-i18next'
import { useSelector } from 'react-redux'
import SectionTabs from '@/components/SectionTabs/SectionTabs'
import { selectIsPartner } from '@/features/auth/authSlice'
import { useLocalizedPath } from '@/i18n/hooks'

/** For partners, who both ask for work and do it: my requests | inbox | sent offers | commissions. Customers see nothing. */
export default function RequestsTabs() {
  const { t } = useTranslation()
  const isPartner = useSelector(selectIsPartner)
  const path = useLocalizedPath()
  if (!isPartner) return null

  return (
    <SectionTabs
      label={t('requests.tabsLabel')}
      items={[
        { to: path('/requests'), label: t('requests.tabs.mine'), end: true },
        { to: path('/inbox'), label: t('requests.tabs.inbox') },
        { to: path('/offers'), label: t('requests.tabs.offers') },
        { to: path('/commissions'), label: t('requests.tabs.commissions') },
      ]}
    />
  )
}
