import { App, Checkbox, Flex, Form, Input, InputNumber, Modal, Select } from 'antd'
import { useEffect, useRef } from 'react'
import { useTranslation } from 'react-i18next'
import { applyApiError } from './formErrors'
import { cleanNames, localizedName, slugify } from './names'
import NameFields from './NameFields'
import { useCatalogLanguages } from './useCatalogLanguages'
import { priceProblem, SURFACES, subcategoryGroups, UNITS } from './workItems'

const PRICE_FIELDS = ['priceMin', 'priceTypical', 'priceMax']
const PRICE_LIMIT = 100_000_000

/**
 * Adds or edits a work item. `item` is the row being edited (null to add); `defaultCategoryId` preselects a
 * subcategory when adding. `onSave(body)` runs the mutation and returns its promise.
 */
export default function WorkItemFormModal({ open, item, categories, defaultCategoryId = null, onSave, onClose }) {
  const { t, i18n } = useTranslation()
  const { message } = App.useApp()
  const [form] = Form.useForm()
  const { languages, defaultCode } = useCatalogLanguages()
  const slugEdited = useRef(false)
  const saving = useRef(false)

  useEffect(() => {
    if (open) slugEdited.current = Boolean(item)
  }, [open, item])

  const nameOf = (category) => localizedName(category.name, i18n.resolvedLanguage, defaultCode)
  const initialValues = item
    ? {
        slug: item.slug,
        name: item.name,
        categoryId: item.categoryId,
        unit: item.unit,
        surface: item.surface,
        priceMin: item.priceMin,
        priceTypical: item.priceTypical,
        priceMax: item.priceMax,
        isPriceLocked: item.isPriceLocked ?? false,
        sortOrder: item.sortOrder,
      }
    : { slug: '', name: {}, categoryId: defaultCategoryId, unit: 'SquareMeter', surface: 'None', isPriceLocked: false, sortOrder: 0 }

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
        categoryId: values.categoryId,
        slug: values.slug.trim(),
        name: cleanNames({ ...(item?.name ?? {}), ...values.name }),
        unit: values.unit,
        surface: values.surface,
        sortOrder: values.sortOrder ?? 0,
        priceMin: values.priceMin ?? null,
        priceTypical: values.priceTypical ?? null,
        priceMax: values.priceMax ?? null,
        isPriceLocked: Boolean(values.isPriceLocked),
      })
      message.success(t(item ? 'catalog.saved' : 'catalog.created'))
      onClose()
    } catch (error) {
      const text = applyApiError({ form, error, t, defaultCode, fieldNames: { price: 'priceTypical' } })
      if (text) message.error(text)
    } finally {
      saving.current = false
    }
  }

  // The range check sits on the typical price and re-runs whenever any of the three changes.
  const priceRule = ({ getFieldsValue }) => ({
    validator: () => {
      const problem = priceProblem(getFieldsValue(PRICE_FIELDS))
      return problem ? Promise.reject(new Error(t(problem))) : Promise.resolve()
    },
  })

  return (
    <Modal
      open={open}
      title={t(item ? 'catalog.workItems.edit' : 'catalog.workItems.add')}
      okText={t('catalog.form.save')}
      cancelText={t('catalog.form.cancel')}
      onOk={() => form.submit()}
      onCancel={onClose}
      destroyOnHidden
      width={640}
    >
      <Form form={form} layout="vertical" initialValues={initialValues} onFinish={handleFinish} onValuesChange={handleValuesChange}>
        <Form.Item
          name="categoryId"
          label={t('catalog.workItems.subcategory')}
          rules={[{ required: true, message: t('catalog.workItems.subcategoryRequired') }]}
        >
          <Select showSearch optionFilterProp="label" options={subcategoryGroups(categories, nameOf, t('catalog.status.hidden'))} />
        </Form.Item>
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
        <Flex gap="middle" wrap>
          <Form.Item name="unit" label={t('catalog.workItems.unit')} style={{ flex: '1 1 200px' }}>
            <Select options={UNITS.map((unit) => ({ value: unit, label: t(`catalog.workItems.units.${unit}`) }))} />
          </Form.Item>
          <Form.Item
            name="surface"
            label={t('catalog.workItems.surface')}
            extra={t('catalog.workItems.surfaceHelp')}
            style={{ flex: '1 1 200px' }}
          >
            <Select options={SURFACES.map((surface) => ({ value: surface, label: t(`catalog.workItems.surfaces.${surface}`) }))} />
          </Form.Item>
        </Flex>
        <Flex gap="middle" wrap>
          {PRICE_FIELDS.map((field) => (
            <Form.Item
              key={field}
              name={field}
              label={t(`catalog.workItems.${field}`)}
              style={{ flex: '1 1 150px' }}
              dependencies={field === 'priceTypical' ? ['priceMin', 'priceMax'] : undefined}
              rules={field === 'priceTypical' ? [priceRule] : undefined}
            >
              <InputNumber min={0} max={PRICE_LIMIT} precision={0} step={100} suffix="֏" style={{ width: '100%' }} />
            </Form.Item>
          ))}
        </Flex>
        <Form.Item name="isPriceLocked" valuePropName="checked" extra={t('catalog.workItems.lockHelp')}>
          <Checkbox>{t('catalog.workItems.lock')}</Checkbox>
        </Form.Item>
        <Form.Item name="sortOrder" label={t('catalog.form.sortOrder')} extra={t('catalog.form.sortOrderHelp')}>
          <InputNumber min={0} precision={0} style={{ width: '100%' }} />
        </Form.Item>
      </Form>
    </Modal>
  )
}
