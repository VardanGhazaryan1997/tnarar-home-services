import { useImperativeHandle } from 'react'
import { useTranslation } from 'react-i18next'
import { errorMessage } from '@/api/errors'
import QueryState from '@/components/QueryState/QueryState'
import Alert from '@/components/ui/Alert/Alert'
import Button from '@/components/ui/Button/Button'
import PriceListEditor from '../prices/PriceListEditor'
import { usePriceList } from '../prices/usePriceList'
import styles from '../partner.module.scss'

/**
 * Step 6 (optional): prices for the work the partner offers. The wizard calls `ref.current.save()` when the partner
 * continues; it resolves to false when a price needs fixing. Empty rows are fine, so the step can be skipped.
 */
export default function PricesStep({ ref, goTo }) {
  const { t } = useTranslation()
  const list = usePriceList()

  useImperativeHandle(ref, () => ({ save: list.save }), [list.save])

  return (
    <div className={styles['partner-wizard__fields']}>
      <p className={styles['partner-wizard__intro']}>{t('partner.prices.stepHint')}</p>
      <QueryState query={list.query}>
        {() => (
          <>
            {list.hasErrors && <Alert tone="danger" title={t('partner.prices.errors.fix')} />}
            {list.saving.error && <Alert tone="danger" title={errorMessage(t, list.saving.error)} />}
            <PriceListEditor
              items={list.items}
              draft={list.draft}
              errors={list.errors}
              onChange={list.change}
              onFillTypical={list.fillTypical}
            />
            {list.items.length === 0 && (
              <div>
                <Button variant="secondary" onClick={() => goTo('services')}>
                  {t('partner.prices.chooseServices')}
                </Button>
              </div>
            )}
          </>
        )}
      </QueryState>
    </div>
  )
}
