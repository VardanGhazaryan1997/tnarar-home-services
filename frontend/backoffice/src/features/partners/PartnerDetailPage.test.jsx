import { screen, waitFor, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { http, HttpResponse } from 'msw'
import i18n from '@/i18n'
import hy from '@/i18n/locales/hy/common.json'
import { problem, signedInAs, staffMember } from '@/test/auth'
import { partnerDetail } from '@/test/partners'
import { renderRoute } from '@/test/renderWithProviders'
import { server } from '@/test/server'

const t = (key, options) => i18n.t(key, options)

function captureDecision(respond = () => HttpResponse.json(partnerDetail({ status: 'Approved' }))) {
  const requests = []
  server.use(
    http.post('*/api/v1/admin/partners/:id/:action', async ({ request, params }) => {
      requests.push({ action: params.action, body: await request.json().catch(() => null) })
      return respond()
    }),
  )
  return requests
}

async function openPartner(detail = partnerDetail()) {
  server.use(http.get('*/api/v1/admin/partners/:id', () => HttpResponse.json(detail)))
  renderRoute('/partners/partner-aram')
  return screen.findByRole('heading', { level: 1, name: detail.profile.displayName })
}

describe('Partner detail page', () => {
  it('shows what the partner offers, where, their work, documents, owner and history', async () => {
    await openPartner()

    expect(await screen.findByText('Սանտեխնիկա')).toBeInTheDocument()
    expect(screen.getByText('Սալիկապատում')).toBeInTheDocument()
    expect(await screen.findByText('Երևան · Կենտրոն')).toBeInTheDocument()
    expect(screen.getByText(t('partners.detail.wholeCity', { city: 'Մասիս' }))).toBeInTheDocument()
    expect(screen.getByRole('img', { name: 'Լոգարան' })).toHaveAttribute('src', 'https://storage.test/file-1/thumbnail')
    expect(screen.getByRole('link', { name: /boiler\.mp4/ })).toHaveAttribute('href', 'https://storage.test/file-2')
    expect(screen.getByRole('link', { name: /Լիցենզիա/ })).toHaveAttribute('href', 'https://storage.test/file-3')
    expect(screen.getByText('aram@example.com')).toBeInTheDocument()
    expect(screen.getByText(`${hy.partners.status.Draft} → ${hy.partners.status.UnderReview}`)).toBeInTheDocument()
    expect(screen.getByText(t('partners.detail.years', { count: 12 }))).toBeInTheDocument()
  })

  it('approves a profile under review', async () => {
    const requests = captureDecision()
    const user = userEvent.setup()
    await openPartner()

    await user.click(screen.getByRole('button', { name: hy.partners.decisions.approve.button }))
    const dialog = await screen.findByRole('dialog')
    expect(within(dialog).getByText(hy.partners.decisions.approve.text)).toBeInTheDocument()
    await user.click(within(dialog).getByRole('button', { name: hy.partners.decisions.approve.confirm }))

    await waitFor(() => expect(requests).toEqual([{ action: 'approve', body: null }]))
    expect(await screen.findByText(hy.partners.decisions.approve.done)).toBeInTheDocument()
  })

  it('needs a comment to ask for changes', async () => {
    const requests = captureDecision(() => HttpResponse.json(partnerDetail({ status: 'NeedsChanges' })))
    const user = userEvent.setup()
    await openPartner()

    await user.click(screen.getByRole('button', { name: hy.partners.decisions.requestChanges.button }))
    const dialog = await screen.findByRole('dialog')
    await user.click(within(dialog).getByRole('button', { name: hy.partners.decisions.requestChanges.confirm }))
    expect(await within(dialog).findByText(hy.errors.comment.required)).toBeInTheDocument()

    await user.type(within(dialog).getByLabelText(hy.partners.decisions.comment), '  Ավելացրեք լիցենզիան ')
    await user.click(within(dialog).getByRole('button', { name: hy.partners.decisions.requestChanges.confirm }))

    await waitFor(() => expect(requests).toEqual([{ action: 'request-changes', body: { comment: 'Ավելացրեք լիցենզիան' } }]))
  })

  it('shows server validation errors on the comment', async () => {
    captureDecision(() => HttpResponse.json({ status: 400, code: 'validation_failed', errors: { comment: ['comment.too_long'] } }, { status: 400 }))
    const user = userEvent.setup()
    await openPartner()

    await user.click(screen.getByRole('button', { name: hy.partners.decisions.reject.button }))
    const dialog = await screen.findByRole('dialog')
    await user.type(within(dialog).getByLabelText(hy.partners.decisions.comment), 'Ոչ')
    await user.click(within(dialog).getByRole('button', { name: hy.partners.decisions.reject.confirm }))

    expect(await within(dialog).findByText(hy.errors.comment.too_long)).toBeInTheDocument()
  })

  it('explains when a decision is no longer possible', async () => {
    captureDecision(() => problem(422, 'partner.not_under_review'))
    const user = userEvent.setup()
    await openPartner()

    await user.click(screen.getByRole('button', { name: hy.partners.decisions.approve.button }))
    await user.click(within(await screen.findByRole('dialog')).getByRole('button', { name: hy.partners.decisions.approve.confirm }))

    expect(await screen.findByText(hy.errors.partner.not_under_review)).toBeInTheDocument()
  })

  it('closes the dialog without deciding when cancelled', async () => {
    const requests = captureDecision()
    const user = userEvent.setup()
    await openPartner()

    await user.click(screen.getByRole('button', { name: hy.partners.decisions.reject.button }))
    await user.click(within(await screen.findByRole('dialog')).getByRole('button', { name: hy.catalog.form.cancel }))

    expect(requests).toEqual([])
  })

  it('shows sparse profiles and unknown names safely', async () => {
    const detail = partnerDetail({
      yearsOfExperience: null,
      slug: null,
      about: '',
      submittedAt: null,
      categoryIds: ['cat-gone'],
      areas: [
        { cityId: 'city-gone', districtId: null },
        { cityId: 'city-yerevan', districtId: 'district-gone' },
      ],
      workExamples: [],
      documents: [],
    })
    detail.owner = { ...detail.owner, fullName: null, email: null, isBlocked: true }
    detail.history = [{ ...detail.history[0], actorName: null, comment: 'Ուղարկված է' }]
    await openPartner(detail)

    expect(screen.getByText(hy.partners.detail.unknownCategory)).toBeInTheDocument()
    expect(await screen.findByText(hy.partners.detail.unknownPlace)).toBeInTheDocument()
    expect(await screen.findByText('Երևան')).toBeInTheDocument()
    expect(screen.getByText(hy.partners.detail.noWorkExamples)).toBeInTheDocument()
    expect(screen.getByText(hy.partners.detail.noDocuments)).toBeInTheDocument()
    expect(screen.getByText(hy.partners.detail.blocked)).toBeInTheDocument()
    expect(screen.getByText(new RegExp(hy.partners.detail.system))).toBeInTheDocument()
    expect(screen.getByText('Ուղարկված է')).toBeInTheDocument()
    expect(screen.getAllByText('—').length).toBeGreaterThanOrEqual(5)
  })

  it('offers suspend for approved partners and reinstate for suspended ones', async () => {
    await openPartner(partnerDetail({ status: 'Approved' }))
    expect(screen.getByRole('button', { name: hy.partners.decisions.suspend.button })).toBeInTheDocument()
    expect(screen.queryByRole('button', { name: hy.partners.decisions.approve.button })).not.toBeInTheDocument()
  })

  it('shows the staff comment while a profile is suspended', async () => {
    await openPartner(partnerDetail({ status: 'Suspended', reviewComment: 'Բողոքներ' }))

    expect(screen.getByRole('button', { name: hy.partners.decisions.reinstate.button })).toBeInTheDocument()
    expect(screen.getByText('Բողոքներ')).toBeInTheDocument()
  })

  it('shows no decisions to staff who can only view', async () => {
    signedInAs(staffMember(['partners.view']))
    await openPartner()

    expect(screen.queryByRole('button', { name: hy.partners.decisions.approve.button })).not.toBeInTheDocument()
  })

  it('says when the partner does not exist', async () => {
    server.use(http.get('*/api/v1/admin/partners/:id', () => problem(404, 'partner.not_found')))
    renderRoute('/partners/missing')

    expect(await screen.findByText(hy.errors.partner.not_found)).toBeInTheDocument()
    expect(screen.getByRole('link', { name: new RegExp(hy.partners.detail.back) })).toHaveAttribute('href', '/partners')
  })
})
