import { Alert, DatePicker, Flex, Input, Select, Table, Typography } from 'antd'
import dayjs from 'dayjs'
import { useTranslation } from 'react-i18next'
import { useSearchParams } from 'react-router'
import { errorMessage } from '@/api/errors'
import { formatDate } from '@/i18n/format'
import AuditChanges from './AuditChanges'
import { ActionTag, Actor, Entity } from './AuditEntryParts'
import { useGetAuditLogQuery } from './auditApi'
import { ACTIONS, ENTITY_TYPES } from './entities'

/**
 * Every change to audited data, newest first, with old and new values. Filters live in the address
 * (?entityType=&entityId=&action=&from=&to=&page=), so other pages can link to an entity's history.
 */
export default function AuditLogPage() {
  const { t, i18n } = useTranslation()
  const [params, setParams] = useSearchParams()
  const entityType = params.get('entityType') ?? undefined
  const entityId = params.get('entityId') ?? ''
  const action = params.get('action') ?? undefined
  const from = params.get('from') ?? undefined
  const to = params.get('to') ?? undefined
  const page = Number(params.get('page') ?? 1)
  const { data, isFetching, error } = useGetAuditLogQuery({ entityType, entityId, action, from, to, page })

  const update = (changes) => {
    const next = new URLSearchParams(params)
    for (const [key, value] of Object.entries({ page: undefined, ...changes })) {
      if (value === undefined || value === '') next.delete(key)
      else next.set(key, String(value))
    }
    setParams(next)
  }

  const columns = [
    {
      title: t('audit.columns.when'),
      key: 'when',
      width: 170,
      render: (_, entry) => formatDate(entry.occurredAt, i18n.language, { withTime: true }),
    },
    { title: t('audit.columns.who'), key: 'who', render: (_, entry) => <Actor entry={entry} /> },
    { title: t('audit.columns.action'), key: 'action', render: (_, entry) => <ActionTag action={entry.action} /> },
    { title: t('audit.columns.what'), key: 'what', render: (_, entry) => <Entity entry={entry} /> },
    {
      title: t('audit.columns.changes'),
      key: 'changes',
      responsive: ['md'],
      render: (_, entry) => t('audit.changeCount', { n: entry.changes.length }),
    },
  ]

  return (
    <>
      <Typography.Title level={1}>{t('nav.audit')}</Typography.Title>
      <Flex vertical gap="middle">
        <Typography.Text type="secondary">{t('audit.help')}</Typography.Text>
        <Flex gap="small" wrap>
          <Select
            aria-label={t('audit.filters.entityType')}
            placeholder={t('audit.filters.anyEntity')}
            allowClear
            showSearch
            optionFilterProp="label"
            value={entityType}
            onChange={(value) => update({ entityType: value })}
            options={ENTITY_TYPES.map((value) => ({ value, label: t(`audit.entities.${value}`) }))}
            style={{ minWidth: 200 }}
          />
          <Input.Search
            key={entityId}
            aria-label={t('audit.filters.entityId')}
            placeholder={t('audit.filters.entityIdPlaceholder')}
            defaultValue={entityId}
            allowClear
            onSearch={(value) => update({ entityId: value.trim() || undefined })}
            style={{ maxWidth: 320 }}
          />
          <Select
            aria-label={t('audit.filters.action')}
            placeholder={t('audit.filters.anyAction')}
            allowClear
            value={action}
            onChange={(value) => update({ action: value })}
            options={ACTIONS.map((value) => ({ value, label: t(`audit.actions.${value}`) }))}
            style={{ minWidth: 160 }}
          />
          <DatePicker.RangePicker
            aria-label={t('audit.filters.period')}
            value={[from ? dayjs(from) : null, to ? dayjs(to) : null]}
            allowEmpty={[true, true]}
            onChange={(range) =>
              update({
                from: range?.[0] ? range[0].startOf('day').toISOString() : undefined,
                to: range?.[1] ? range[1].endOf('day').toISOString() : undefined,
              })
            }
          />
        </Flex>
        {error && <Alert type="error" showIcon title={errorMessage(t, error)} />}
        <Table
          rowKey="id"
          columns={columns}
          dataSource={data?.items ?? []}
          loading={isFetching}
          scroll={{ x: true }}
          locale={{ emptyText: t('audit.empty') }}
          expandable={{ expandedRowRender: (entry) => <AuditChanges changes={entry.changes} />, rowExpandable: (entry) => entry.changes.length > 0 }}
          pagination={{
            current: page,
            pageSize: data?.pageSize ?? 50,
            total: data?.totalCount ?? 0,
            showSizeChanger: false,
            hideOnSinglePage: true,
            onChange: (next) => update({ page: next === 1 ? undefined : next }),
          }}
        />
      </Flex>
    </>
  )
}
