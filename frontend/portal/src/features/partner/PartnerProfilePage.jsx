import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { useSearchParams } from 'react-router'
import PageHeader from '@/components/PageHeader/PageHeader'
import QueryState from '@/components/QueryState/QueryState'
import { useCategories, useCities } from '@/features/catalog/catalogApi'
import { useLocalizedPath } from '@/i18n/hooks'
import { useGetMyPartnerProfileQuery } from './partnerProfileApi'
import PartnerWizard from './PartnerWizard'
import ProfileStatus from './ProfileStatus'

const EDITABLE = ['Draft', 'NeedsChanges']

/**
 * The partner profile (onboarding): no profile yet or a draft → the wizard; submitted → where it stands.
 * An approved profile opens as its status, with Edit going back into the wizard.
 */
export default function PartnerProfilePage() {
  const { t } = useTranslation()
  const path = useLocalizedPath()
  const [, setParams] = useSearchParams()
  const query = useGetMyPartnerProfileQuery()
  const categories = useCategories()
  const cities = useCities()
  const [editing, setEditing] = useState(false)
  const [notice, setNotice] = useState(null)
  const missing = query.error?.status === 404
  // A missing profile is not an error here: the user is about to create it.
  const state = missing ? { ...query, isError: false, data: null } : query

  return (
    <div>
      <title>{t('partner.documentTitle')}</title>
      <PageHeader title={t('partner.title')} subtitle={t('partner.subtitle')} back={{ to: path('/account'), label: t('account.title') }} />
      <QueryState query={state}>
        {(profile) => {
          if (!categories.data || !cities.data) return <QueryState query={categories.data ? cities : categories}>{() => null}</QueryState>
          const wizard = !profile || EDITABLE.includes(profile.status) || (profile.status === 'Approved' && editing)
          return wizard ? (
            <PartnerWizard
              key={profile?.id ?? 'new'}
              profile={profile}
              categories={categories.data}
              cities={cities.data}
              onDone={(result) => {
                setEditing(false)
                setNotice(result)
                setParams({})
              }}
            />
          ) : (
            <ProfileStatus profile={profile} categories={categories.data} cities={cities.data} notice={notice} onEdit={() => setEditing(true)} />
          )
        }}
      </QueryState>
    </div>
  )
}
