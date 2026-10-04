import { Alert, App, Button, Card, Flex, Form, Popconfirm, Space, Spin, Table, Tag, Typography } from 'antd'
import { useEffect, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { useSelector } from 'react-redux'
import { errorMessage, fieldErrors } from '@/api/errors'
import { selectStaff } from '@/features/auth/authSlice'
import { hasPermission } from '@/features/auth/permissions'
import { useClearCategoryRateMutation, useGetCommissionRatesQuery, useSetCategoryRateMutation, useSetDefaultRateMutation } from './commissionsApi'
import { formatPercent, percentRules } from './commissionParts'
import PercentInput from './PercentInput'
import RateModal from './RateModal'

/**
 * Commission rates: the default, and each category's own rate (a subcategory without one uses its parent's).
 * Staff with commissions.manage change them; a changed rate applies to orders completed from then on.
 */
export default function RatesTab() {
  const { t, i18n } = useTranslation()
  const language = i18n.language
  const { message } = App.useApp()
  const staff = useSelector(selectStaff)
  const canManage = hasPermission(staff, 'commissions.manage')
  const { data, isLoading, error } = useGetCommissionRatesQuery()
  const [setCategory] = useSetCategoryRateMutation()
  const [clearCategory] = useClearCategoryRateMutation()
  const [editing, setEditing] = useState(null)
  if (isLoading) return <Spin />
  if (error) return <Alert type="error" showIcon title={errorMessage(t, error)} />

  const saveCategory = async (value) => {
    try {
      await setCategory({ categoryId: editing.categoryId, percent: value }).unwrap()
      message.success(t('commissions.rates.saved'))
    } catch (failure) {
      if (failure?.status === 400) throw failure
      message.error(errorMessage(t, failure))
    }
    setEditing(null)
  }

  const clear = async (category) => {
    try {
      await clearCategory(category.categoryId).unwrap()
      message.success(t('commissions.rates.cleared'))
    } catch (failure) {
      message.error(errorMessage(t, failure))
    }
  }

  const columns = [
    {
      title: t('commissions.rates.category'),
      key: 'name',
      render: (_, category) => (
        <Typography.Text strong={!category.parentId} style={category.parentId ? { paddingInlineStart: 24 } : undefined}>
          {category.name}
        </Typography.Text>
      ),
    },
    {
      title: t('commissions.rates.applies'),
      key: 'effective',
      render: (_, category) => (
        <Space wrap>
          <span>{formatPercent(category.effectivePercent, language)}</span>
          {category.percent === null && (
            <Tag>{t(category.parentId ? 'commissions.rates.fromParent' : 'commissions.rates.fromDefault')}</Tag>
          )}
        </Space>
      ),
    },
    {
      title: '',
      key: 'actions',
      render: (_, category) =>
        canManage && (
          <Space wrap>
            <Button size="small" onClick={() => setEditing(category)}>
              {t(category.percent === null ? 'commissions.rates.set' : 'commissions.rates.change')}
            </Button>
            {category.percent !== null && (
              <Popconfirm
                title={t('commissions.rates.clearConfirm')}
                okText={t('commissions.rates.clear')}
                cancelText={t('common.cancel')}
                onConfirm={() => clear(category)}
              >
                <Button size="small" danger>
                  {t('commissions.rates.clear')}
                </Button>
              </Popconfirm>
            )}
          </Space>
        ),
    },
  ]

  return (
    <Flex vertical gap="middle">
      <Typography.Text type="secondary">{t('commissions.rates.help')}</Typography.Text>
      <Card title={t('commissions.rates.default')}>
        {canManage ? (
          <DefaultRateForm percent={data.defaultPercent} />
        ) : (
          <Typography.Text strong>{formatPercent(data.defaultPercent, language)}</Typography.Text>
        )}
      </Card>
      <Table rowKey="categoryId" columns={columns} dataSource={data.categories} pagination={false} scroll={{ x: true }} locale={{ emptyText: t('commissions.rates.empty') }} />
      <RateModal category={editing} onSave={saveCategory} onClose={() => setEditing(null)} />
    </Flex>
  )
}

/** Changes the default rate; follows the saved value when the rates reload. */
function DefaultRateForm({ percent }) {
  const { t } = useTranslation()
  const { message } = App.useApp()
  const [form] = Form.useForm()
  const [setDefault, { isLoading }] = useSetDefaultRateMutation()

  useEffect(() => {
    form.setFieldsValue({ percent })
  }, [percent, form])

  const save = async ({ percent: value }) => {
    try {
      await setDefault(value).unwrap()
      message.success(t('commissions.rates.saved'))
    } catch (failure) {
      if (failure?.status === 400) form.setFields(fieldErrors(t, failure))
      else message.error(errorMessage(t, failure))
    }
  }

  return (
    <Form form={form} layout="inline" onFinish={save} initialValues={{ percent }}>
      <Form.Item name="percent" rules={percentRules(t)}>
        <PercentInput aria-label={t('commissions.rates.default')} />
      </Form.Item>
      <Form.Item>
        <Button type="primary" htmlType="submit" loading={isLoading}>
          {t('commissions.rates.save')}
        </Button>
      </Form.Item>
    </Form>
  )
}
