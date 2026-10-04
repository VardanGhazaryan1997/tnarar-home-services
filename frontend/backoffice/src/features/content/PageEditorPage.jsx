import { ArrowLeftOutlined } from '@ant-design/icons'
import { Alert, App, Button, Card, Flex, Form, Input, InputNumber, Space, Spin, Switch, Typography } from 'antd'
import { useTranslation } from 'react-i18next'
import { Link, useNavigate, useParams } from 'react-router'
import { errorMessage } from '@/api/errors'
import { cleanNames, localizedName } from '@/features/catalog/names'
import { useCatalogLanguages } from '@/features/catalog/useCatalogLanguages'
import { applyContentError } from './contentErrors'
import { useCreatePageMutation, useGetPageQuery, useSetPagePublishedMutation, useUpdatePageMutation } from './contentApi'
import LocalizedFields from './LocalizedFields'
import PublishedTag from './PublishedTag'

const TITLE_MAX = 200
const BODY_MAX = 50000

function PageForm({ page }) {
  const { t, i18n } = useTranslation()
  const { message } = App.useApp()
  const navigate = useNavigate()
  const [form] = Form.useForm()
  const { languages, defaultCode } = useCatalogLanguages()
  const [createPage, { isLoading: creating }] = useCreatePageMutation()
  const [updatePage, { isLoading: updating }] = useUpdatePageMutation()
  const [setPublished, { isLoading: publishing }] = useSetPagePublishedMutation()

  const save = async (values) => {
    const body = {
      slug: values.slug.trim(),
      // Keep translations for languages that aren't active right now.
      title: cleanNames({ ...(page?.title ?? {}), ...values.title }),
      body: cleanNames({ ...(page?.body ?? {}), ...values.body }),
      showInFooter: Boolean(values.showInFooter),
      sortOrder: values.sortOrder ?? 0,
    }
    try {
      if (page) {
        await updatePage({ id: page.id, ...body }).unwrap()
        message.success(t('catalog.saved'))
      } else {
        const created = await createPage(body).unwrap()
        message.success(t('catalog.created'))
        navigate(`/content/pages/${created.id}`, { replace: true })
      }
    } catch (error) {
      const text = applyContentError({ form, error, t, defaultCode, localized: ['title', 'body'] })
      if (text) message.error(text)
    }
  }

  const togglePublished = async () => {
    try {
      await setPublished({ id: page.id, published: !page.isPublished }).unwrap()
      message.success(t(page.isPublished ? 'content.unpublished' : 'content.publishedDone'))
    } catch (failure) {
      message.error(errorMessage(t, failure))
    }
  }

  return (
    <Flex vertical gap="middle">
      <Link to="/content/pages">
        <ArrowLeftOutlined /> {t('content.pages.back')}
      </Link>
      <Flex justify="space-between" align="center" wrap gap="middle">
        <Space align="center" wrap>
          <Typography.Title level={1} style={{ margin: 0 }}>
            {page ? localizedName(page.title, i18n.resolvedLanguage, defaultCode) : t('content.pages.newTitle')}
          </Typography.Title>
          {page && <PublishedTag published={page.isPublished} />}
        </Space>
        <Space wrap>
          {page && (
            <Button onClick={togglePublished} loading={publishing}>
              {t(page.isPublished ? 'content.unpublish' : 'content.publish')}
            </Button>
          )}
          <Button type="primary" htmlType="submit" form="page-form" loading={creating || updating}>
            {t('catalog.form.save')}
          </Button>
        </Space>
      </Flex>
      {page?.isPublished && <Alert type="info" showIcon title={t('content.pages.liveNotice')} />}

      <Form
        id="page-form"
        form={form}
        layout="vertical"
        requiredMark={false}
        onFinish={save}
        initialValues={
          page
            ? { slug: page.slug, title: page.title, body: page.body, showInFooter: page.showInFooter, sortOrder: page.sortOrder }
            : { slug: '', title: {}, body: {}, showInFooter: false, sortOrder: 0 }
        }
      >
        <Card title={t('content.pages.settings')} style={{ marginBottom: 16 }}>
          <Flex gap="middle" wrap>
            <Form.Item
              name="slug"
              label={t('content.form.slug')}
              extra={t('content.form.slugHelp')}
              rules={[
                { required: true, message: t('catalog.form.slugRequired') },
                { pattern: /^[a-z0-9]+(-[a-z0-9]+)*$/, message: t('errors.slug.invalid') },
              ]}
              style={{ minWidth: 260, flex: 1 }}
            >
              <Input prefix="/pages/" maxLength={64} />
            </Form.Item>
            <Form.Item name="sortOrder" label={t('catalog.form.sortOrder')} extra={t('catalog.form.sortOrderHelp')}>
              <InputNumber min={0} precision={0} />
            </Form.Item>
            <Form.Item name="showInFooter" label={t('content.form.showInFooter')} valuePropName="checked">
              <Switch />
            </Form.Item>
          </Flex>
        </Card>
        <Card title={t('content.pages.text')}>
          <Typography.Paragraph type="secondary">{t('content.form.markdownHelp')}</Typography.Paragraph>
          <LocalizedFields
            languages={languages}
            defaultCode={defaultCode}
            fields={[
              { name: 'title', label: t('content.form.title'), max: TITLE_MAX, required: true },
              { name: 'body', label: t('content.form.body'), max: BODY_MAX, rows: 16 },
            ]}
          />
        </Card>
      </Form>
    </Flex>
  )
}

/** Creates a page (/content/pages/new) or edits one (/content/pages/:id). */
export default function PageEditorPage() {
  const { id } = useParams()
  const { t } = useTranslation()
  const { data: page, isLoading, error } = useGetPageQuery(id, { skip: !id })

  if (!id) return <PageForm />
  if (isLoading) return <Spin />
  if (error) {
    return (
      <Flex vertical gap="middle">
        <Link to="/content/pages">
          <ArrowLeftOutlined /> {t('content.pages.back')}
        </Link>
        <Alert type="error" showIcon title={errorMessage(t, error)} />
      </Flex>
    )
  }
  // Re-created when another page opens, so the form starts from that page's values.
  return <PageForm key={page.id} page={page} />
}
