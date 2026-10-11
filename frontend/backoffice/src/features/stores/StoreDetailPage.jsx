import { ArrowLeftOutlined } from '@ant-design/icons'
import { Alert, Button, Flex, Form, Skeleton, Tabs, Typography } from 'antd'
import { useTranslation } from 'react-i18next'
import { Link, useParams, useSearchParams } from 'react-router'
import { errorMessage } from '@/api/errors'
import BranchesTab from './BranchesTab'
import ProductsTab from './ProductsTab'
import StoreForm from './StoreForm'
import { useGetStoreQuery, useUpdateStoreMutation } from './storesApi'

const TABS = ['details', 'branches', 'products']

/** One store: its details, its branches and its products, in tabs kept in the address (?tab=). */
export default function StoreDetailPage() {
  const { t } = useTranslation()
  const { id } = useParams()
  const [params, setParams] = useSearchParams()
  const { data: store, isLoading, error } = useGetStoreQuery(id)
  const tab = TABS.includes(params.get('tab')) ? params.get('tab') : 'details'

  const changeTab = (key) => {
    const next = new URLSearchParams(params)
    if (key === 'details') next.delete('tab')
    else next.set('tab', key)
    setParams(next, { replace: true })
  }

  if (isLoading) return <Skeleton active />
  if (error || !store) {
    return (
      <>
        <Alert type="error" showIcon title={errorMessage(t, error)} style={{ marginBottom: 16 }} />
        <Link to="/stores">{t('stores.back')}</Link>
      </>
    )
  }

  return (
    <>
      <Flex align="center" gap="small" style={{ marginBottom: 8 }}>
        <Link to="/stores" aria-label={t('stores.back')}>
          <Button type="text" icon={<ArrowLeftOutlined />} tabIndex={-1} />
        </Link>
        <Typography.Title level={1} style={{ margin: 0 }}>
          {store.name}
        </Typography.Title>
      </Flex>
      <Tabs
        activeKey={tab}
        onChange={changeTab}
        items={[
          { key: 'details', label: t('stores.tabs.details'), children: <DetailsTab store={store} /> },
          { key: 'branches', label: `${t('stores.tabs.branches')} (${store.branchCount})`, children: <BranchesTab store={store} /> },
          { key: 'products', label: `${t('stores.tabs.products')} (${store.productCount})`, children: <ProductsTab store={store} /> },
        ]}
      />
    </>
  )
}

function DetailsTab({ store }) {
  const { t } = useTranslation()
  const [form] = Form.useForm()
  const [updateStore, { isLoading }] = useUpdateStoreMutation()

  return (
    <div style={{ maxWidth: 640 }}>
      <StoreForm form={form} store={store} onSave={(body) => updateStore({ id: store.id, ...body }).unwrap()} />
      <Button type="primary" loading={isLoading} onClick={() => form.submit()}>
        {t('catalog.form.save')}
      </Button>
    </div>
  )
}
