import { DeleteOutlined, DownloadOutlined, EditOutlined, PlusOutlined, UploadOutlined } from '@ant-design/icons'
import { Alert, App, Button, Card, Flex, Input, Popconfirm, Progress, Segmented, Select, Space, Table, Tag, Tooltip, Typography } from 'antd'
import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { useSearchParams } from 'react-router'
import { errorMessage } from '@/api/errors'
import { KNOWN_NAMESPACES } from './namespaces'
import TextFormModal from './TextFormModal'
import { ExportModal, ImportModal } from './TransferModals'
import { useDeleteTextMutation, useGetAdminLanguagesQuery, useGetNamespacesQuery, useGetTextsQuery } from './translationsApi'

// Default language first, then by position.
const ordered = (languages) => [...languages].sort((a, b) => Number(b.isDefault) - Number(a.isDefault) || a.sortOrder - b.sortOrder)

function Progresses({ namespace, languages }) {
  const { t } = useTranslation()
  if (!namespace || namespace.keyCount === 0) return <Typography.Text type="secondary">{t('translations.texts.noKeys')}</Typography.Text>

  return (
    <Flex wrap gap="large">
      {languages.map((language) => {
        const progress = namespace.languages.find((p) => p.language === language.code)
        const translated = progress?.translated ?? 0
        return (
          <Flex key={language.code} vertical style={{ minWidth: 160 }}>
            <Typography.Text>
              {language.nativeName} · {t('translations.texts.translated', { translated, total: namespace.keyCount })}
            </Typography.Text>
            <Progress percent={Math.round((translated / namespace.keyCount) * 100)} size="small" />
          </Flex>
        )
      })}
    </Flex>
  )
}

/**
 * Interface texts of one app (namespace), one row per key and a column per language. The default
 * language's keys are the full set; empty texts in other languages show the default text.
 * Filters live in the address (?ns=&search=&missingIn=&page=).
 */
