import { App, Form, Input, InputNumber, Modal, Select } from 'antd'
import { useEffect, useRef } from 'react'
import { useTranslation } from 'react-i18next'
import { useCreateCategoryMutation, useUpdateCategoryMutation } from './catalogApi'
import { applyApiError } from './formErrors'
import { cleanNames, localizedName, slugify } from './names'
import NameFields from './NameFields'
import { useCatalogLanguages } from './useCatalogLanguages'

const ICON_MAX_LENGTH = 32

/**
 * Creates or edits a category. `category` is the row being edited (or null to create);
 * `parentId` pre-selects the parent when adding a subcategory; `topLevel` lists possible parents.
 */
export default function CategoryFormModal({ open, category, parentId = null, topLevel, onClose }) {
  const { t, i18n } = useTranslation()
  const { message } = App.useApp()
  const [form] = Form.useForm()
  const { languages, defaultCode } = useCatalogLanguages()
  const [createCategory, creating] = useCreateCategoryMutation()
  const [updateCategory, updating] = useUpdateCategoryMutation()
  const slugEdited = useRef(false)
  const isEdit = Boolean(category)
  const hasChildren = Boolean(category?.children?.length)

  // The form is recreated each time the dialog opens (destroyOnHidden), starting from these values.
  useEffect(() => {
    if (open) slugEdited.current = isEdit
  }, [open, isEdit])

  const initialValues = category
    ? { slug: category.slug, name: category.name, icon: category.icon, parentId: category.parentId, sortOrder: category.sortOrder }
    : { slug: '', name: {}, icon: '', parentId, sortOrder: 0 }

  // While creating, suggest a slug from the English name until the slug is edited by hand.
  const handleValuesChange = (changed) => {
    if ('slug' in changed) slugEdited.current = true
    if (!slugEdited.current && changed.name?.en !== undefined) {
      form.setFieldValue('slug', slugify(changed.name.en))
    }
  }

  const handleFinish = async (values) => {
    const body = {
      slug: values.slug.trim(),
      // Keep translations for languages that aren't active right now.
      name: cleanNames({ ...(category?.name ?? {}), ...values.name }),
      icon: values.icon?.trim() || null,
      parentId: values.parentId ?? null,
      sortOrder: values.sortOrder ?? 0,
    }

    try {
      if (isEdit) await updateCategory({ id: category.id, ...body }).unwrap()
      else await createCategory(body).unwrap()
      message.success(t(isEdit ? 'catalog.saved' : 'catalog.created'))
      onClose()
    } catch (error) {
      const text = applyApiError({ form, error, t, defaultCode })
      if (text) message.error(text)
    }
  }

  const parentOptions = topLevel
    .filter((c) => c.id !== category?.id)
    .map((c) => ({ value: c.id, label: localizedName(c.name, i18n.resolvedLanguage, defaultCode) }))

  return (
    <Modal
      open={open}
      title={t(isEdit ? 'catalog.categories.edit' : 'catalog.categories.add')}
      okText={t('catalog.form.save')}
      cancelText={t('catalog.form.cancel')}
      onOk={() => form.submit()}
      onCancel={onClose}
      confirmLoading={creating.isLoading || updating.isLoading}
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
        <Form.Item name="parentId" label={t('catalog.categories.parent')} extra={hasChildren ? t('catalog.categories.parentLocked') : null}>
          <Select allowClear disabled={hasChildren} placeholder={t('catalog.categories.topLevel')} options={parentOptions} />
        </Form.Item>
        <Form.Item name="icon" label={t('catalog.categories.icon')} extra={t('catalog.categories.iconHelp')}>
          <Input maxLength={ICON_MAX_LENGTH} />
        </Form.Item>
        <Form.Item name="sortOrder" label={t('catalog.form.sortOrder')} extra={t('catalog.form.sortOrderHelp')}>
          <InputNumber min={0} precision={0} style={{ width: '100%' }} />
        </Form.Item>
      </Form>
    </Modal>
  )
}
