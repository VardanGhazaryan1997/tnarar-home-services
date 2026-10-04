import { InboxOutlined } from '@ant-design/icons'
import { Alert, App, Checkbox, Descriptions, Form, Modal, Select, Typography, Upload } from 'antd'
import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { errorMessage } from '@/api/errors'
import { downloadText } from './download'
import { useExportTextsMutation, useImportTextsMutation } from './translationsApi'

const languageOptions = (languages) => languages.map(({ code, nativeName }) => ({ value: code, label: `${nativeName} (${code})` }))

/** Downloads a namespace's texts in one language as an i18next JSON file. */
export function ExportModal({ open, ns, languages, onClose }) {
  const { t } = useTranslation()
  const { message } = App.useApp()
  const [form] = Form.useForm()
  const [exportTexts, { isLoading }] = useExportTextsMutation()

  const run = async ({ language, withFallback }) => {
    try {
      const json = await exportTexts({ ns, language, withFallback }).unwrap()
      downloadText(json, `${ns}.${language}.json`)
      onClose()
    } catch (failure) {
      message.error(errorMessage(t, failure))
    }
  }

  return (
    <Modal
      open={open}
      title={t('translations.export.title')}
      okText={t('translations.export.confirm')}
      okButtonProps={{ htmlType: 'submit', form: 'export-form', loading: isLoading }}
      cancelText={t('catalog.form.cancel')}
      onCancel={onClose}
      destroyOnHidden
    >
      <Form id="export-form" form={form} layout="vertical" onFinish={run} initialValues={{ language: languages[0]?.code, withFallback: false }}>
        <Form.Item name="language" label={t('translations.transfer.language')}>
          <Select options={languageOptions(languages)} />
        </Form.Item>
        <Form.Item name="withFallback" valuePropName="checked" extra={t('translations.export.withFallbackHelp')}>
          <Checkbox>{t('translations.export.withFallback')}</Checkbox>
        </Form.Item>
      </Form>
    </Modal>
  )
}

/** Loads an i18next JSON file into a namespace for one language and shows what changed. */
export function ImportModal({ open, ns, languages, onClose }) {
  const { t } = useTranslation()
  const [form] = Form.useForm()
  const [importTexts, { isLoading }] = useImportTextsMutation()
  const [result, setResult] = useState(null)
  const [failure, setFailure] = useState(null)

  const run = async ({ language, file, replace }) => {
    setFailure(null)
    let content
    try {
      content = JSON.parse(await file[0].originFileObj.text())
    } catch {
      setFailure(t('translations.import.notJson'))
      return
    }
    try {
      setResult(await importTexts({ ns, language, content, replace }).unwrap())
    } catch (error) {
      setFailure(errorMessage(t, error))
    }
  }

  const close = () => {
    setResult(null)
    setFailure(null)
    onClose()
  }

  return (
    <Modal
      open={open}
      title={t('translations.import.title')}
      okText={result ? t('staff.inviteLink.done') : t('translations.import.confirm')}
      okButtonProps={result ? undefined : { htmlType: 'submit', form: 'import-form', loading: isLoading }}
      onOk={result ? close : undefined}
      cancelButtonProps={result ? { style: { display: 'none' } } : undefined}
      cancelText={t('catalog.form.cancel')}
      onCancel={close}
      destroyOnHidden
    >
      {failure && <Alert type="error" showIcon title={failure} style={{ marginBottom: 16 }} />}
      {result ? (
        <>
          <Descriptions column={1} size="small" bordered>
            <Descriptions.Item label={t('translations.import.added')}>{result.added}</Descriptions.Item>
            <Descriptions.Item label={t('translations.import.updated')}>{result.updated}</Descriptions.Item>
            <Descriptions.Item label={t('translations.import.unchanged')}>{result.unchanged}</Descriptions.Item>
            <Descriptions.Item label={t('translations.import.removed')}>{result.removed}</Descriptions.Item>
          </Descriptions>
          {result.skipped.length > 0 && (
            <Alert
              type="warning"
              showIcon
              style={{ marginTop: 16 }}
              title={t('translations.import.skipped', { n: result.skipped.length })}
              description={<Typography.Text code>{result.skipped.join(', ')}</Typography.Text>}
            />
          )}
          {result.invalid.length > 0 && (
            <Alert
              type="error"
              showIcon
              style={{ marginTop: 16 }}
              title={t('translations.import.invalid', { n: result.invalid.length })}
              description={<Typography.Text code>{result.invalid.join(', ')}</Typography.Text>}
            />
          )}
        </>
      ) : (
        <Form id="import-form" form={form} layout="vertical" requiredMark={false} onFinish={run} initialValues={{ language: languages[0]?.code, replace: false }}>
          <Form.Item name="language" label={t('translations.transfer.language')} extra={t('translations.import.languageHelp')}>
            <Select options={languageOptions(languages)} />
          </Form.Item>
          <Form.Item
            name="file"
            label={t('translations.import.file')}
            valuePropName="fileList"
            getValueFromEvent={(event) => event.fileList.slice(-1)}
            rules={[{ required: true, message: t('translations.import.fileRequired') }]}
          >
            <Upload.Dragger accept=".json,application/json" beforeUpload={() => false} maxCount={1}>
              <p className="ant-upload-drag-icon">
                <InboxOutlined />
              </p>
              <p className="ant-upload-text">{t('translations.import.drop')}</p>
            </Upload.Dragger>
          </Form.Item>
          <Form.Item name="replace" valuePropName="checked" extra={t('translations.import.replaceHelp')}>
            <Checkbox>{t('translations.import.replace')}</Checkbox>
          </Form.Item>
        </Form>
      )}
    </Modal>
  )
}
