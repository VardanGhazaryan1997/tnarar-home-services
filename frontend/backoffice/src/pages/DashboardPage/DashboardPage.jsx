import { ArrowRightOutlined } from '@ant-design/icons'
import { Card, Col, Flex, Row, Skeleton, Statistic, Table, Typography } from 'antd'
import { useTranslation } from 'react-i18next'
import { useSelector } from 'react-redux'
import { Link } from 'react-router'
import { ActionTag, Actor, Entity } from '@/features/audit/AuditEntryParts'
import { useGetAuditLogQuery } from '@/features/audit/auditApi'
import { selectStaff } from '@/features/auth/authSlice'
import { hasPermission } from '@/features/auth/permissions'
import { useGetPagesQuery } from '@/features/content/contentApi'
import { useGetPartnersQuery } from '@/features/partners/partnersApi'
import { useGetStaffQuery } from '@/features/staff/staffApi'
import { useGetNamespacesQuery } from '@/features/translations/translationsApi'
import { useGetUsersQuery } from '@/features/users/usersApi'
import { formatDate } from '@/i18n/format'

/** One number with a link to the list behind it. `query` is an RTK Query result; `count` reads the number from its data. */
function Counter({ title, query, count, to, highlight }) {
  const { t } = useTranslation()
  const value = query.data ? count(query.data) : null

  return (
    <Col xs={24} sm={12} xl={8}>
      <Card size="small" style={{ height: '100%' }}>
        <Flex vertical gap={8}>
          {query.isError ? (
            <Statistic title={title} value="—" />
          ) : value === null ? (
            <Skeleton active paragraph={false} title={{ width: '60%' }} />
          ) : (
            <Statistic title={title} value={value} styles={highlight && value > 0 ? { content: { color: '#B4532C' } } : undefined} />
          )}
          <Link to={to}>
            {t('dashboard.open')} <ArrowRightOutlined />
          </Link>
        </Flex>
      </Card>
    </Col>
  )
}

const total = (page) => page.totalCount

function RecentActivity() {
  const { t, i18n } = useTranslation()
  const { data, isLoading } = useGetAuditLogQuery({ pageSize: 8 })

  return (
    <Card title={t('dashboard.recentActivity')} extra={<Link to="/audit">{t('dashboard.allActivity')}</Link>}>
      <Table
        size="small"
        rowKey="id"
        loading={isLoading}
        pagination={false}
        scroll={{ x: true }}
        dataSource={data?.items ?? []}
        locale={{ emptyText: t('audit.empty') }}
        columns={[
          { key: 'when', render: (_, entry) => formatDate(entry.occurredAt, i18n.language, { withTime: true }) },
          { key: 'who', render: (_, entry) => <Actor entry={entry} /> },
          { key: 'action', render: (_, entry) => <ActionTag action={entry.action} /> },
          { key: 'what', render: (_, entry) => <Entity entry={entry} /> },
        ]}
        showHeader={false}
      />
    </Card>
  )
}

/**
 * Start page: what needs attention, for the sections this staff member can open. Each counter reads the
 * total of the matching list (page size 1), so it always agrees with what the list shows.
 */
export default function DashboardPage() {
  const { t } = useTranslation()
  const staff = useSelector(selectStaff)
  const can = (permission) => hasPermission(staff, permission)

  const pendingPartners = useGetPartnersQuery({ status: 'UnderReview', pageSize: 1 }, { skip: !can('partners.view') })
  const users = useGetUsersQuery({ pageSize: 1 }, { skip: !can('users.view') })
  const blockedUsers = useGetUsersQuery({ status: 'Blocked', pageSize: 1 }, { skip: !can('users.view') })
  const invitedStaff = useGetStaffQuery({ status: 'Invited', pageSize: 1 }, { skip: !can('staff.view') })
  const pages = useGetPagesQuery(undefined, { skip: !can('content.manage') })
  const namespaces = useGetNamespacesQuery(undefined, { skip: !can('translations.manage') })

  const counters = [
    can('partners.view') && { key: 'pendingPartners', query: pendingPartners, count: total, to: '/partners', highlight: true },
    can('users.view') && { key: 'users', query: users, count: total, to: '/users' },
    can('users.view') && { key: 'blockedUsers', query: blockedUsers, count: total, to: '/users?status=Blocked' },
    can('staff.view') && { key: 'invitedStaff', query: invitedStaff, count: total, to: '/staff/members?status=Invited' },
    can('content.manage') && { key: 'draftPages', query: pages, count: (list) => list.filter((page) => !page.isPublished).length, to: '/content/pages' },
    can('translations.manage') && {
      key: 'missingTexts',
      query: namespaces,
      count: (list) => list.reduce((sum, ns) => sum + ns.languages.reduce((inner, language) => inner + language.missing, 0), 0),
      to: '/translations/texts',
      highlight: true,
    },
  ].filter(Boolean)

  return (
    <Flex vertical gap="middle">
      <div>
        <Typography.Title level={1}>{t('dashboard.title')}</Typography.Title>
        <Typography.Paragraph type="secondary">{t('dashboard.greeting', { name: staff?.fullName ?? '' })}</Typography.Paragraph>
      </div>
      {counters.length > 0 ? (
        <Row gutter={[16, 16]}>
          {counters.map(({ key, ...counter }) => (
            <Counter key={key} title={t(`dashboard.counters.${key}`)} {...counter} />
          ))}
        </Row>
      ) : (
        <Typography.Paragraph>{t('dashboard.welcome')}</Typography.Paragraph>
      )}
      {can('audit.view') && <RecentActivity />}
    </Flex>
  )
}