export default function TextsTab() {
  const { t } = useTranslation()
  const { message } = App.useApp()
  const [params, setParams] = useSearchParams()
  const ns = params.get('ns') ?? KNOWN_NAMESPACES[0]
  const search = params.get('search') ?? ''
  const missingIn = params.get('missingIn') ?? undefined
  const page = Number(params.get('page') ?? 1)
  const { data: namespaces = [] } = useGetNamespacesQuery()
  const { data: allLanguages = [] } = useGetAdminLanguagesQuery()
  const { data, isFetching, error } = useGetTextsQuery({ ns, search, missingIn, page })
  const [deleteText] = useDeleteTextMutation()
  // null = closed, {} = new key, otherwise the row being edited.
  const [editing, setEditing] = useState(null)
  const [transfer, setTransfer] = useState(null)
  const languages = ordered(allLanguages)

  const update = (changes) => {
    const next = new URLSearchParams(params)
    for (const [key, value] of Object.entries({ page: undefined, ...changes })) {
      if (value === undefined || value === '') next.delete(key)
      else next.set(key, String(value))
    }
    setParams(next)
  }

  const remove = async (key) => {
    try {
      await deleteText({ ns, key }).unwrap()
      message.success(t('catalog.deleted'))
    } catch (failure) {
      message.error(errorMessage(t, failure))
    }
  }

  const columns = [
    {
      title: t('translations.texts.key'),
      dataIndex: 'key',
      width: 240,
      render: (key) => (
        <Typography.Text code style={{ wordBreak: 'break-all' }}>
          {key}
        </Typography.Text>
      ),
    },
    ...languages.map((language) => ({
      title: language.nativeName,
      key: language.code,
      render: (_, row) =>
        row.values[language.code] ? (
          <Typography.Paragraph ellipsis={{ rows: 2 }} style={{ margin: 0 }} lang={language.code}>
            {row.values[language.code]}
          </Typography.Paragraph>
        ) : (
          <Tag color="warning">{t('translations.texts.missing')}</Tag>
        ),
    })),
    {
      title: t('catalog.columns.actions'),
      key: 'actions',
      fixed: 'right',
      render: (_, row) => (
        <Space>
          <Tooltip title={t('catalog.actions.edit')}>
            <Button type="text" icon={<EditOutlined />} aria-label={`${t('catalog.actions.edit')}: ${row.key}`} onClick={() => setEditing(row)} />
          </Tooltip>
          <Popconfirm
            title={t('translations.texts.deleteConfirm', { key: row.key })}
            description={t('translations.texts.deleteHelp')}
            okText={t('catalog.actions.delete')}
            okButtonProps={{ danger: true }}
            cancelText={t('catalog.form.cancel')}
            onConfirm={() => remove(row.key)}
          >
            <Button type="text" danger icon={<DeleteOutlined />} aria-label={`${t('catalog.actions.delete')}: ${row.key}`} />
          </Popconfirm>
        </Space>
      ),
    },
  ]

  const namespaceOptions = [...new Set([...KNOWN_NAMESPACES, ...namespaces.map((n) => n.namespace)])].map((value) => ({
    value,
    label: t(`translations.namespaces.${value}`, { defaultValue: value }),
  }))

  return (
    <Flex vertical gap="middle">
      <Segmented aria-label={t('translations.texts.namespace')} value={ns} onChange={(value) => update({ ns: value, search: undefined, missingIn: undefined })} options={namespaceOptions} />
      <Card size="small">
        <Progresses namespace={namespaces.find((n) => n.namespace === ns)} languages={languages} />
      </Card>
      <Flex gap="small" wrap justify="space-between">
        <Flex gap="small" wrap>
          <Input.Search
            key={ns}
            aria-label={t('translations.texts.search')}
            placeholder={t('translations.texts.searchPlaceholder')}
            defaultValue={search}
            allowClear
            onSearch={(value) => update({ search: value.trim() || undefined })}
            style={{ maxWidth: 320 }}
          />
          <Select
            aria-label={t('translations.texts.missingIn')}
            placeholder={t('translations.texts.missingInPlaceholder')}
            allowClear
            value={missingIn}
            onChange={(value) => update({ missingIn: value })}
            options={languages.map(({ code, nativeName }) => ({ value: code, label: t('translations.texts.missingInLanguage', { language: nativeName }) }))}
            style={{ minWidth: 220 }}
          />
        </Flex>
        <Space wrap>
          <Button icon={<UploadOutlined />} onClick={() => setTransfer('import')}>
            {t('translations.import.button')}
          </Button>
          <Button icon={<DownloadOutlined />} onClick={() => setTransfer('export')}>
            {t('translations.export.button')}
          </Button>
          <Button type="primary" icon={<PlusOutlined />} onClick={() => setEditing({})}>
            {t('translations.texts.add')}
          </Button>
        </Space>
      </Flex>
      <Typography.Text type="secondary">{t('translations.texts.help')}</Typography.Text>
      {error && <Alert type="error" showIcon title={errorMessage(t, error)} />}
      <Table
        rowKey="key"
        columns={columns}
        dataSource={data?.items ?? []}
        loading={isFetching}
        scroll={{ x: 'max-content' }}
        locale={{ emptyText: t('translations.texts.empty') }}
        pagination={{
          current: page,
          pageSize: data?.pageSize ?? 50,
          total: data?.totalCount ?? 0,
          showSizeChanger: false,
          hideOnSinglePage: true,
          onChange: (next) => update({ page: next === 1 ? undefined : next }),
        }}
      />
      <TextFormModal open={editing !== null} ns={ns} row={editing?.key ? editing : undefined} languages={languages} onClose={() => setEditing(null)} />
      <ExportModal open={transfer === 'export'} ns={ns} languages={languages} onClose={() => setTransfer(null)} />
      <ImportModal open={transfer === 'import'} ns={ns} languages={languages} onClose={() => setTransfer(null)} />
    </Flex>
  )
}
