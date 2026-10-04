import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import Stars from '@/components/Stars/Stars'
import Card from '@/components/ui/Card/Card'
import Pagination from '@/components/ui/Pagination/Pagination'
import { formatDate } from '@/shared/format'
import { REVIEWS_PAGE_SIZE, usePartnerReviews } from './publicApi'
import styles from './PartnerPage.module.scss'

/** Customers' reviews on a public profile, newest first, with the partner's replies. */
export default function PartnerReviews({ partner }) {
  const { t, i18n } = useTranslation()
  const [page, setPage] = useState(1)
  const query = usePartnerReviews(partner.slug, page)
  const reviews = query.data?.items ?? []

  return (
    <Card title={t('reviews.publicHeading', { n: partner.reviewCount })}>
      {partner.reviewCount === 0 ? (
        <p className={styles['partner-page__muted']}>{t('reviews.noneYet')}</p>
      ) : (
        <>
          <ul className={styles['partner-page__reviews']}>
            {reviews.map((review) => (
              <li key={review.id} className={styles['partner-page__review']}>
                <div className={styles['partner-page__review-head']}>
                  <Stars value={review.rating} />
                  <span className={styles['partner-page__review-by']}>{review.customerName ?? t('reviews.anonymous')}</span>
                  <time className={styles['partner-page__muted']} dateTime={review.submittedAt}>
                    {formatDate(review.submittedAt, i18n.language)}
                  </time>
                </div>
                {review.text && <p className={styles['partner-page__review-text']}>{review.text}</p>}
                {review.reply && (
                  <div className={styles['partner-page__review-reply']}>
                    <p className={styles['partner-page__review-by']}>{t('reviews.replyFrom', { name: partner.displayName })}</p>
                    <p className={styles['partner-page__review-text']}>{review.reply}</p>
                  </div>
                )}
              </li>
            ))}
          </ul>
          {query.data && <Pagination page={page} pageSize={REVIEWS_PAGE_SIZE} totalCount={query.data.totalCount} onChange={setPage} />}
        </>
      )}
    </Card>
  )
}
