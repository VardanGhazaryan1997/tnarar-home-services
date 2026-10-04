import { screen, waitFor, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { http, HttpResponse } from 'msw'
import hy from '@/i18n/locales/hy/common.json'
import { problem, signedInAs, staffMember } from '@/test/auth'
import { paymentRow } from '@/test/operations'
import { page } from '@/test/partners'
import { renderRoute } from '@/test/renderWithProviders'
import { server } from '@/test/server'

// Several dialogs in one test: slow machines need more than the default 60 s.
const SLOW = 180_000

describe('Payments page', () => {
  it('opens on disputes and filters by status', async () => {
    const requests = []
    server.use(
      http.get('*/api/v1/admin/payments', ({ request }) => {
        requests.push(Object.fromEntries(new URL(request.url).searchParams))
        return HttpResponse.json(page([paymentRow({ resolutionNote: 'Ստուգված' })], { totalCount: 25 }))
      }),
    )
    const user = userEvent.setup()
    renderRoute('/payments')

    const table = await screen.findByRole('table')
    expect(await within(table).findByText('Չեմ վճարել')).toBeInTheDocument()
    expect(within(table).getByRole('link', { name: 'Արամ Սանտեխնիկ' })).toHaveAttribute('href', '/partners/partner-aram')
    expect(requests[0]).toEqual({ status: 'Disputed', page: '1', pageSize: '20' })

    await user.click(screen.getByText(hy.payments.status.Confirmed))
    await waitFor(() => expect(requests.at(-1)).toEqual({ status: 'Confirmed', page: '1', pageSize: '20' }))
    await user.click(await screen.findByTitle('2'))
    await waitFor(() => expect(requests.at(-1)).toEqual({ status: 'Confirmed', page: '2', pageSize: '20' }))
    await user.click(screen.getByText(hy.payments.filters.all))
    await waitFor(() => expect(requests.at(-1)).toEqual({ page: '1', pageSize: '20' }))
    await user.click(screen.getByText(hy.payments.disputes))
    expect(await within(await screen.findByRole('table')).findByText('Չեմ վճարել')).toBeInTheDocument()
  })

  it('decides a dispute with a note, and reports failures', async () => {
    const bodies = []
    server.use(
      http.post('*/api/v1/admin/payments/:id/resolve', async ({ request }) => {
        bodies.push(await request.json())
        if (bodies.length === 2) return problem(422, 'payment.not_disputed')
        if (bodies.length === 3) return HttpResponse.json({ status: 400, code: 'validation_failed', errors: { note: ['note.too_long'] } }, { status: 400 })
        return HttpResponse.json(paymentRow({ status: 'Rejected' }))
      }),
    )
    const user = userEvent.setup()
    renderRoute('/payments')

    const decide = async (note) => {
      await user.click(await screen.findByRole('button', { name: hy.payments.resolve.button }))
      const dialog = (await screen.findAllByRole('dialog')).at(-1)
      await user.click(within(dialog).getByLabelText(hy.payments.resolve.rejected))
      await user.type(within(dialog).getByRole('textbox'), note)
      await user.click(within(dialog).getByRole('button', { name: hy.payments.resolve.confirm }))
      return dialog
    }

    await decide('Կտրոն չկա')
    await waitFor(() => expect(bodies[0]).toEqual({ counts: false, note: 'Կտրոն չկա' }))
    expect(await screen.findByText(hy.payments.resolve.doneRejected)).toBeInTheDocument()

    await decide('Կրկին')
    expect(await screen.findByText(hy.errors.payment.not_disputed)).toBeInTheDocument()

    const dialog = await decide('Երկար')
    expect(await within(dialog).findByText(hy.errors.note.too_long)).toBeInTheDocument()
  }, SLOW)

  it('shows errors, and no decisions for staff who can only view', async () => {
    signedInAs(staffMember(['payments.view']))
    renderRoute('/payments')
    expect(await within(await screen.findByRole('table')).findByText('Չեմ վճարել')).toBeInTheDocument()
    expect(screen.queryByRole('button', { name: hy.payments.resolve.button })).not.toBeInTheDocument()
  })

  it('shows list errors', async () => {
    server.use(http.get('*/api/v1/admin/payments', () => problem(500, 'boom')))
    renderRoute('/payments?status=all')
    expect(await screen.findByText(hy.errors.generic)).toBeInTheDocument()
  })
})
