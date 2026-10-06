import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { errorMessage } from '@/api/errors'
import PageHeader from '@/components/PageHeader/PageHeader'
import QueryState from '@/components/QueryState/QueryState'
import Alert from '@/components/ui/Alert/Alert'
import Button from '@/components/ui/Button/Button'
import EmptyState from '@/components/ui/EmptyState/EmptyState'
import Icon from '@/components/ui/Icon/Icon'
import RequestsTabs from '@/features/requests/RequestsTabs'
import { useLocalizedPath } from '@/i18n/hooks'
import PriceListEditor from './PriceListEditor'
import { usePriceList } from './usePriceList'
import styles from './prices.module.scss'

/**
 * "My prices": the partner's labour prices for the work they offer. Customers will see them on the profile and in
 * estimates; the market range next to each item helps pick a fair price.
 */
export default function PricesPage() {
  const { t } = useTranslation()
  const path = useLocalizedPath()
  const list = usePriceList()
  const [saved, setSaved] = useState(false)

  const save = async () => {
    setSaved(false)
    if (await list.save()) setSaved(true)
  }

  const saveButton = (
    <Button variant="accent" icon={<Icon name="check" />} onClick={save} loading={list.saving.isLoading} disabled={!list.changed}>
      {t('partner.prices.save')}
    </Button>
  )

  return (
    <div className={styles['prices-page']}>
      <PageHeader title={t('partner.prices.title')} subtitle={t('partner.prices.subtitle')} actions={list.items.length > 0 && saveButton} />
      <RequestsTabs />
      <QueryState
        query={list.query}
        notFound={
          <EmptyState
            icon="account"
            title={t('partner.prices.noProfileTitle')}
            description={t('partner.prices.noProfileText')}
            action={<Button to={path('/partner')}>{t('partner.prices.createProfile')}</Button>}
          />
        }
      >
        {() => (
          <>
            <Alert tone="info" title={t('partner.prices.howTitle')}>
              <p>{t('partner.prices.howText')}</p>
            </Alert>
            {saved && !list.changed && <Alert tone="success" title={t('partner.prices.saved')} />}
            {list.hasErrors && <Alert tone="danger" title={t('partner.prices.errors.fix')} />}
            {list.saving.error && <Alert tone="danger" title={errorMessage(t, list.saving.error)} />}
            <PriceListEditor
              items={list.items}
              draft={list.draft}
              errors={list.errors}
              onChange={(id, patch) => {
                setSaved(false)
                list.change(id, patch)
              }}
              onFillTypical={list.fillTypical}
              servicesLink={path('/partner?step=services')}
            />
            {list.items.length > 0 && <div className={styles['prices-page__footer']}>{saveButton}</div>}
          </>
        )}
      </QueryState>
    </div>
  )
}
