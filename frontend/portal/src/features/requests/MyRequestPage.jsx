import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Link, useLocation, useParams } from 'react-router'
import { errorMessage } from '@/api/errors'
import PageHeader from '@/components/PageHeader/PageHeader'
import QueryState from '@/components/QueryState/QueryState'
import Alert from '@/components/ui/Alert/Alert'
import Button from '@/components/ui/Button/Button'
import Card from '@/components/ui/Card/Card'
import EmptyState from '@/components/ui/EmptyState/EmptyState'
import { TextField } from '@/components/ui/Field/Field'
import Modal from '@/components/ui/Modal/Modal'
import Tag from '@/components/ui/Tag/Tag'
import StartChatButton from '@/features/chat/StartChatButton'
import RequestOffers from '@/features/offers/RequestOffers'
import { useLocalizedPath } from '@/i18n/hooks'
import { formatDate, formatDateTime } from '@/shared/format'
import { MediaGallery, RequestFacts, RequestStatusTag } from './components/RequestParts'
import RequestLines from './components/RequestLines'
import { useCancelMyRequestMutation, useGetMyRequestQuery } from './requestsApi'
import styles from './requests.module.scss'

/** One of the customer's requests: details, who has it, the offers, and cancelling. */
export default function MyRequestPage() {
  const { t, i18n } = useTranslation()
  const { id } = useParams()
  const location = useLocation()
  const path = useLocalizedPath()
  const query = useGetMyRequestQuery(id, { refetchOnMountOrArgChange: true })
  const [cancelOpen, setCancelOpen] = useState(false)
  const [reason, setReason] = useState('')
  const [cancelRequest, cancelling] = useCancelMyRequestMutation()
  const back = { to: path('/requests'), label: t('requests.mineTitle') }

  const confirmCancel = async () => {
    const result = await cancelRequest({ id, reason: reason.trim() })
    if (result.data) setCancelOpen(false)
  }

  return (
    <QueryState
      query={query}
      notFound={
        <>
          <PageHeader title={t('requests.notFound')} back={back} />
          <EmptyState icon="requests" title={t('requests.notFound')} description={t('requests.notFoundText')} />
        </>
      }
    >
      {(request) => (
        <>
          <PageHeader
            title={request.place.categoryName}
            subtitle={t('requests.createdOn', { date: formatDate(request.createdAt, i18n.language) })}
            back={back}
            actions={
              <>
                {request.kind === 'Direct' && <Tag tone="info">{t('requests.kind.Direct')}</Tag>}
                <RequestStatusTag status={request.status} />
              </>
            }
          />
          <div className={styles['request-page']}>
            <div className={styles['request-page__main']}>
              {location.state?.created && <Alert tone="success" title={t('requests.created')} />}
              {request.findingPartners && request.status === 'Open' && <Alert tone="info" title={t('requests.findingPartnersLong')} />}
              {request.status === 'Cancelled' && (
                <Alert tone="warning" title={t('requests.cancelledOn', { date: formatDateTime(request.cancelledAt, i18n.language) })}>
                  {request.cancelReason}
                </Alert>
              )}
              <RequestOffers request={request} />
              <Card title={t('requests.details')}>
                <RequestFacts request={request} showBudget />
                <p className={styles['request-page__text']}>{request.description}</p>
                <MediaGallery files={request.media} />
              </Card>
              {request.lines?.length > 0 && (
                <Card
                  title={t('requests.lines.title')}
                  actions={
                    request.estimateId && (
                      <Link to={path(`/estimates/${request.estimateId}`)} className={styles['request-page__muted']}>
                        {t('requests.lines.openEstimate')}
                      </Link>
                    )
                  }
                >
                  <RequestLines lines={request.lines} showEstimate />
                </Card>
              )}
            </div>
            <aside className={styles['request-page__side']}>
              <Card title={t('requests.whoHasIt')}>
                <p className={styles['request-page__muted']}>{t('requests.sentSummary', { sent: request.sentTo, responded: request.partners.filter((p) => p.status === 'Responded').length })}</p>
                {request.partners.length > 0 && (
                  <ul className={styles['request-page__partners']}>
                    {request.partners.map((partner) => (
                      <li key={partner.partnerId} className={styles['request-page__partner']}>
                        {partner.slug ? <Link to={path(`/partners/${partner.slug}`)}>{partner.displayName}</Link> : <span>{partner.displayName}</span>}
                        <span className={styles['request-page__actions']}>
                          <Tag tone={partner.status === 'Responded' ? 'success' : 'neutral'}>{t(`inbox.status.${partner.status}`)}</Tag>
                          {partner.status !== 'Declined' && <StartChatButton requestId={request.id} partnerId={partner.partnerId} variant="ghost" size="sm" />}
                        </span>
                      </li>
                    ))}
                  </ul>
                )}
              </Card>
              {request.status === 'Open' && (
                <Button variant="danger" block onClick={() => setCancelOpen(true)}>
                  {t('requests.cancel')}
                </Button>
              )}
            </aside>
          </div>
          <Modal
            open={cancelOpen}
            title={t('requests.cancelTitle')}
            onClose={() => setCancelOpen(false)}
            footer={
              <>
                <Button variant="secondary" onClick={() => setCancelOpen(false)}>
                  {t('common.back')}
                </Button>
                <Button variant="danger" loading={cancelling.isLoading} onClick={confirmCancel}>
                  {t('requests.cancelConfirm')}
                </Button>
              </>
            }
          >
            <p>{t('requests.cancelText')}</p>
            <TextField
              label={t('requests.cancelReason')}
              optionalText={t('common.optional')}
              multiline
              maxLength={500}
              value={reason}
              onChange={(event) => setReason(event.target.value)}
            />
            {cancelling.error && <Alert tone="danger" title={errorMessage(t, cancelling.error)} />}
          </Modal>
        </>
      )}
    </QueryState>
  )
}
