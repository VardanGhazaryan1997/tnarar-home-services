import { DeleteOutlined, PlusOutlined } from '@ant-design/icons'
import { Alert, App, Button, Card, Flex, Form, Input, Select, Typography } from 'antd'
import { useMemo, useRef } from 'react'
import { useTranslation } from 'react-i18next'
import { errorMessage } from '@/api/errors'
import { useGetPlaceNamesQuery, useGetRegionNamesQuery } from '@/features/partners/partnersApi'
import { MAX_BRANCHES, blankBranch, toBranchInputs, toBranchRows, townOptions } from './stores'
import { useSetStoreBranchesMutation } from './storesApi'

/**
 * The store's branches as one list: staff edit the rows and save them all at once (the API replaces the list).
 * Towns and districts come from the public place lists, grouped by region.
 */
export default function BranchesTab({ store }) {
  const { t, i18n } = useTranslation()
  const { message } = App.useApp()
  const language = i18n.resolvedLanguage
  const [form] = Form.useForm()
  const [setBranches, { isLoading }] = useSetStoreBranchesMutation()
  const { data: cities = [] } = useGetPlaceNamesQuery(language)
  const { data: regions = [] } = useGetRegionNamesQuery(language)
  const saving = useRef(false)
  const towns = useMemo(() => townOptions(cities, regions, t('stores.branches.otherRegion')), [cities, regions, t])
  const districtsOf = (cityId) => (cities.find((city) => city.id === cityId)?.districts ?? []).map((district) => ({ value: district.id, label: district.name }))

  const handleFinish = async ({ branches = [] }) => {
    if (saving.current) return
    saving.current = true
    try {
      await setBranches({ id: store.id, branches: toBranchInputs(branches) }).unwrap()
      message.success(t('catalog.saved'))
    } catch (error) {
      message.error(errorMessage(t, error))
    } finally {
      saving.current = false
    }
  }

  return (
    <Form form={form} layout="vertical" initialValues={{ branches: toBranchRows(store.branches) }} onFinish={handleFinish}>
      <Typography.Paragraph type="secondary">{t('stores.branches.help')}</Typography.Paragraph>
      <Form.List name="branches">
        {(fields, { add, remove }) => (
          <Flex vertical gap="middle">
            {fields.length === 0 && <Alert type="info" showIcon title={t('stores.branches.empty')} />}
            {fields.map((field, index) => (
              <Card
                key={field.key}
                size="small"
                title={t('stores.branches.branch', { number: index + 1 })}
                extra={
                  <Button type="text" danger icon={<DeleteOutlined />} aria-label={`${t('catalog.actions.delete')}: ${t('stores.branches.branch', { number: index + 1 })}`} onClick={() => remove(field.name)} />
                }
              >
                <Flex gap="middle" wrap>
                  <Form.Item name={[field.name, 'name']} label={t('stores.branches.name')} style={{ flex: '1 1 240px' }} rules={[{ required: true, whitespace: true, message: t('stores.branches.nameRequired') }]}>
                    <Input maxLength={100} />
                  </Form.Item>
                  <Form.Item name={[field.name, 'cityId']} label={t('stores.branches.town')} style={{ flex: '1 1 200px' }} rules={[{ required: true, message: t('stores.branches.townRequired') }]}>
                    <Select
                      showSearch
                      optionFilterProp="label"
                      options={towns}
                      onChange={() => form.setFieldValue(['branches', field.name, 'districtId'], undefined)}
                    />
                  </Form.Item>
                  <Form.Item noStyle shouldUpdate={(before, after) => before.branches?.[field.name]?.cityId !== after.branches?.[field.name]?.cityId}>
                    {({ getFieldValue }) => {
                      const options = districtsOf(getFieldValue(['branches', field.name, 'cityId']))
                      return (
                        <Form.Item name={[field.name, 'districtId']} label={t('stores.branches.district')} style={{ flex: '1 1 200px' }}>
                          <Select allowClear options={options} disabled={options.length === 0} />
                        </Form.Item>
                      )
                    }}
                  </Form.Item>
                </Flex>
                <Form.Item name={[field.name, 'address']} label={t('stores.branches.address')} rules={[{ required: true, whitespace: true, message: t('stores.branches.addressRequired') }]}>
                  <Input maxLength={300} />
                </Form.Item>
                <Flex gap="middle" wrap>
                  <Form.Item name={[field.name, 'phone']} label={t('stores.form.phone')} style={{ flex: '1 1 200px' }}>
                    <Input maxLength={30} />
                  </Form.Item>
                  <Form.Item name={[field.name, 'openingHours']} label={t('stores.branches.hours')} style={{ flex: '1 1 240px' }}>
                    <Input maxLength={200} />
                  </Form.Item>
                </Flex>
              </Card>
            ))}
            <Flex gap="middle" align="center" wrap>
              <Button icon={<PlusOutlined />} disabled={fields.length >= MAX_BRANCHES} onClick={() => add(blankBranch())}>
                {t('stores.branches.add')}
              </Button>
              {fields.length >= MAX_BRANCHES && <Typography.Text type="secondary">{t('stores.branches.limit', { max: MAX_BRANCHES })}</Typography.Text>}
            </Flex>
          </Flex>
        )}
      </Form.List>
      <Button type="primary" htmlType="submit" loading={isLoading} style={{ marginTop: 16 }}>
        {t('stores.branches.save')}
      </Button>
    </Form>
  )
}
