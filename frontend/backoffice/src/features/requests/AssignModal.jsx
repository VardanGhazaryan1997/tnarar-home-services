import { Form, Modal, Select, Typography } from 'antd'
import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { errorMessage } from '@/api/errors'
import { useGetPartnersQuery } from '@/features/partners/partnersApi'

/**
 * Sends the request to partners the operator picks among approved ones (searched by name or phone). Partners
 * who already received it are left out. `onConfirm(partnerIds)` returns a promise.
 */
export default function AssignModal({ open, exclude, onConfirm, onCancel }) {
  const { t } = useTranslation()
  const [form] = Form.useForm()
  const [search, setSearch] = useState('')
  const [failure, setFailure] = useState(null)
  const { data, isFetching } = useGetPartnersQuery({ status: 'Approved', search, pageSize: 20 }, { skip: !open })
  const options = (data?.items ?? []).filter((partner) => !exclude.includes(partner.id)).map((partner) => ({ value: partner.id, label: `${partner.displayName} · ${partner.phone}` }))

  const close = () => {
    form.resetFields()
    setFailure(null)
    onCancel()
  }

  const submit = async ({ partnerIds }) => {
    try {
      await onConfirm(partnerIds)
      form.resetFields()
      setFailure(null)
    } catch (error) {
      setFailure(errorMessage(t, error))
    }
  }

  return (
    <Modal
      open={open}
      title={t('requests.assign.title')}
      okText={t('requests.assign.confirm')}
      okButtonProps={{ htmlType: 'submit', form: 'assign-form' }}
      cancelText={t('common.cancel')}
      onCancel={close}
      destroyOnHidden
    >
      <Typography.Paragraph>{t('requests.assign.text')}</Typography.Paragraph>
      <Form id="assign-form" form={form} layout="vertical" onFinish={submit}>
        <Form.Item
          name="partnerIds"
          label={t('requests.assign.partners')}
          rules={[{ required: true, message: t('requests.assign.required') }]}
          validateStatus={failure ? 'error' : undefined}
          help={failure ?? undefined}
        >
          <Select
            mode="multiple"
            showSearch={{ filterOption: false, onSearch: setSearch }}
            loading={isFetching}
            options={options}
            placeholder={t('requests.assign.placeholder')}
            notFoundContent={t('requests.assign.none')}
          />
        </Form.Item>
      </Form>
    </Modal>
  )
}
