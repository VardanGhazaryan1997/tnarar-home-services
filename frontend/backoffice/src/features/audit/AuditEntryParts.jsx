import { Flex, Tag, Typography } from 'antd'
import { useTranslation } from 'react-i18next'
import { Link } from 'react-router'
import { ACTION_COLORS, entityPage } from './entities'

export function ActionTag({ action }) {
  const { t } = useTranslation()
  return <Tag color={ACTION_COLORS[action]}>{t(`audit.actions.${action}`, { defaultValue: action })}</Tag>
}

/** Who made a change: a staff member, a Portal user or the system. */
export function Actor({ entry }) {
  const { t } = useTranslation()
  if (entry.actorType === 'System') return <Typography.Text type="secondary">{t('audit.actorTypes.System')}</Typography.Text>
  return (
    <Flex vertical>
      <Typography.Text>{entry.actorName ?? entry.actorId ?? '—'}</Typography.Text>
      <Typography.Text type="secondary" style={{ fontSize: 12 }}>
        {t(`audit.actorTypes.${entry.actorType}`, { defaultValue: entry.actorType })}
      </Typography.Text>
    </Flex>
  )
}

/** What was changed, linked to its page when it has one. */
export function Entity({ entry }) {
  const { t } = useTranslation()
  const page = entityPage(entry.entityType, entry.entityId)
  const name = t(`audit.entities.${entry.entityType}`, { defaultValue: entry.entityType })
  return (
    <Flex vertical>
      {page ? <Link to={page}>{name}</Link> : <Typography.Text>{name}</Typography.Text>}
      <Typography.Text type="secondary" style={{ fontSize: 12, wordBreak: 'break-all' }}>
        {entry.entityId}
      </Typography.Text>
    </Flex>
  )
}
