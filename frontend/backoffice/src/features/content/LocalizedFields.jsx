import { Form, Input, Tabs } from 'antd'
import { useTranslation } from 'react-i18next'

/**
 * Per-language inputs in one tab per language (default language first). `fields` describe the inputs:
 * [{ name: "title", label, max, rows? }] — `rows` makes a multi-line input. In the default language the
 * fields marked `required` must be filled; other languages fall back to it on the website.
 */
export default function LocalizedFields({ languages, defaultCode, fields }) {
  const { t } = useTranslation()

  return (
    <Tabs
      size="small"
      items={languages.map((language) => ({
        key: language.code,
        label: language.code === defaultCode ? `${language.nativeName} *` : language.nativeName,
        // Render every tab so validation reaches fields in tabs nobody opened.
        forceRender: true,
        children: fields.map((field) => (
          <Form.Item
            key={field.name}
            name={[field.name, language.code]}
            label={`${field.label} (${language.nativeName})`}
            rules={[
              ...(field.required && language.code === defaultCode
                ? [{ required: true, whitespace: true, message: t('content.form.requiredInDefault') }]
                : []),
              { max: field.max, message: t('content.form.tooLong') },
            ]}
          >
            {field.rows ? (
              <Input.TextArea lang={language.code} rows={field.rows} maxLength={field.max} showCount />
            ) : (
              <Input lang={language.code} maxLength={field.max} />
            )}
          </Form.Item>
        )),
      }))}
    />
  )
}
