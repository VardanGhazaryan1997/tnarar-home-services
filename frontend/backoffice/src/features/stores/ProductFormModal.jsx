import { App, Checkbox, DatePicker, Flex, Form, Input, InputNumber, Modal, Select } from 'antd'
import { useRef } from 'react'
import { useTranslation } from 'react-i18next'
import { localizedName } from '@/features/catalog/names'
import { useCatalogLanguages } from '@/features/catalog/useCatalogLanguages'
import ImagePicker from '@/features/files/ImagePicker'
import { applyStoreError } from './formErrors'
import { GRADES, PRICE_LIMIT, productPriceProblem, toProductBody, toProductValues } from './stores'
import { useGetLinkableMaterialsQuery } from './storesApi'

/**
 * Adds or edits a store's product. `product` is the row being edited (null to add). Linking it to a material (with the
 * grade it counts as) is what makes its price a market price; the pack size is in that material's unit.
 */
export default function ProductFormModal({ open, product, onSave, onClose }) {
  const { t, i18n } = useTranslation()
  const { message } = App.useApp()
  const [form] = Form.useForm()
  const { defaultCode } = useCatalogLanguages()
  const { data: materials = [] } = useGetLinkableMaterialsQuery(undefined, { skip: !open })
  const saving = useRef(false)
  const materialId = Form.useWatch('materialId', form)
  const material = materials.find((item) => item.id === materialId)
  const unit = material ? t(`catalog.materials.units.${material.unit}`) : null
  const nameOf = (item) => localizedName(item.name, i18n.resolvedLanguage, defaultCode)

  const handleFinish = async (values) => {
    if (saving.current) return
    saving.current = true
    try {
      await onSave(toProductBody(values))
      message.success(t(product ? 'catalog.saved' : 'catalog.created'))
      onClose()
    } catch (error) {
      const text = applyStoreError({ form, error, t })
      if (text) message.error(text)
    } finally {
      saving.current = false
    }
  }

  const saleRule = ({ getFieldsValue }) => ({
    validator: () => {
      const problem = productPriceProblem(getFieldsValue(['price', 'salePrice', 'saleEndsOn']))
      return problem ? Promise.reject(new Error(t(problem))) : Promise.resolve()
    },
  })

  return (
    <Modal
      open={open}
      title={t(product ? 'stores.product.edit' : 'stores.product.add')}
      okText={t('catalog.form.save')}
      cancelText={t('catalog.form.cancel')}
      onOk={() => form.submit()}
      onCancel={onClose}
      destroyOnHidden
      width={680}
    >
      <Form form={form} layout="vertical" initialValues={toProductValues(product)} onFinish={handleFinish}>
        <Form.Item name="name" label={t('stores.product.name')} rules={[{ required: true, whitespace: true, message: t('stores.form.nameRequired') }]}>
          <Input maxLength={200} />
        </Form.Item>
        <Flex gap="middle" wrap>
          <Form.Item name="brand" label={t('stores.product.brand')} style={{ flex: '1 1 200px' }}>
            <Input maxLength={100} />
          </Form.Item>
          <Form.Item name="code" label={t('stores.product.code')} extra={t('stores.product.codeHelp')} style={{ flex: '1 1 200px' }}>
            <Input maxLength={64} />
          </Form.Item>
        </Flex>
        <Flex gap="middle" wrap>
          <Form.Item name="materialId" label={t('stores.product.material')} extra={t('stores.product.materialHelp')} style={{ flex: '2 1 280px' }}>
            <Select
              allowClear
              showSearch
              optionFilterProp="label"
              options={materials.map((item) => ({ value: item.id, label: nameOf(item) }))}
              onChange={(value) => {
                if (!value) form.setFieldValue('grade', undefined)
              }}
            />
          </Form.Item>
          <Form.Item
            name="grade"
            label={t('stores.product.grade')}
            style={{ flex: '1 1 160px' }}
            dependencies={['materialId']}
            rules={[{ required: Boolean(materialId), message: t('stores.product.gradeRequired') }]}
          >
            <Select disabled={!materialId} options={GRADES.map((grade) => ({ value: grade, label: t(`stores.grades.${grade}`) }))} />
          </Form.Item>
        </Flex>
        <Flex gap="middle" wrap>
          <Form.Item name="packSize" label={unit ? t('stores.product.packSizeIn', { unit }) : t('stores.product.packSize')} style={{ flex: '1 1 160px' }} rules={[{ required: true, message: t('stores.product.packSizeRequired') }]}>
            <InputNumber min={0.001} max={100000} style={{ width: '100%' }} />
          </Form.Item>
          <Form.Item name="packLabel" label={t('stores.product.packLabel')} extra={t('stores.product.packLabelHelp')} style={{ flex: '1 1 200px' }}>
            <Input maxLength={50} />
          </Form.Item>
        </Flex>
        <Flex gap="middle" wrap>
          <Form.Item name="price" label={t('stores.product.price')} style={{ flex: '1 1 160px' }} dependencies={['salePrice', 'saleEndsOn']} rules={[{ required: true, message: t('stores.product.priceRequired') }, saleRule]}>
            <InputNumber min={1} max={PRICE_LIMIT} precision={0} suffix="֏" style={{ width: '100%' }} />
          </Form.Item>
          <Form.Item name="salePrice" label={t('stores.product.salePrice')} style={{ flex: '1 1 160px' }} dependencies={['price', 'saleEndsOn']} rules={[saleRule]}>
            <InputNumber min={1} max={PRICE_LIMIT} precision={0} suffix="֏" style={{ width: '100%' }} />
          </Form.Item>
          <Form.Item name="saleEndsOn" label={t('stores.product.saleEndsOn')} style={{ flex: '1 1 160px' }} dependencies={['price', 'salePrice']} rules={[saleRule]}>
            <DatePicker style={{ width: '100%' }} />
          </Form.Item>
        </Flex>
        <Form.Item name="photo" label={t('stores.product.photo')} rules={[{ validator: (_, photo) => (photo?.uploading ? Promise.reject(new Error(t('files.stillUploading'))) : Promise.resolve()) }]}>
          <ImagePicker label={t('stores.product.photo')} />
        </Form.Item>
        <Flex gap="large" wrap>
          <Form.Item name="inStock" valuePropName="checked">
            <Checkbox>{t('stores.product.inStock')}</Checkbox>
          </Form.Item>
          <Form.Item name="isActive" valuePropName="checked" extra={t('stores.product.activeHelp')}>
            <Checkbox>{t('stores.product.active')}</Checkbox>
          </Form.Item>
        </Flex>
      </Form>
    </Modal>
  )
}
