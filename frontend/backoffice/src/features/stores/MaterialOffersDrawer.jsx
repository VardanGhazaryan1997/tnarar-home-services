import { Alert, Drawer, Skeleton, Table, Tag, Typography } from 'antd'
import { useTranslation } from 'react-i18next'
import { Link } from 'react-router'
import { errorMessage } from '@/api/errors'
import { formatDate, formatMoney, formatUnitPrice } from '@/i18n/format'
import { GRADE_COLORS } from './stores'
import { useGetMaterialStoreProductsQuery } from './storesApi'

/** The store products compared under a material, cheapest per unit first within each grade. */
export default function MaterialOffersDrawer({ material, materialName, onClose }) {
  const { t, i18n } = useTranslation()
  const language = i18n.resolvedLanguage
  const { data = [], isFetching, error } = useGetMaterialStoreProductsQuery(material?.id, { skip: !material })

  const columns = [
    {
      title: t('stores.offers.store'),
      key: 'store',
      render: (_, offer) => (
        <>
          <Link to={`/stores/${offer.storeId}?tab=products`}>{offer.storeName}</Link>
          {!offer.storeIsActive && (
            <Tag style={{ marginInlineStart: 8 }}>{t('catalog.status.hidden')}</Tag>
          )}
        </>
      ),
    },
    { title: t('stores.product.name'), key: 'product', render: (_, offer) => offer.product.name },
    {
      title: t('stores.product.grade'),
      key: 'grade',
      render: (_, offer) => <Tag color={GRADE_COLORS[offer.product.grade]}>{t(`stores.grades.${offer.product.grade}`)}</Tag>,
    },
    { title: t('stores.product.price'), key: 'price', align: 'right', render: (_, offer) => formatMoney(offer.product.currentPrice, language) },
    {
      title: t('stores.product.unitPrice'),
      key: 'unit',
      align: 'right',
      render: (_, offer) => (offer.product.unitPrice == null ? '—' : formatUnitPrice(offer.product.unitPrice, language)),
    },
    {
      title: t('stores.product.updated'),
      key: 'updated',
      render: (_, offer) => (
        <>
          {formatDate(offer.product.priceUpdatedAt, language)}
          {offer.product.isStale && (
            <Tag color="orange" style={{ marginInlineStart: 8 }}>
              {t('stores.product.stale')}
            </Tag>
          )}
        </>
      ),
    },
  ]

  return (
    <Drawer open={Boolean(material)} size={720} title={t('stores.offers.title', { name: materialName })} onClose={onClose} destroyOnHidden>
      <Typography.Paragraph type="secondary">{t('stores.offers.help')}</Typography.Paragraph>
      {error && <Alert type="error" showIcon title={errorMessage(t, error)} />}
      {isFetching && data.length === 0 ? (
        <Skeleton active />
      ) : (
        <Table
          rowKey={(offer) => offer.product.id}
          columns={columns}
          dataSource={data}
          pagination={false}
          scroll={{ x: 'max-content' }}
          locale={{ emptyText: t('stores.offers.empty') }}
        />
      )}
    </Drawer>
  )
}
