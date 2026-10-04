import { useTranslation } from 'react-i18next'
import Markdown from 'react-markdown'
import { useParams } from 'react-router'
import PageHeader from '@/components/PageHeader/PageHeader'
import QueryState from '@/components/QueryState/QueryState'
import Button from '@/components/ui/Button/Button'
import EmptyState from '@/components/ui/EmptyState/EmptyState'
import { useLocalizedPath } from '@/i18n/hooks'
import { formatDate } from '@/shared/format'
import { usePage } from './publicApi'
import styles from './InfoPage.module.scss'

/** An information page written in the Back Office (terms, privacy, about). The body is Markdown; raw HTML isn't rendered. */
export default function InfoPage() {
  const { t, i18n } = useTranslation()
  const path = useLocalizedPath()
  const { slug } = useParams()
  const query = usePage(slug)

  return (
    <QueryState
      query={query}
      notFound={<EmptyState icon="file" title={t('public.info.notFound')} action={<Button to={path('/')}>{t('notFound.backHome')}</Button>} />}
    >
      {(page) => (
        <article className={styles['info-page']}>
          <title>{t('public.info.documentTitle', { title: page.title })}</title>
          <PageHeader title={page.title} subtitle={t('public.info.updated', { date: formatDate(page.updatedAt, i18n.language) })} />
          <div className={styles['info-page__body']}>
            <Markdown>{page.body}</Markdown>
          </div>
        </article>
      )}
    </QueryState>
  )
}
