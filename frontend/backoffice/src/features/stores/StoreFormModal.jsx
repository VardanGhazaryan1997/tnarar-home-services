import { Form, Modal } from 'antd'
import { useTranslation } from 'react-i18next'
import StoreForm from './StoreForm'

/** Adds a store: the store form in a dialog. `onSave(body)` runs the mutation; `onDone(saved)` after it succeeded. */
export default function StoreFormModal({ open, onSave, onDone, onClose }) {
  const { t } = useTranslation()
  const [form] = Form.useForm()

  return (
    <Modal
      open={open}
      title={t('stores.add')}
      okText={t('catalog.form.save')}
      cancelText={t('catalog.form.cancel')}
      onOk={() => form.submit()}
      onCancel={onClose}
      destroyOnHidden
      width={640}
    >
      <StoreForm form={form} store={null} onSave={onSave} onDone={onDone} />
    </Modal>
  )
}
