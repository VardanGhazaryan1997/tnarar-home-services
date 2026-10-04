import { App, Form, Input, InputNumber, Modal, Radio, Select } from 'antd'
import { useEffect, useRef } from 'react'
import { useTranslation } from 'react-i18next'
import { applyApiError } from './formErrors'
import { cleanNames, slugify } from './names'
import NameFields from './NameFields'
import { useCatalogLanguages } from './useCatalogLanguages'

/**
 * Creates or edits a town/village or a district. `place` is the row being edited (or null to create).
 * `onSave(body)` runs the right mutation and returns its promise. With `regions` (towns and villages only), the
 * form also asks for the region and whether the place is a town or a village.
 */
export default function PlaceFormModal({ open, title, place, regions, regionName = (region) => region.slug, onSave, onClose }) {
  const { t } = useTranslation()
  const { message } = App.useApp()
  const [form] = Form.useForm()
  const { languages, defaultCode } = useCatalogLanguages()
  const slugEdited = useRef(false)
  const saving = useRef(false)

  // The form is recreated each time the dialog opens (destroyOnHidden), starting from these values.
  useEffect(() => {
    if (open) slugEdited.current = Boolean(place)
  }, [open, place])

  const placement = Boolean(regions)
  const initialValues = place
    ? { slug: place.slug, name: place.name, sortOrder: place.sortOrder, regionId: place.regionId ?? null, kind: place.kind ?? 'City' }
    : { slug: '', name: {}, sortOrder: 0, regionId: null, kind: 'City' }

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
        name: cleanNames({ ...(place?.name ?? {}), ...values.name }),
        sortOrder: values.sortOrder ?? 0,
        ...(placement ? { regionId: values.regionId ?? null, kind: values.kind } : {}),
      })
      message.success(t(place ? 'catalog.saved' : 'catalog.created'))
      onClose()
    } catch (error) {
      const text = applyApiError({ form, error, t, defaultCode })
      if (text) message.error(text)
    } finally {
      saving.current = false
    }
  }

  return (
    <Modal
      open={open}
      title={title}
      okText={t('catalog.form.save')}
      cancelText={t('catalog.form.cancel')}
      onOk={() => form.submit()}
      onCancel={onClose}
      destroyOnHidden
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
        {placement && (
          <>
            <Form.Item name="regionId" label={t('catalog.form.region')}>
              <Select
                allowClear
                showSearch
                optionFilterProp="label"
                placeholder={t('catalog.cities.noRegion')}
                options={regions.map((region) => ({ value: region.id, label: regionName(region) }))}
              />
            </Form.Item>
            <Form.Item name="kind" label={t('catalog.form.kind')}>
              <Radio.Group
                optionType="button"
                options={['City', 'Village'].map((kind) => ({ value: kind, label: t(`catalog.cities.kinds.${kind}`) }))}
              />
            </Form.Item>
          </>
        )}
        <Form.Item name="sortOrder" label={t('catalog.form.sortOrder')} extra={t('catalog.form.sortOrderHelp')}>
          <InputNumber min={0} precision={0} style={{ width: '100%' }} />
        </Form.Item>
      </Form>
    </Modal>
  )
}
