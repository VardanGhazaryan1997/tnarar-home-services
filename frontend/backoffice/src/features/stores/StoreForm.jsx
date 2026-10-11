import { App, Checkbox, Form, Input, InputNumber } from 'antd'
import { useRef } from 'react'
import { useTranslation } from 'react-i18next'
import { slugify } from '@/features/catalog/names'
import { useCatalogLanguages } from '@/features/catalog/useCatalogLanguages'
import ImagePicker from '@/features/files/ImagePicker'
import { applyStoreError } from './formErrors'
import { isWebAddress, toStoreBody, toStoreValues } from './stores'

const DESCRIPTION_MAX = 2000

/**
 * The store fields: name, slug, description per language, website, phone, logo, order and visibility. `form` comes
 * from the caller, who submits it; `onSave(body)` runs the mutation and returns its promise, `onDone(saved)` runs after
 * it succeeded. `store` is the store being edited (null to add).
 */
export default function StoreForm({ form, store, onSave, onDone }) {
  const { t } = useTranslation()
  const { message } = App.useApp()
  const { languages, defaultCode } = useCatalogLanguages()
  const slugEdited = useRef(Boolean(store))
  const saving = useRef(false)

  const handleValuesChange = (changed) => {
    if ('slug' in changed) slugEdited.current = true
    if (!slugEdited.current && changed.name !== undefined) form.setFieldValue('slug', slugify(changed.name))
  }

  const handleFinish = async (values) => {
    if (saving.current) return
    saving.current = true
    try {
      const saved = await onSave(toStoreBody(values))
      message.success(t(store ? 'catalog.saved' : 'catalog.created'))
      onDone?.(saved)
    } catch (error) {
      const text = applyStoreError({ form, error, t, fieldNames: { description: ['description', defaultCode] } })
      if (text) message.error(text)
    } finally {
      saving.current = false
    }
  }

  const logoRule = {
    validator: (_, logo) => (logo?.uploading ? Promise.reject(new Error(t('files.stillUploading'))) : Promise.resolve()),
  }

  return (
    <Form form={form} layout="vertical" initialValues={toStoreValues(store)} onFinish={handleFinish} onValuesChange={handleValuesChange}>
      <Form.Item name="name" label={t('stores.form.name')} extra={t('stores.form.nameHelp')} rules={[{ required: true, whitespace: true, message: t('stores.form.nameRequired') }]}>
        <Input maxLength={100} />
      </Form.Item>
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
      {languages.map(({ code, nativeName }) => (
        <Form.Item key={code} name={['description', code]} label={t('stores.form.descriptionIn', { language: nativeName })}>
          <Input.TextArea rows={2} maxLength={DESCRIPTION_MAX} />
        </Form.Item>
      ))}
      <Form.Item
        name="website"
        label={t('stores.form.website')}
        rules={[{ validator: (_, value) => (!value?.trim() || isWebAddress(value) ? Promise.resolve() : Promise.reject(new Error(t('stores.form.websiteInvalid')))) }]}
      >
        <Input maxLength={300} placeholder="https://" />
      </Form.Item>
      <Form.Item name="phone" label={t('stores.form.phone')}>
        <Input maxLength={30} />
      </Form.Item>
      <Form.Item name="logo" label={t('stores.form.logo')} rules={[logoRule]}>
        <ImagePicker label={t('stores.form.logo')} />
      </Form.Item>
      <Form.Item name="sortOrder" label={t('catalog.form.sortOrder')} extra={t('catalog.form.sortOrderHelp')}>
        <InputNumber min={0} precision={0} style={{ width: '100%' }} />
      </Form.Item>
      <Form.Item name="isActive" valuePropName="checked" extra={t('stores.form.activeHelp')}>
        <Checkbox>{t('stores.form.active')}</Checkbox>
      </Form.Item>
    </Form>
  )
}
