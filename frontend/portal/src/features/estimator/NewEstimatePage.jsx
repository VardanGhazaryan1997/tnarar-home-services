import { useEffect, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { useLocation, useNavigate } from 'react-router'
import { errorMessage } from '@/api/errors'
import PageHeader from '@/components/PageHeader/PageHeader'
import Alert from '@/components/ui/Alert/Alert'
import Button from '@/components/ui/Button/Button'
import Icon from '@/components/ui/Icon/Icon'
import { useLocalizedPath } from '@/i18n/hooks'
import ConfirmDialog from './ConfirmDialog'
import { clearLocalDraft, emptyDraft, isRoomValid, loadLocalDraft, saveLocalDraft, toRequest } from './draft'
import EstimateEditor from './EstimateEditor'
import { useCreateEstimateMutation } from './estimatorApi'
import { useSignedIn } from './useSignedIn'
import styles from './estimator.module.scss'

/**
 * A new estimate, room by room. Anyone can make one: it's kept in this browser until a signed-in user saves it to
 * their account (visitors sign in and come back to it).
 */
export default function NewEstimatePage() {
  const { t } = useTranslation()
  const path = useLocalizedPath()
  const location = useLocation()
  const navigate = useNavigate()
  const signedIn = useSignedIn()
  const [draft, setDraft] = useState(() => loadLocalDraft() ?? emptyDraft())
  const [problem, setProblem] = useState(null)
  const [confirmReset, setConfirmReset] = useState(false)
  const [create, creating] = useCreateEstimateMutation()

  useEffect(() => saveLocalDraft(draft), [draft])

  const save = async () => {
    if (!draft.rooms.every(isRoomValid)) return setProblem('rooms')
    setProblem(null)
    try {
      const saved = await create(toRequest({ ...draft, title: draft.title.trim() || t('estimator.editor.defaultTitle') })).unwrap()
      clearLocalDraft()
      navigate(path(`/estimates/${saved.id}`), { replace: true })
    } catch {
      // The error shows from the mutation state.
    }
    return undefined
  }

  const actions = (
    <>
      {signedIn ? (
        <Button variant="accent" block icon={<Icon name="check" />} loading={creating.isLoading} onClick={save}>
          {t('estimator.editor.save')}
        </Button>
      ) : (
        <>
          <Button variant="accent" block to={path('/sign-in')} state={{ from: location }}>
            {t('estimator.editor.signInToSave')}
          </Button>
          <p className={styles['estimate-summary__note']}>{t('estimator.editor.keptHere')}</p>
        </>
      )}
      <Button variant="ghost" block disabled={draft.rooms.length === 0} onClick={() => setConfirmReset(true)}>
        {t('estimator.editor.startOver')}
      </Button>
    </>
  )

  return (
    <div className={styles['estimator-page']}>
      <PageHeader
        title={t('estimator.editor.newTitle')}
        subtitle={t('estimator.editor.subtitle')}
        back={signedIn ? { to: path('/estimates'), label: t('estimator.list.title') } : undefined}
      />
      <EstimateEditor
        draft={draft}
        onChange={setDraft}
        actions={actions}
        notice={
          <>
            {problem === 'rooms' && <Alert tone="danger" title={t('estimator.errors.fixRooms')} />}
            {creating.isError && <Alert tone="danger" title={errorMessage(t, creating.error)} />}
          </>
        }
      />
      <ConfirmDialog
        open={confirmReset}
        title={t('estimator.editor.startOverTitle')}
        text={t('estimator.editor.startOverText')}
        confirmLabel={t('estimator.editor.startOver')}
        onClose={() => setConfirmReset(false)}
        onConfirm={() => {
          setDraft(emptyDraft())
          setConfirmReset(false)
        }}
      />
    </div>
  )
}
