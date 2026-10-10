import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { useNavigate, useParams } from 'react-router'
import { errorMessage } from '@/api/errors'
import PageHeader from '@/components/PageHeader/PageHeader'
import QueryState from '@/components/QueryState/QueryState'
import Alert from '@/components/ui/Alert/Alert'
import Button from '@/components/ui/Button/Button'
import EmptyState from '@/components/ui/EmptyState/EmptyState'
import { TextField } from '@/components/ui/Field/Field'
import Icon from '@/components/ui/Icon/Icon'
import { useLocalizedPath } from '@/i18n/hooks'
import ConfirmDialog from './ConfirmDialog'
import { fromEstimate, isRoomValid, sameDraft, toRequest } from './draft'
import EstimateEditor from './EstimateEditor'
import { useDeleteEstimateMutation, useMyEstimate, useShareEstimateMutation, useUpdateEstimateMutation } from './estimatorApi'
import styles from './estimator.module.scss'

/** One of the user's saved estimates: edit and save it, share a read-only link, or delete it. */
export default function SavedEstimatePage() {
  const { t } = useTranslation()
  const path = useLocalizedPath()
  const { id } = useParams()
  const query = useMyEstimate(id)

  return (
    <div className={styles['estimator-page']}>
      <QueryState
        query={query}
        notFound={
          <EmptyState
            icon="file"
            title={t('estimator.saved.notFound')}
            action={<Button to={path('/estimates')}>{t('estimator.list.title')}</Button>}
          />
        }
      >
        {(estimate) => <SavedEstimate key={estimate.id} estimate={estimate} />}
      </QueryState>
    </div>
  )
}

function SavedEstimate({ estimate }) {
  const { t } = useTranslation()
  const path = useLocalizedPath()
  const navigate = useNavigate()
  // The estimate as last saved, to tell whether there's anything to save.
  const [saved, setSaved] = useState(() => fromEstimate(estimate))
  const [draft, setDraft] = useState(saved)
  const [problem, setProblem] = useState(null)
  const [justSaved, setJustSaved] = useState(false)
  const [copied, setCopied] = useState(false)
  const [confirmDelete, setConfirmDelete] = useState(false)
  const [update, updating] = useUpdateEstimateMutation()
  const [share, sharing] = useShareEstimateMutation()
  const [remove, removing] = useDeleteEstimateMutation()
  const changed = !sameDraft(draft, saved)
  const shareUrl = estimate.shareToken ? `${window.location.origin}${path(`/estimates/shared/${estimate.shareToken}`)}` : null

  const change = (next) => {
    setDraft(next)
    setJustSaved(false)
  }

  const save = async () => {
    if (!draft.rooms.every(isRoomValid)) return setProblem('rooms')
    setProblem(null)
    const named = { ...draft, title: draft.title.trim() || t('estimator.editor.defaultTitle') }
    try {
      await update({ id: estimate.id, ...toRequest(named) }).unwrap()
      setDraft(named)
      setSaved(named)
      setJustSaved(true)
    } catch {
      // The error shows from the mutation state.
    }
    return undefined
  }

  const copy = async () => {
    try {
      await navigator.clipboard.writeText(shareUrl)
      setCopied(true)
    } catch {
      setCopied(false)
    }
  }

  const deleteEstimate = async () => {
    try {
      await remove(estimate.id).unwrap()
      navigate(path('/estimates'), { replace: true })
    } catch {
      setConfirmDelete(false)
    }
  }

  const actions = (
    <>
      <Button variant="accent" block icon={<Icon name="check" />} disabled={!changed} loading={updating.isLoading} onClick={save}>
        {t('estimator.saved.save')}
      </Button>
      <div className={styles['estimate-share']}>
        {shareUrl ? (
          <>
            <TextField label={t('estimator.saved.link')} value={shareUrl} readOnly onFocus={(event) => event.target.select()} />
            <div className={styles['estimate-share__buttons']}>
              <Button variant="secondary" size="sm" onClick={copy}>
                {copied ? t('estimator.saved.copied') : t('estimator.saved.copy')}
              </Button>
              <Button variant="ghost" size="sm" loading={sharing.isLoading} onClick={() => share({ id: estimate.id, share: false })}>
                {t('estimator.saved.stopSharing')}
              </Button>
            </div>
          </>
        ) : (
          <Button variant="secondary" block icon={<Icon name="send" />} loading={sharing.isLoading} onClick={() => share({ id: estimate.id, share: true })}>
            {t('estimator.saved.share')}
          </Button>
        )}
        <p className={styles['estimate-summary__note']}>{t('estimator.saved.shareHint')}</p>
      </div>
      <Button variant="ghost" block icon={<Icon name="trash" />} onClick={() => setConfirmDelete(true)}>
        {t('estimator.saved.delete')}
      </Button>
    </>
  )

  return (
    <>
      <PageHeader title={saved.title} back={{ to: path('/estimates'), label: t('estimator.list.title') }} />
      <EstimateEditor
        draft={draft}
        onChange={change}
        actions={actions}
        notice={
          <>
            {justSaved && !changed && <Alert tone="success" title={t('estimator.saved.saved')} />}
            {problem === 'rooms' && <Alert tone="danger" title={t('estimator.errors.fixRooms')} />}
            {updating.isError && <Alert tone="danger" title={errorMessage(t, updating.error)} />}
            {sharing.isError && <Alert tone="danger" title={errorMessage(t, sharing.error)} />}
            {removing.isError && <Alert tone="danger" title={errorMessage(t, removing.error)} />}
          </>
        }
      />
      <ConfirmDialog
        open={confirmDelete}
        title={t('estimator.saved.deleteTitle', { title: saved.title })}
        text={t('estimator.saved.deleteText')}
        confirmLabel={t('estimator.saved.delete')}
        loading={removing.isLoading}
        onClose={() => setConfirmDelete(false)}
        onConfirm={deleteEstimate}
      />
    </>
  )
}
