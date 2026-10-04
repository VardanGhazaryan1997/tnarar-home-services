import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { useLocation, useParams } from 'react-router'
import { errorMessage } from '@/api/errors'
import PageHeader from '@/components/PageHeader/PageHeader'
import QueryState from '@/components/QueryState/QueryState'
import Alert from '@/components/ui/Alert/Alert'
import Button from '@/components/ui/Button/Button'
import Card from '@/components/ui/Card/Card'
import EmptyState from '@/components/ui/EmptyState/EmptyState'
import { TextField } from '@/components/ui/Field/Field'
import Icon from '@/components/ui/Icon/Icon'
import Modal from '@/components/ui/Modal/Modal'
import Tag from '@/components/ui/Tag/Tag'
import StartChatButton from '@/features/chat/StartChatButton'
import MyOffersOnRequest from '@/features/offers/MyOffersOnRequest'
import { useLocalizedPath } from '@/i18n/hooks'
import { formatDateTime } from '@/shared/format'
import { MediaGallery, RecipientStatusTag, RequestFacts } from './components/RequestParts'
import { useDeclineInboxRequestMutation, useGetInboxRequestQuery } from './requestsApi'
import styles from './requests.module.scss'

/** A request the partner received: details (never the customer's budget), offering and declining. */
export default function InboxRequestPage() {
  const { t, i18n } = useTranslation()
  const { id } = useParams()
  const location = useLocation()
  const path = useLocalizedPath()
  const query = useGetInboxRequestQuery(id, { refetchOnMountOrArgChange: true })
  const [declineOpen, setDeclineOpen] = useState(false)
  const [reason, setReason] = useState('')
  const [decline, declining] = useDeclineInboxRequestMutation()
  const back = { to: path('/inbox'), label: t('inbox.title') }

  const confirmDecline = async () => {
    const result = await decline({ id, reason: reason.trim() })
    if (result.data) setDeclineOpen(false)
  }

  return (
    <QueryState
      query={query}
      notFound={
        <>
          <PageHeader title={t('requests.notFound')} back={back} />
          <EmptyState icon="inbox" title={t('requests.notFound')} description={t('inbox.notFoundText')} />
        </>
      }
    >
      {(request) => {
        const canAct = request.requestStatus === 'Open' && request.myStatus !== 'Declined'
        return (
          <>
            <PageHeader
              title={request.place.categoryName}
              subtitle={t('inbox.receivedOn', { date: formatDateTime(request.sentAt, i18n.language) })}
              back={back}
              actions={
                <>
                  {request.kind === 'Direct' && <Tag tone="info">{t('inbox.direct')}</Tag>}
                  <RecipientStatusTag status={request.myStatus} />
                </>
              }
            />
            <div className={styles['request-page']}>
              <div className={styles['request-page__main']}>
                {location.state?.offerSent && <Alert tone="success" title={t('offers.sent')} />}
                {request.requestStatus !== 'Open' && <Alert tone="warning" title={t(`inbox.requestEnded.${request.requestStatus}`)} />}
                {request.myStatus === 'Declined' && <Alert tone="info" title={t('inbox.youDeclined')} />}
                <Card title={t('requests.details')}>
                  <RequestFacts request={request} />
                  <p className={styles['request-page__text']}>{request.description}</p>
                  <MediaGallery files={request.media} />
                </Card>
                <MyOffersOnRequest requestId={request.id} />
              </div>
              <aside className={styles['request-page__side']}>
                <Card title={t('inbox.customer')}>
                  <p>{request.customerFirstName ?? t('inbox.customerNoName')}</p>
                  <p className={styles['request-page__muted']}>{t('inbox.contactAfterOrder')}</p>
                  {canAct && <StartChatButton requestId={request.id} label={t('chat.askCustomer')} block />}
                </Card>
                {canAct && (
                  <div className={styles['request-page__actions']}>
                    <Button to={path(`/inbox/${request.id}/offer`)} variant="accent" block icon={<Icon name="send" />}>
                      {t('inbox.sendOffer')}
                    </Button>
                    {request.myStatus !== 'Responded' && (
                      <Button variant="secondary" block onClick={() => setDeclineOpen(true)}>
                        {t('inbox.decline')}
                      </Button>
                    )}
                  </div>
                )}
              </aside>
            </div>
            <Modal
              open={declineOpen}
              title={t('inbox.declineTitle')}
              onClose={() => setDeclineOpen(false)}
              footer={
                <>
                  <Button variant="secondary" onClick={() => setDeclineOpen(false)}>
                    {t('common.back')}
                  </Button>
                  <Button variant="danger" loading={declining.isLoading} onClick={confirmDecline}>
                    {t('inbox.declineConfirm')}
                  </Button>
                </>
              }
            >
              <p>{t('inbox.declineText')}</p>
              <TextField
                label={t('inbox.declineReason')}
                optionalText={t('common.optional')}
                multiline
                maxLength={500}
                value={reason}
                onChange={(event) => setReason(event.target.value)}
              />
              {declining.error && <Alert tone="danger" title={errorMessage(t, declining.error)} />}
            </Modal>
          </>
        )
      }}
    </QueryState>
  )
}
