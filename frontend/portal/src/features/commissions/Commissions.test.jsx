import { screen, waitFor, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { http, HttpResponse } from 'msw'
import i18n from '@/i18n'
import en from '@/i18n/locales/en/common.json'
import { notificationText } from '@/features/notifications/notificationText'
import { PARTNER_USER, problem, signedInAs } from '@/test/auth'
import { commissionLine, commissionSummary, myStatement, page, statementDetail } from '@/test/fixtures'
import { renderRoute } from '@/test/renderWithProviders'
import { server } from '@/test/server'
import { formatDate } from '@/shared/format'

function commissionsApi({ summary = commissionSummary(), statements = [myStatement()], lines = [commissionLine()] } = {}) {
  const calls = { statements: [], lines: [] }
  server.use(
    http.get('*/api/v1/me/commissions/summary', () => HttpResponse.json(summary)),
    http.get('*/api/v1/me/commission-statements', ({ request }) => {
      calls.statements.push(Object.fromEntries(new URL(request.url).searchParams))
      return HttpResponse.json(page(statements, { totalCount: statements.length > 0 ? 25 : 0 }))
    }),
    http.get('*/api/v1/me/commissions', ({ request }) => {
      calls.lines.push(Object.fromEntries(new URL(request.url).searchParams))
      return HttpResponse.json(page(lines))
    }),
    http.get('*/api/v1/me/commission-statements/:id', () => HttpResponse.json(statementDetail())),
  )
  return calls
}

describe('Commissions page', () => {
  beforeEach(() => signedInAs(PARTNER_USER))

  it('shows the totals and weekly statements, and the commission on each order', async () => {
    const calls = commissionsApi({ statements: [myStatement(), myStatement({ id: 'statement-2', overdue: true })] })
    const user = userEvent.setup()
    renderRoute('/en/commissions')

    expect(await screen.findByRole('heading', { name: en.commissions.title })).toBeInTheDocument()
    expect(await screen.findByText('10,000 ֏')).toBeInTheDocument()
    expect(screen.getByText(en.commissions.summary.unbilled)).toBeInTheDocument()
    expect(screen.getByText('3,000 ֏')).toBeInTheDocument()
    expect(screen.queryByText(en.commissions.paused.title)).not.toBeInTheDocument()
    expect(screen.getByRole('link', { name: en.requests.tabs.commissions })).toHaveAttribute('href', '/en/commissions')

    const statement = (await screen.findAllByRole('link', { name: /Oct 5, 2026 – Oct 11, 2026/ }))[0]
    expect(statement).toHaveAttribute('href', '/en/commissions/statement-1')
    expect(within(statement).getByText('Total 15,000 ֏ · paid 5,000 ֏')).toBeInTheDocument()
    expect(within(statement).getByText(`10,000 ֏ to pay by ${formatDate('2026-10-19', 'en')}`)).toBeInTheDocument()
    expect(screen.getByText(en.commissions.status.Overdue, { selector: '.tag' })).toBeInTheDocument()
    expect(calls.statements[0]).toEqual({ page: '1', pageSize: '20' })

    await user.click(screen.getByRole('button', { name: en.pagination.next }))
    await waitFor(() => expect(calls.statements.at(-1)).toEqual({ page: '2', pageSize: '20' }))

    await user.click(screen.getByRole('radio', { name: en.commissions.views.orders }))
    const order = await screen.findByRole('link', { name: /Replace the kitchen tap/ })
    expect(order).toHaveAttribute('href', '/en/orders/order-1')
    expect(within(order).getByText('150,000 ֏ × 10% = 15,000 ֏')).toBeInTheDocument()
    expect(within(order).getByText(en.commissions.billed)).toBeInTheDocument()
    expect(calls.lines[0]).toEqual({ page: '1', pageSize: '20' })
  })

  it('marks unbilled orders and shows paid statements', async () => {
    commissionsApi({
      statements: [myStatement({ status: 'Paid', outstanding: 0, paidAmount: 15000, paidAt: '2026-10-14T10:00:00Z' })],
      lines: [commissionLine({ statementId: null, summary: '' })],
    })
    const user = userEvent.setup()
    renderRoute('/en/commissions')

    expect(await screen.findByText(`Paid on ${formatDate('2026-10-14T10:00:00Z', 'en')}`)).toBeInTheDocument()
    expect(screen.getByText(en.commissions.status.Paid)).toBeInTheDocument()
    await user.click(screen.getByRole('radio', { name: en.commissions.views.orders }))
    const order = await screen.findByRole('link', { name: new RegExp(`^${en.commissions.order}\\s*${en.commissions.unbilled}`) })
    expect(within(order).getByText(en.commissions.unbilled)).toBeInTheDocument()
  })

  it('explains why new requests stopped while the partner is paused', async () => {
    commissionsApi({ summary: commissionSummary({ overdue: 10000, pausedSince: '2026-11-03T08:00:00Z' }) })
    renderRoute('/en/commissions')

    expect(await screen.findByText(en.commissions.paused.title)).toBeInTheDocument()
    expect(screen.getByText(new RegExp(`more than 14 days overdue`))).toBeInTheDocument()
  })

  it('says so in the inbox too, with a way to the statements', async () => {
    commissionsApi({ summary: commissionSummary({ pausedSince: '2026-11-03T08:00:00Z' }) })
    server.use(http.get('*/api/v1/requests/inbox', () => HttpResponse.json(page([]))))
    renderRoute('/en/inbox')

    expect(await screen.findByText(en.commissions.paused.title)).toBeInTheDocument()
    expect(screen.getByRole('link', { name: en.commissions.paused.open })).toHaveAttribute('href', '/en/commissions')
  })

  it('shows empty lists and load errors', async () => {
    commissionsApi({ statements: [], lines: [] })
    server.use(http.get('*/api/v1/me/commissions/summary', () => problem(500, 'boom')))
    const user = userEvent.setup()
    renderRoute('/en/commissions?view=orders')

    expect(await screen.findByText(en.commissions.chargesEmpty)).toBeInTheDocument()
    expect(await screen.findByText(en.errors.generic)).toBeInTheDocument()
    await user.click(screen.getByRole('radio', { name: en.commissions.views.statements }))
    expect(await screen.findByText(en.commissions.statementsEmpty)).toBeInTheDocument()
  })
})

describe('Statement page', () => {
  beforeEach(() => signedInAs(PARTNER_USER))

  it('shows the totals, the orders and the payments recorded', async () => {
    commissionsApi()
    renderRoute('/en/commissions/statement-1')

    expect(await screen.findByRole('heading', { name: /Statement for Oct 5, 2026/ })).toBeInTheDocument()
    expect(screen.getByRole('link', { name: 'Replace the kitchen tap' })).toHaveAttribute('href', '/en/orders/order-1')
    expect(screen.getByText(/Receipt 7/)).toBeInTheDocument()
    expect(screen.getByText(/Cash ·/)).toBeInTheDocument()
    expect(screen.getByText(en.commissions.howToPay)).toBeInTheDocument()
    expect(screen.getByRole('link', { name: en.commissions.back })).toHaveAttribute('href', '/en/commissions')
  })

  it('shows a paid statement without payments recorded', async () => {
    server.use(
      http.get('*/api/v1/me/commission-statements/:id', () =>
        HttpResponse.json(
          statementDetail({
            statement: myStatement({ status: 'Paid', outstanding: 0, paidAt: '2026-10-14T10:00:00Z' }),
            lines: [commissionLine({ summary: null })],
            settlements: [{ id: 's-2', amount: 15000, method: 'BankTransfer', paidOn: '2026-10-14', reference: null, recordedAt: '2026-10-14T10:00:00Z' }],
          }),
        ),
      ),
    )
    renderRoute('/en/commissions/statement-1')

    expect(await screen.findByText(en.commissions.facts.paidAt)).toBeInTheDocument()
    expect(screen.queryByText(en.commissions.howToPay)).not.toBeInTheDocument()
    expect(screen.getByRole('link', { name: en.commissions.order })).toBeInTheDocument()
    expect(screen.getByText(/Bank transfer ·/)).toBeInTheDocument()
  })

  it('says when there are no payments yet, and when a statement is not found', async () => {
    server.use(http.get('*/api/v1/me/commission-statements/:id', () => HttpResponse.json(statementDetail({ settlements: [] }))))
    renderRoute('/en/commissions/statement-1')
    expect(await screen.findByText(en.commissions.noPayments)).toBeInTheDocument()

    server.use(http.get('*/api/v1/me/commission-statements/:id', () => problem(404, 'commission.statement_not_found')))
    renderRoute('/en/commissions/missing')
    expect(await screen.findByText(en.commissions.notFoundText)).toBeInTheDocument()
  })
})

describe('Commission notifications', () => {
  it('read well in the user language', async () => {
    await i18n.changeLanguage('en')
    const t = i18n.t.bind(i18n)
    const text = (type, params = {}) => notificationText(t, { type, params }, 'en')
    expect(text('CommissionStatementIssued', { amount: '15000', dueOn: '2026-10-19' })).toBe(
      `Your weekly commission statement is ready: 15,000 ֏, due by ${formatDate('2026-10-19', 'en')}.`,
    )
    expect(text('CommissionSettled', { amount: '5000', outstanding: '10000' })).toBe('We recorded your payment of 5,000 ֏. Still to pay: 10,000 ֏.')
    expect(text('PartnerResumed')).toBe(en.notifications.types.PartnerResumed)
  })
})
