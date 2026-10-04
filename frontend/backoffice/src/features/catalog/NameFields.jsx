import { Form, Input } from 'antd'
import { useTranslation } from 'react-i18next'

const NAME_MAX_LENGTH = 100

/** One name input per language; the default language is required. */
export default function NameFields({ languages, defaultCode }) {
  const { t } = useTranslation()

  return languages.map((language) => (
    <Form.Item
      key={language.code}
      name={['name', language.code]}
      label={t('catalog.form.nameIn', { language: language.nativeName })}
      rules={
        language.code === defaultCode
          ? [{ required: true, whitespace: true, message: t('catalog.form.nameRequired') }]
          : []
      }
    >
      <Input lang={language.code} maxLength={NAME_MAX_LENGTH} />
    </Form.Item>
  ))
}
