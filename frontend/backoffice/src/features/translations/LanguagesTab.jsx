import { EditOutlined, EyeInvisibleOutlined, EyeOutlined, PlusOutlined } from '@ant-design/icons'
import { Alert, App, Button, Flex, Popconfirm, Space, Table, Tag, Tooltip, Typography } from 'antd'
import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { errorMessage } from '@/api/errors'
import LanguageFormModal from './LanguageFormModal'
import { useGetAdminLanguagesQuery, useSetLanguageActiveMutation } from './translationsApi'

/** Platform languages. New ones start hidden: translate the texts first, then show the language. */
export default function LanguagesTab() {
  const { t } = useTranslation()
  const { message } = App.useApp()
  const { data: languages = [], isFetching, error } = useGetAdminLanguagesQuery()
  const [setActive] = useSetLanguageActiveMutation()
  // null = closed, {} = new language, otherwise the language being edited.
  const [editing, setEditing] = useState(null)

  const toggle = async (language) => {
    try {
      await setActive({ code: language.code, active: !language.isActive }).unwrap()
      message.success(t(language.isActive ? 'translations.languages.hidden' : 'translations.languages.shown'))
    } catch (failure) {
      message.error(errorMessage(t, failure))
    }
  }

  const columns = [
    {
      title: t('translations.languages.language'),
      key: 'language',
      render: (_, language) => (
        <Space wrap>
          <Typography.Text strong>{language.nativeName}</Typography.Text>
          <Typography.Text type="secondary">{language.name}</Typography.Text>
          {language.isDefault && <Tag color="blue">{t('translations.languages.default')}</Tag>}
        </Space>
      ),
    },
    { title: t('translations.languages.code'), dataIndex: 'code' },
    { title: t('catalog.columns.sortOrder'), dataIndex: 'sortOrder', responsive: ['md'] },
    {
      title: t('catalog.columns.status'),
      key: 'status',
      render: (_, language) =>
        language.isActive ? <Tag color="success">{t('translations.languages.active')}</Tag> : <Tag>{t('translations.languages.inactive')}</Tag>,
    },
    {
      title: t('catalog.columns.actions'),
      key: 'actions',
      render: (_, language) => (
        <Space>
          <Tooltip title={t('catalog.actions.edit')}>
            <Button type="text" icon={<EditOutlined />} aria-label={`${t('catalog.actions.edit')}: ${language.nativeName}`} onClick={() => setEditing(language)} />
          </Tooltip>
          {!language.isDefault &&
            (language.isActive ? (
              <Popconfirm
                title={t('translations.languages.hideConfirm', { name: language.nativeName })}
                description={t('translations.languages.hideHelp')}
                okText={t('translations.languages.hide')}
                cancelText={t('catalog.form.cancel')}
                onConfirm={() => toggle(language)}
              >
                <Button type="text" icon={<EyeInvisibleOutlined />} aria-label={`${t('translations.languages.hide')}: ${language.nativeName}`} />
              </Popconfirm>
            ) : (
              <Tooltip title={t('translations.languages.show')}>
                <Button type="text" icon={<EyeOutlined />} aria-label={`${t('translations.languages.show')}: ${language.nativeName}`} onClick={() => toggle(language)} />
              </Tooltip>
            ))}
        </Space>
      ),
    },
  ]

  return (
    <Flex vertical gap="middle">
      <Flex justify="space-between" align="center" wrap gap="small">
        <Typography.Text type="secondary">{t('translations.languages.help')}</Typography.Text>
        <Button type="primary" icon={<PlusOutlined />} onClick={() => setEditing({})}>
          {t('translations.languages.add')}
        </Button>
      </Flex>
      {error && <Alert type="error" showIcon title={errorMessage(t, error)} />}
      <Table rowKey="code" columns={columns} dataSource={languages} loading={isFetching} pagination={false} scroll={{ x: true }} />
      <LanguageFormModal open={editing !== null} language={editing?.code ? editing : undefined} onClose={() => setEditing(null)} />
    </Flex>
  )
}
