import { App, Form, Input, InputNumber, Modal } from 'antd'
import { useTranslation } from 'react-i18next'
import { errorMessage, fieldErrors, problemCode } from '@/api/errors'
import { LANGUAGE_CODE_PATTERN } from './namespaces'
import { useCreateLanguageMutation, useUpdateLanguageMutation } from './translationsApi'

const NAME_MAX = 64

/** Adds a language (no `language`; it starts hidden) or changes its names and position. The code can't change. */
export default function LanguageFormModal({ open, language, onClose }) {
  const { t } = useTranslation()
  const { message } = App.useApp()
  const [form] = Form.useForm()
  const [create, { isLoading: creating }] = useCreateLanguageMutation()
  const [update, { isLoading: updating }] = useUpdateLanguageMutation()

  const save = async (values) => {
    const body = { name: values.name.trim(), nativeName: values.nativeName.trim(), sortOrder: values.sortOrder ?? 0 }
    try {
      if (language) await update({ code: language.code, ...body }).unwrap()
      else await create({ code: values.code.trim().toLowerCase(), ...body }).unwrap()
      message.success(t(language ? 'catalog.saved' : 'translations.languages.added'))
      onClose()
    } catch (error) {
      const fields = fieldErrors(t, error)
      if (problemCode(error) === 'language.code_taken') fields.push({ name: 'code', errors: [errorMessage(t, error)] })
      if (fields.length > 0) form.setFields(fields)
      else message.error(errorMessage(t, error))
    }
  }

  return (
    <Modal
      open={open}
      title={language ? t('translations.languages.edit', { name: language.nativeName }) : t('translations.languages.add')}
      okText={t('catalog.form.save')}
      okButtonProps={{ htmlType: 'submit', form: 'language-form', loading: creating || updating }}
      cancelText={t('catalog.form.cancel')}
      onCancel={onClose}
      destroyOnHidden
    >
      <Form
        id="language-form"
        form={form}
        layout="vertical"
        requiredMark={false}
        onFinish={save}
        initialValues={language ? { name: language.name, nativeName: language.nativeName, sortOrder: language.sortOrder } : { sortOrder: 0 }}
      >
        {!language && (
          <Form.Item
            name="code"
            label={t('translations.languages.code')}
            extra={t('translations.languages.codeHelp')}
            rules={[
              { required: true, message: t('translations.languages.codeRequired') },
              { pattern: LANGUAGE_CODE_PATTERN, message: t('errors.code.invalid') },
            ]}
          >
            <Input maxLength={12} />
          </Form.Item>
        )}
        <Form.Item
          name="name"
          label={t('translations.languages.name')}
          extra={t('translations.languages.nameHelp')}
          rules={[{ required: true, whitespace: true, message: t('errors.name.required') }]}
        >
          <Input maxLength={NAME_MAX} />
        </Form.Item>
        <Form.Item
          name="nativeName"
          label={t('translations.languages.nativeName')}
          extra={t('translations.languages.nativeNameHelp')}
          rules={[{ required: true, whitespace: true, message: t('errors.native_name.required') }]}
        >
          <Input maxLength={NAME_MAX} />
        </Form.Item>
        <Form.Item name="sortOrder" label={t('catalog.form.sortOrder')} extra={t('catalog.form.sortOrderHelp')}>
          <InputNumber min={0} max={10000} precision={0} />
        </Form.Item>
      </Form>
    </Modal>
  )
}
