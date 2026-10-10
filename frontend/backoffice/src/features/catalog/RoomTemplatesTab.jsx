import { ArrowDownOutlined, ArrowUpOutlined, DeleteOutlined, UndoOutlined } from '@ant-design/icons'
import { Alert, App, Button, Flex, InputNumber, Select, Space, Table, Tag, Tooltip, Typography } from 'antd'
import { useMemo, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { errorMessage } from '@/api/errors'
import { useGetRoomTemplatesQuery, useGetWorkItemsQuery, useSetRoomTemplateMutation } from './catalogApi'
import { localizedName } from './names'
import {
  canMeasure,
  lineProblem,
  MAX_COUNT,
  MAX_LINES,
  MAX_PER_SQUARE_METER,
  MODES,
  moveLine,
  newLine,
  ROOM_TYPES,
  sameLines,
  toInputs,
  toLines,
} from './roomTemplates'
import { useCatalogLanguages } from './useCatalogLanguages'
import { marketRangeText, priceRangeText } from './workItems'

/**
 * Room templates: the work usually done in each kind of room, ticked by default when a customer adds such a room to an
 * estimate (and used for the quick estimate on the Portal home page). Work measured from the room (m² of walls,
 * skirting) needs nothing more; pieces and points get a fixed count or a count per m² of floor.
 */
export default function RoomTemplatesTab() {
  const { t, i18n } = useTranslation()
  const { message } = App.useApp()
  const lng = i18n.resolvedLanguage
  const { defaultCode } = useCatalogLanguages()
  const templates = useGetRoomTemplatesQuery()
  const { data: workItems = [] } = useGetWorkItemsQuery()
  const [setRoomTemplate, saving] = useSetRoomTemplateMutation()
  const [roomType, setRoomType] = useState(ROOM_TYPES[0])
  // Unsaved lines per room type; a type without an entry shows what's saved.
  const [drafts, setDrafts] = useState({})

  const itemsById = useMemo(() => new Map(workItems.map((item) => [item.id, item])), [workItems])
  const savedLines = (type) => toLines(templates.data?.find((template) => template.type === type)?.items)
  const lines = drafts[roomType] ?? savedLines(roomType)
  const dirty = (type) => drafts[type] != null && !sameLines(drafts[type], savedLines(type))
  const problems = lines.map((line) => lineProblem(line, itemsById.get(line.workItemId)))
  const nameOf = (item) => (item ? localizedName(item.name, lng, defaultCode) : '—')
  const unitText = (item) => (item ? t(`catalog.workItems.units.${item.unit}`) : '')

  const change = (next) => setDrafts((current) => ({ ...current, [roomType]: next }))
  const update = (index, patch) => change(lines.map((line, i) => (i === index ? { ...line, ...patch } : line)))
  const undo = () =>
    setDrafts((current) => {
      const next = { ...current }
      delete next[roomType]
      return next
    })

  const save = async () => {
    try {
      await setRoomTemplate({ roomType, items: toInputs(lines) }).unwrap()
      undo()
      message.success(t('catalog.saved'))
    } catch (failure) {
      message.error(errorMessage(t, failure))
    }
  }

  const used = new Set(lines.map((line) => line.workItemId))
  const addOptions = workItems
    .filter((item) => !used.has(item.id))
    .map((item) => ({ value: item.id, label: `${nameOf(item)} · ${unitText(item)}`, title: item.slug }))

  const columns = [
    { title: '#', key: 'order', width: 48, render: (_, __, index) => index + 1 },
    {
      title: t('catalog.roomTemplates.work'),
      key: 'work',
      render: (_, line) => {
        const item = itemsById.get(line.workItemId)
        const price = item && (marketRangeText(item, lng) ?? priceRangeText(item, lng))
        return (
          <Flex vertical>
            <Space size={4} wrap>
              <Typography.Text>{nameOf(item)}</Typography.Text>
              {item && !item.isActive && (
                <Tooltip title={t('catalog.roomTemplates.hiddenHelp')}>
                  <Tag>{t('catalog.status.hidden')}</Tag>
                </Tooltip>
              )}
            </Space>
            <Typography.Text type="secondary" style={{ fontSize: 12 }}>
              {[unitText(item), price].filter(Boolean).join(' · ')}
            </Typography.Text>
          </Flex>
        )
      },
    },
    {
      title: t('catalog.roomTemplates.amount'),
      key: 'amount',
      render: (_, line, index) => {
        const item = itemsById.get(line.workItemId)
        const name = nameOf(item)
        const problem = problems[index]
        return (
          <Flex vertical gap={4}>
            <Space wrap>
              <Select
                aria-label={`${t('catalog.roomTemplates.amount')}: ${name}`}
                style={{ minWidth: 190 }}
                value={line.mode}
                onChange={(mode) => update(index, { mode, value: mode === 'measured' ? null : mode === 'count' ? 1 : 0.25 })}
                options={MODES.map((mode) => ({
                  value: mode,
                  label: t(`catalog.roomTemplates.modes.${mode}`),
                  disabled: mode === 'measured' && !canMeasure(item),
                }))}
              />
              {line.mode !== 'measured' && (
                <InputNumber
                  aria-label={`${t(`catalog.roomTemplates.values.${line.mode}`)}: ${name}`}
                  min={0}
                  max={line.mode === 'count' ? MAX_COUNT : MAX_PER_SQUARE_METER}
                  step={line.mode === 'count' ? 1 : 0.05}
                  precision={line.mode === 'count' ? 2 : 3}
                  status={problem ? 'error' : undefined}
                  value={line.value}
                  onChange={(value) => update(index, { value })}
                  suffix={line.mode === 'count' ? unitText(item) : t('catalog.roomTemplates.perSquareMeterSuffix', { unit: unitText(item) })}
                />
              )}
            </Space>
            {problem && <Typography.Text type="danger">{t(problem)}</Typography.Text>}
          </Flex>
        )
      },
    },
    {
      title: t('catalog.columns.actions'),
      key: 'actions',
      align: 'right',
      render: (_, line, index) => {
        const name = nameOf(itemsById.get(line.workItemId))
        return (
          <Space size={0}>
            <Button type="text" icon={<ArrowUpOutlined />} disabled={index === 0} aria-label={`${t('catalog.roomTemplates.up')}: ${name}`} onClick={() => change(moveLine(lines, index, -1))} />
            <Button
              type="text"
              icon={<ArrowDownOutlined />}
              disabled={index === lines.length - 1}
              aria-label={`${t('catalog.roomTemplates.down')}: ${name}`}
              onClick={() => change(moveLine(lines, index, 1))}
            />
            <Button
              type="text"
              danger
              icon={<DeleteOutlined />}
              aria-label={`${t('catalog.roomTemplates.remove')}: ${name}`}
              onClick={() => change(lines.filter((_, i) => i !== index))}
            />
          </Space>
        )
      },
    },
  ]

  return (
    <>
      <Typography.Paragraph type="secondary">{t('catalog.roomTemplates.help')}</Typography.Paragraph>
      <Flex gap="middle" wrap align="center" style={{ marginBottom: 16 }}>
        <Select
          aria-label={t('catalog.roomTemplates.roomType')}
          style={{ minWidth: 260 }}
          value={roomType}
          onChange={setRoomType}
          options={ROOM_TYPES.map((type) => ({
            value: type,
            label: `${t(`catalog.roomTemplates.roomTypes.${type}`)} (${(drafts[type] ?? savedLines(type)).length})${dirty(type) ? ' •' : ''}`,
          }))}
        />
        <Select
          aria-label={t('catalog.roomTemplates.add')}
          placeholder={t('catalog.roomTemplates.add')}
          style={{ minWidth: 320, flex: 1, maxWidth: 480 }}
          showSearch
          optionFilterProp="label"
          value={null}
          disabled={lines.length >= MAX_LINES}
          onChange={(id) => change([...lines, newLine(itemsById.get(id))])}
          options={addOptions}
        />
      </Flex>
      {templates.error && <Alert type="error" showIcon title={errorMessage(t, templates.error)} style={{ marginBottom: 16 }} />}
      <Table
        rowKey="workItemId"
        columns={columns}
        dataSource={lines}
        loading={templates.isLoading}
        pagination={false}
        scroll={{ x: 'max-content' }}
        locale={{ emptyText: t('catalog.roomTemplates.empty') }}
      />
      <Flex gap="small" justify="end" style={{ marginTop: 16 }}>
        <Button icon={<UndoOutlined />} disabled={!dirty(roomType)} onClick={undo}>
          {t('catalog.roomTemplates.undo')}
        </Button>
        <Button type="primary" loading={saving.isLoading} disabled={!dirty(roomType) || problems.some(Boolean)} onClick={save}>
          {t('catalog.form.save')}
        </Button>
      </Flex>
    </>
  )
}
