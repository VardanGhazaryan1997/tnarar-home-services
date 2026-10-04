import { App, Flex, Form, InputNumber, Modal, Select } from 'antd'
import { useTranslation } from 'react-i18next'
import { cleanNames } from '@/features/catalog/names'
import { useCatalogLanguages } from '@/features/catalog/useCatalogLanguages'
import { applyContentError } from './contentErrors'
import { useCreateFaqMutation, useUpdateFaqMutation } from './contentApi'
import { AUDIENCES } from './audiences'
import LocalizedFields from './LocalizedFields'

const QUESTION_MAX = 300
const ANSWER_MAX = 5000

/** Adds a question (no `faq`) or edits one. New questions start unpublished. */
export default function FaqFormModal({ open, faq, onClose }) {
  const { t } = useTranslation()
  const { message } = App.useApp()
  const [form] = Form.useForm()
  const { languages, defaultCode } = useCatalogLanguages()
  const [createFaq, { isLoading: creating }] = useCreateFaqMutation()
  const [updateFaq, { isLoading: updating }] = useUpdateFaqMutation()

  const save = async (values) => {
    const body = {
      question: cleanNames({ ...(faq?.question ?? {}), ...values.question }),
      answer: cleanNames({ ...(faq?.answer ?? {}), ...values.answer }),
      audience: values.audience,
      sortOrder: values.sortOrder ?? 0,
    }
    try {
      if (faq) await updateFaq({ id: faq.id, ...body }).unwrap()
      else await createFaq(body).unwrap()
      message.success(t(faq ? 'catalog.saved' : 'catalog.created'))
      onClose()
    } catch (error) {
      const text = applyContentError({ form, error, t, defaultCode, localized: ['question', 'answer'] })
      if (text) message.error(text)
    }
  }

  return (
    <Modal
      open={open}
      title={t(faq ? 'content.faqs.edit' : 'content.faqs.add')}
      okText={t('catalog.form.save')}
      okButtonProps={{ htmlType: 'submit', form: 'faq-form', loading: creating || updating }}
      cancelText={t('catalog.form.cancel')}
      onCancel={onClose}
      width={720}
      destroyOnHidden
    >
      <Form
        id="faq-form"
        form={form}
        layout="vertical"
        requiredMark={false}
        onFinish={save}
        initialValues={
          faq
            ? { question: faq.question, answer: faq.answer, audience: faq.audience, sortOrder: faq.sortOrder }
            : { question: {}, answer: {}, audience: 'General', sortOrder: 0 }
        }
      >
        <Flex gap="middle" wrap>
          <Form.Item name="audience" label={t('content.faqs.audience')} style={{ minWidth: 200 }}>
            <Select options={AUDIENCES.map((value) => ({ value, label: t(`content.audience.${value}`) }))} />
          </Form.Item>
          <Form.Item name="sortOrder" label={t('catalog.form.sortOrder')} extra={t('catalog.form.sortOrderHelp')}>
            <InputNumber min={0} precision={0} />
          </Form.Item>
        </Flex>
        <LocalizedFields
          languages={languages}
          defaultCode={defaultCode}
          fields={[
            { name: 'question', label: t('content.faqs.question'), max: QUESTION_MAX, required: true },
            { name: 'answer', label: t('content.faqs.answer'), max: ANSWER_MAX, rows: 5, required: true },
          ]}
        />
      </Form>
    </Modal>
  )
}
