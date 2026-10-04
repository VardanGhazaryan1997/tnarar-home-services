import { App, Form, Input, Modal, Typography } from 'antd'
import { useTranslation } from 'react-i18next'
import { errorMessage, fieldErrors } from '@/api/errors'
import { KEY_PATTERN } from './namespaces'
import { useSaveTextMutation } from './translationsApi'

const VALUE_MAX = 4000
const KEY_MAX = 200

/**
 * Adds a key (no `row`) or changes its texts. `languages` are all platform languages, default first;
 * the default language's text is required, an emptied text is removed (the default shows instead).
 */
export default function TextFormModal({ open, ns, row, languages, onClose }) {
  const { t } = useTranslation()
  const { message } = App.useApp()
  const [form] = Form.useForm()
  const [saveText, { isLoading }] = useSaveTextMutation()

  const save = async (values) => {
    const key = row?.key ?? values.key.trim()
    const texts = Object.fromEntries(languages.map(({ code }) => [code, values.values?.[code]?.trim() ? values.values[code] : '']))
    try {
      await saveText({ ns, key, values: texts }).unwrap()
      message.success(t(row ? 'catalog.saved' : 'catalog.created'))
      onClose()
    } catch (error) {
      const fields = fieldErrors(t, error).map((field) => (field.name === 'values' ? { ...field, name: ['values', languages[0].code] } : field))
      if (fields.length > 0) form.setFields(fields)
      else message.error(errorMessage(t, error))
    }
  }

  return (
    <Modal
      open={open}
      title={row ? t('translations.texts.edit') : t('translations.texts.add')}
      okText={t('catalog.form.save')}
      okButtonProps={{ htmlType: 'submit', form: 'text-form', loading: isLoading }}
      cancelText={t('catalog.form.cancel')}
      onCancel={onClose}
      width={720}
      destroyOnHidden
    >
      <Form id="text-form" form={form} layout="vertical" requiredMark={false} onFinish={save} initialValues={{ values: row?.values ?? {} }}>
        {row ? (
          <Typography.Paragraph>
            <Typography.Text code>{row.key}</Typography.Text>
          </Typography.Paragraph>
        ) : (
          <Form.Item
            name="key"
            label={t('translations.texts.key')}
            extra={t('translations.texts.keyHelp')}
            rules={[
              { required: true, message: t('translations.texts.keyRequired') },
              { pattern: KEY_PATTERN, message: t('errors.key.invalid') },
              { max: KEY_MAX, message: t('errors.key.invalid') },
            ]}
          >
            <Input maxLength={KEY_MAX} style={{ fontFamily: 'monospace' }} />
          </Form.Item>
        )}
        {languages.map((language, index) => (
          <Form.Item
            key={language.code}
            name={['values', language.code]}
            label={index === 0 ? t('translations.texts.defaultText', { language: language.nativeName }) : language.nativeName}
            rules={[
              ...(index === 0 ? [{ required: true, whitespace: true, message: t('content.form.requiredInDefault') }] : []),
              { max: VALUE_MAX, message: t('content.form.tooLong') },
            ]}
          >
            <Input.TextArea lang={language.code} autoSize={{ minRows: 1, maxRows: 6 }} maxLength={VALUE_MAX} />
          </Form.Item>
        ))}
      </Form>
    </Modal>
  )
}
