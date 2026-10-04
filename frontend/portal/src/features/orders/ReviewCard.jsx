import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { errorMessage, fieldErrors } from '@/api/errors'
import Stars, { StarInput } from '@/components/Stars/Stars'
import Alert from '@/components/ui/Alert/Alert'
import Button from '@/components/ui/Button/Button'
import Card from '@/components/ui/Card/Card'
import { TextField } from '@/components/ui/Field/Field'
import { formatDate } from '@/shared/format'
import { useReplyToReviewMutation, useSubmitReviewMutation } from './ordersApi'
import styles from './orders.module.scss'

const MAX = 2000

/** The customer rates a completed order: stars (required) and a few words (optional). */
function ReviewForm({ order }) {
  const { t } = useTranslation()
  const [rating, setRating] = useState(0)
  const [text, setText] = useState('')
  const [tried, setTried] = useState(false)
  const [submit, submitting] = useSubmitReviewMutation()
  const api = fieldErrors(t, submitting.error)
  const general = submitting.error && !Object.keys(api).length ? errorMessage(t, submitting.error) : null

  const send = async (event) => {
    event.preventDefault()
    setTried(true)
    if (!rating) return
    await submit({ id: order.id, rating, text: text.trim() || null })
  }

  return (
    <form className={styles['review-form']} onSubmit={send} noValidate>
      <p className={styles['review-form__intro']}>{t('reviews.formIntro', { name: order.partner.displayName })}</p>
      <StarInput label={t('reviews.rating')} value={rating} onChange={setRating} error={api.rating ?? (tried && !rating ? t('reviews.ratingRequired') : undefined)} />
      <TextField
        label={t('reviews.text')}
        optionalText={t('common.optional')}
        hint={t('reviews.textHint')}
        multiline
        maxLength={MAX}
        value={text}
        onChange={(event) => setText(event.target.value)}
        error={api.text}
      />
      {general && <Alert tone="danger" title={general} />}
      <div>
        <Button type="submit" variant="accent" loading={submitting.isLoading}>
          {t('reviews.submit')}
        </Button>
      </div>
    </form>
  )
}

/** The partner answers the review once; the answer shows under it on the public profile. */
function ReplyForm({ order }) {
  const { t } = useTranslation()
  const [text, setText] = useState('')
  const [tried, setTried] = useState(false)
  const [reply, replying] = useReplyToReviewMutation()
  const api = fieldErrors(t, replying.error)
  const general = replying.error && !Object.keys(api).length ? errorMessage(t, replying.error) : null

  const send = async (event) => {
    event.preventDefault()
    setTried(true)
    if (!text.trim()) return
    await reply({ id: order.id, text: text.trim() })
  }

  return (
    <form className={styles['review-form']} onSubmit={send} noValidate>
      <TextField
        label={t('reviews.replyLabel')}
        hint={t('reviews.replyHint')}
        multiline
        maxLength={MAX}
        value={text}
        onChange={(event) => setText(event.target.value)}
        error={api.text ?? (tried && !text.trim() ? t('reviews.replyRequired') : undefined)}
      />
      {general && <Alert tone="danger" title={general} />}
      <div>
        <Button type="submit" variant="primary" loading={replying.isLoading}>
          {t('reviews.reply')}
        </Button>
      </div>
    </form>
  )
}

/** The review of a completed order: the form for the customer, then the review itself and the partner's reply. */
export default function ReviewCard({ order }) {
  const { t, i18n } = useTranslation()
  const review = order.review
  const canReview = order.actions.includes('review')
  if (!review && !canReview) return null

  return (
    <Card title={t('reviews.heading')}>
      {canReview && <ReviewForm order={order} />}
      {review && (
        <div className={styles['review']}>
          <div className={styles['review__head']}>
            <Stars value={review.rating} size={20} />
            <time className={styles['review__date']} dateTime={review.submittedAt}>
              {formatDate(review.submittedAt, i18n.language)}
            </time>
          </div>
          {review.text && <p className={styles['review__text']}>{review.text}</p>}
          {review.isHidden && <Alert tone="info" title={t('reviews.hidden')} />}
          {review.reply && (
            <div className={styles['review__reply']}>
              <p className={styles['review__reply-title']}>{t('reviews.replyFrom', { name: order.partner.displayName })}</p>
              <p className={styles['review__text']}>{review.reply}</p>
            </div>
          )}
          {order.actions.includes('replyReview') && <ReplyForm order={order} />}
        </div>
      )}
    </Card>
  )
}
