import { App, Checkbox, Flex, Form, Input, InputNumber, Modal, Select } from 'antd'
import { useEffect, useRef } from 'react'
import { useTranslation } from 'react-i18next'
import { applyApiError } from './formErrors'
import { MATERIAL_PRICE_LIMIT, MATERIAL_PRICES, MATERIAL_UNITS, materialPriceProblem } from './materials'
import { cleanNames, slugify } from './names'
import NameFields from './NameFields'
import { useCatalogLanguages } from './useCatalogLanguages'

/**
 * Adds or edits a material. `material` is the row being edited (null to add). `onSave(body)` runs the mutation and
 * returns its promise.
 */
export default function MaterialFormModal({ open, material, onSave, onClose }) {
  const { t } = useTranslation()
  const { message } = App.useApp()
  const [form] = Form.useForm()
  const { languages, defaultCode } = useCatalogLanguages()
  const slugEdited = useRef(false)
  const saving = useRef(false)

  useEffect(() => {
    if (open) slugEdited.current = Boolean(material)
  }, [open, material])

  const initialValues = material
    ? {
        slug: material.slug,
        name: material.name,
        unit: material.unit,
        priceBudget: material.priceBudget,
        priceStandard: material.priceStandard,
        pricePremium: material.pricePremium,
        sortOrder: material.sortOrder,
        isActive: material.isActive,
        isPriceLocked: material.isPriceLocked,
      }
    : { slug: '', name: {}, unit: 'Kilogram', sortOrder: 0, isActive: true }

  const handleValuesChange = (changed) => {
    if ('slug' in changed) slugEdited.current = true
    if (!slugEdited.current && changed.name?.en !== undefined) {
      form.setFieldValue('slug', slugify(changed.name.en))
    }
  }

  const handleFinish = async (values) => {
    if (saving.current) return
    saving.current = true
    try {
      await onSave({
        slug: values.slug.trim(),
        name: cleanNames({ ...(material?.name ?? {}), ...values.name }),
        unit: values.unit,
        priceBudget: values.priceBudget,
        priceStandard: values.priceStandard,
        pricePremium: values.pricePremium,
        sortOrder: values.sortOrder ?? 0,
        isActive: Boolean(values.isActive),
        // Only an existing material can be locked: the lock keeps staff prices over what the stores say.
        ...(material ? { isPriceLocked: Boolean(values.isPriceLocked) } : {}),
      })
      message.success(t(material ? 'catalog.saved' : 'catalog.created'))
      onClose()
    } catch (error) {
      const text = applyApiError({ form, error, t, defaultCode, fieldNames: { price: 'priceStandard' } })
      if (text) message.error(text)
    } finally {
      saving.current = false
    }
  }

  // The order check sits on the standard price and re-runs whenever any of the three changes.
  const priceRule = ({ getFieldsValue }) => ({
    validator: () => {
      const problem = materialPriceProblem(getFieldsValue(MATERIAL_PRICES))
      return problem ? Promise.reject(new Error(t(problem))) : Promise.resolve()
    },
  })

  return (
    <Modal
      open={open}
      title={t(material ? 'catalog.materials.edit' : 'catalog.materials.add')}
      okText={t('catalog.form.save')}
      cancelText={t('catalog.form.cancel')}
      onOk={() => form.submit()}
      onCancel={onClose}
      destroyOnHidden
      width={640}
    >
      <Form form={form} layout="vertical" initialValues={initialValues} onFinish={handleFinish} onValuesChange={handleValuesChange}>
        <NameFields languages={languages} defaultCode={defaultCode} />
        <Form.Item
          name="slug"
          label={t('catalog.form.slug')}
          extra={t('catalog.form.slugHelp')}
          rules={[
            { required: true, message: t('catalog.form.slugRequired') },
            { pattern: /^[a-z0-9]+(-[a-z0-9]+)*$/, message: t('errors.slug.invalid') },
          ]}
        >
          <Input maxLength={64} />
        </Form.Item>
        <Form.Item name="unit" label={t('catalog.materials.unit')}>
          <Select options={MATERIAL_UNITS.map((unit) => ({ value: unit, label: t(`catalog.materials.units.${unit}`) }))} />
        </Form.Item>
        <Flex gap="middle" wrap>
          {MATERIAL_PRICES.map((field) => (
            <Form.Item
              key={field}
              name={field}
              label={t(`catalog.materials.${field}`)}
              style={{ flex: '1 1 150px' }}
              dependencies={field === 'priceStandard' ? ['priceBudget', 'pricePremium'] : undefined}
              rules={field === 'priceStandard' ? [priceRule] : undefined}
            >
              <InputNumber min={1} max={MATERIAL_PRICE_LIMIT} precision={0} step={10} suffix="֏" style={{ width: '100%' }} />
            </Form.Item>
          ))}
        </Flex>
        {material && (
          <Form.Item name="isPriceLocked" valuePropName="checked" extra={t('catalog.materials.lockedHelp')}>
            <Checkbox>{t('catalog.materials.locked')}</Checkbox>
          </Form.Item>
        )}
        <Form.Item name="sortOrder" label={t('catalog.form.sortOrder')} extra={t('catalog.form.sortOrderHelp')}>
          <InputNumber min={0} precision={0} style={{ width: '100%' }} />
        </Form.Item>
        <Form.Item name="isActive" valuePropName="checked" extra={t('catalog.materials.activeHelp')}>
          <Checkbox>{t('catalog.materials.active')}</Checkbox>
        </Form.Item>
      </Form>
    </Modal>
  )
}
