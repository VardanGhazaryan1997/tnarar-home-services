import { App } from 'antd'
import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { errorMessage } from '@/api/errors'
import ReasonModal from '@/components/ReasonModal/ReasonModal'
import { useHideReviewMutation, useRestoreReviewMutation } from './reviewsApi'

/**
 * Hiding (with a reason) and restoring reviews, for the reviews page and the order page. Returns `hide(review)`,
 * `restore(review)` and the dialog to render.
 */
export function useReviewModeration() {
  const { t } = useTranslation()
  const { message } = App.useApp()
  const [hiding, setHiding] = useState(null)
  const [hideReview] = useHideReviewMutation()
  const [restoreReview] = useRestoreReviewMutation()

  const restore = async (review) => {
    try {
      await restoreReview(review.id).unwrap()
      message.success(t('reviews.restore.done'))
    } catch (failure) {
      message.error(errorMessage(t, failure))
    }
  }

  const confirmHide = async (reason) => {
    try {
      await hideReview({ id: hiding.id, reason }).unwrap()
      message.success(t('reviews.hide.done'))
    } catch (failure) {
      if (failure?.status === 400) throw failure
      message.error(errorMessage(t, failure))
    }
    setHiding(null)
  }

  const dialog = (
    <ReasonModal
      open={Boolean(hiding)}
      title={t('reviews.hide.title')}
      text={t('reviews.hide.text')}
      label={t('reviews.hide.reason')}
      okText={t('reviews.hide.confirm')}
      danger
      max={500}
      onConfirm={confirmHide}
      onCancel={() => setHiding(null)}
    />
  )

  return { hide: setHiding, restore, dialog }
}
