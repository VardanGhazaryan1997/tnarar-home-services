import { screen, waitFor, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import dayjs from 'dayjs'
import { http, HttpResponse } from 'msw'
import { entityPage } from '@/features/audit/entities'
import hy from '@/i18n/locales/hy/common.json'
import { problem, signedInAs, staffMember } from '@/test/auth'
import { commissionRates, statementDetail, statementRow } from '@/test/operations'
import { page } from '@/test/partners'
import { renderRoute } from '@/test/renderWithProviders'
import { server } from '@/test/server'

// Several dialogs in one test: slow machines need more than the default 60 s.
const SLOW = 180_000

const lastDialog = async () => (await screen.findAllByRole('dialog')).at(-1)

describe('Commission statements', () => {
  it('opens on unpaid statements and filters by state and partner', async () => {
    const requests = []
    server.use(
      http.get('*/api/v1/admin/commission-statements', ({ request }) => {
        requests.push(Object.fromEntries(new URL(request.url).searchParams))
        return HttpResponse.json(page([statementRow({ partnerPaused: true })], { totalCount: 25 }))
      }),
    )
    const user = userEvent.setup()
    renderRoute('/commissions')

    const table = await screen.findByRole('table')
    expect(await within(table).findByRole('link', { name: 'Արամ Սանտեխնիկ' })).toHaveAttribute('href', '/partners/partner-aram')
    expect(within(table).getByText(hy.commissions.paused)).toBeInTheDocument()
    expect(within(table).getByText(hy.commissions.status.Overdue)).toBeInTheDocument()
    expect(within(table).getByRole('link', { name: hy.commissions.open })).toHaveAttribute('href', '/commissions/statements/statement-1')
    expect(requests[0]).toEqual({ filter: 'Open', page: '1', pageSize: '20' })

    const filters = screen.getByLabelText(hy.commissions.filters.label)
    await user.click(within(filters).getByText(hy.commissions.filters.Overdue))
    await waitFor(() => expect(requests.at(-1)).toEqual({ filter: 'Overdue', page: '1', pageSize: '20' }))
    await user.click(await screen.findByTitle('2'))
    await waitFor(() => expect(requests.at(-1)).toEqual({ filter: 'Overdue', page: '2', pageSize: '20' }))
    await user.click(within(filters).getByText(hy.commissions.filters.all))
    await waitFor(() => expect(requests.at(-1)).toEqual({ page: '1', pageSize: '20' }))
  })

  it("keeps one partner's statements until the filter is removed", async () => {
    const requests = []
    server.use(
      http.get('*/api/v1/admin/commission-statements', ({ request }) => {
        requests.push(Object.fromEntries(new URL(request.url).searchParams))
        return HttpResponse.json(page([statementRow()]))
      }),
    )
    const user = userEvent.setup()
    renderRoute('/commissions/statements?partnerId=partner-aram&filter=Paid')

    const tag = await screen.findByText(hy.commissions.filters.partner.replace('{{name}}', 'Արամ Սանտեխնիկ'))
    expect(requests[0]).toEqual({ filter: 'Paid', partnerId: 'partner-aram', page: '1', pageSize: '20' })
    await user.click(tag.closest('.ant-tag').querySelector('.ant-tag-close-icon'))
    await waitFor(() => expect(requests.at(-1)).toEqual({ filter: 'Paid', page: '1', pageSize: '20' }))
  })

  it('records a payment from the list and reports failures', async () => {
    const bodies = []
    server.use(
      http.post('*/api/v1/admin/commission-statements/:id/settlements', async ({ request, params }) => {
        bodies.push({ id: params.id, ...(await request.json()) })
        if (bodies.length === 2) return problem(422, 'commission.settlement_amount_invalid')
        if (bodies.length === 3) return HttpResponse.json({ status: 400, code: 'validation_failed', errors: { amount: ['amount.invalid'] } }, { status: 400 })
        return HttpResponse.json(statementDetail({}, { status: 'Paid', outstanding: 0 }))
      }),
    )
    const user = userEvent.setup()
    renderRoute('/commissions')

    const settle = async ({ amount, reference } = {}) => {
      await user.click(await screen.findByRole('button', { name: hy.commissions.settle.button }))
      const dialog = await lastDialog()
      const input = within(dialog).getByRole('spinbutton')
      expect(input).toHaveValue('10000')
      if (amount) {
        await user.clear(input)
        await user.type(input, amount)
      }
      if (reference) await user.type(within(dialog).getByPlaceholderText(hy.commissions.settle.referenceHint), reference)
      await user.click(within(dialog).getByRole('button', { name: hy.commissions.settle.confirm }))
      return dialog
    }

    await settle({ amount: '4000', reference: ' TX-1 ' })
    await waitFor(() => expect(bodies[0]).toEqual({ id: 'statement-1', amount: 4000, method: 'BankTransfer', paidOn: dayjs().format('YYYY-MM-DD'), reference: 'TX-1' }))
    expect(await screen.findByText(hy.commissions.settle.done)).toBeInTheDocument()

    await settle()
    expect(await screen.findByText(hy.errors.commission.settlement_amount_invalid)).toBeInTheDocument()
    expect(bodies[1].reference).toBeNull()

    const dialog = await settle()
    expect(await within(dialog).findByText(hy.errors.amount.invalid)).toBeInTheDocument()
  }, SLOW)

  it('checks the amount before sending', async () => {
    const user = userEvent.setup()
    renderRoute('/commissions')
    await user.click(await screen.findByRole('button', { name: hy.commissions.settle.button }))
    const dialog = await lastDialog()
    await user.clear(within(dialog).getByRole('spinbutton'))
    await user.click(within(dialog).getByRole('button', { name: hy.commissions.settle.confirm }))
    expect(await within(dialog).findByText(/1-ից մինչև/)).toBeInTheDocument()
    await user.click(within(dialog).getByRole('button', { name: hy.common.cancel }))
  }, SLOW)

  it('shows list errors, and no payment button for staff who can only view', async () => {
    signedInAs(staffMember(['commissions.view']))
    renderRoute('/commissions/statements')
    expect(await within(await screen.findByRole('table')).findByText('Արամ Սանտեխնիկ')).toBeInTheDocument()
    expect(screen.queryByRole('button', { name: hy.commissions.settle.button })).not.toBeInTheDocument()

    server.use(http.get('*/api/v1/admin/commission-statements', () => problem(500, 'boom')))
    renderRoute('/commissions/statements?filter=all')
    expect(await screen.findByText(hy.errors.generic)).toBeInTheDocument()
  })
})

describe('Commission statement detail', () => {
  it('shows the orders and payments and records the rest', async () => {
    const bodies = []
    server.use(
      http.get('*/api/v1/admin/commission-statements/:id', () => HttpResponse.json(statementDetail({ partnerPaused: true }))),
      http.post('*/api/v1/admin/commission-statements/:id/settlements', async ({ request }) => {
        bodies.push(await request.json())
        return HttpResponse.json(statementDetail({}, { status: 'Paid', outstanding: 0 }))
      }),
    )
    const user = userEvent.setup()
    renderRoute('/commissions/statements/statement-1')

    expect(await screen.findByRole('link', { name: 'Ծորակի փոխարինում' })).toHaveAttribute('href', '/orders/order-1')
    expect(screen.getByText('Անդորրագիր 7')).toBeInTheDocument()
    expect(screen.getByText(hy.commissions.paused)).toBeInTheDocument()
    expect(screen.getByRole('link', { name: /Արամ Սանտեխնիկ/ })).toHaveAttribute('href', '/partners/partner-aram')

    await user.click(screen.getByRole('button', { name: hy.commissions.settle.button }))
    const dialog = await lastDialog()
    await user.click(within(dialog).getByRole('button', { name: hy.commissions.settle.confirm }))
    await waitFor(() => expect(bodies[0]).toMatchObject({ amount: 10000, method: 'BankTransfer', reference: null }))
  }, SLOW)

  it('shows a paid statement without the payment button', async () => {
    server.use(
      http.get('*/api/v1/admin/commission-statements/:id', () =>
        HttpResponse.json({ ...statementDetail({}, { status: 'Paid', overdue: false, outstanding: 0, paidAmount: 15000, paidAt: '2026-10-14T10:00:00Z' }), settlements: [] }),
      ),
    )
    renderRoute('/commissions/statements/statement-1')
    expect(await screen.findByText(hy.commissions.detail.paidAt)).toBeInTheDocument()
    expect(screen.getByText(hy.commissions.status.Paid, { selector: '.ant-tag' })).toBeInTheDocument()
    expect(screen.getByText(hy.commissions.detail.noSettlements)).toBeInTheDocument()
    expect(screen.queryByRole('button', { name: hy.commissions.settle.button })).not.toBeInTheDocument()
  })

  it('shows when a statement does not exist', async () => {
    server.use(http.get('*/api/v1/admin/commission-statements/:id', () => problem(404, 'commission.statement_not_found')))
    renderRoute('/commissions/statements/missing')
    expect(await screen.findByText(hy.errors.commission.statement_not_found)).toBeInTheDocument()
    expect(screen.getByRole('link', { name: new RegExp(hy.commissions.detail.back) })).toHaveAttribute('href', '/commissions/statements')
  })

  it('is linked from the audit log', () => {
    expect(entityPage('CommissionStatement', 'statement-1')).toBe('/commissions/statements/statement-1')
  })
})

describe('Commission rates', () => {
  it('changes the default rate and reports failures', async () => {
    const bodies = []
    server.use(
      http.put('*/api/v1/admin/commission-rates/default', async ({ request }) => {
        bodies.push(await request.json())
        if (bodies.length === 2) return HttpResponse.json({ status: 400, code: 'validation_failed', errors: { percent: ['percent.invalid'] } }, { status: 400 })
        if (bodies.length === 3) return problem(500, 'boom')
        return HttpResponse.json(commissionRates({ defaultPercent: 11 }))
      }),
    )
    const user = userEvent.setup()
    renderRoute('/commissions/rates')

    const input = await screen.findByRole('spinbutton', { name: hy.commissions.rates.default })
    await waitFor(() => expect(input).toHaveValue('10.00'))
    const save = screen.getByRole('button', { name: hy.commissions.rates.save })
    await user.clear(input)
    await user.type(input, '11')
    await user.click(save)
    await waitFor(() => expect(bodies[0]).toEqual({ percent: 11 }))
    expect(await screen.findByText(hy.commissions.rates.saved)).toBeInTheDocument()

    await user.click(save)
    expect(await screen.findByText(hy.errors.percent.invalid)).toBeInTheDocument()
    await user.click(save)
    expect(await screen.findByText(hy.errors.generic)).toBeInTheDocument()
  }, SLOW)

  it('sets, changes and removes category rates', async () => {
    const puts = []
    const deletes = []
    const rates = commissionRates()
    rates.categories[2] = { ...rates.categories[2], percent: 5, effectivePercent: 5 }
    server.use(
      http.get('*/api/v1/admin/commission-rates', () => HttpResponse.json(rates)),
      http.put('*/api/v1/admin/commission-rates/categories/:id', async ({ request, params }) => {
        puts.push({ id: params.id, ...(await request.json()) })
        if (puts.length === 2) return HttpResponse.json({ status: 400, code: 'validation_failed', errors: { percent: ['percent.invalid'] } }, { status: 400 })
        if (puts.length === 3) return problem(404, 'category.not_found')
        return HttpResponse.json(commissionRates())
      }),
      http.delete('*/api/v1/admin/commission-rates/categories/:id', ({ params }) => {
        deletes.push(params.id)
        return deletes.length === 2 ? problem(500, 'boom') : HttpResponse.json(commissionRates())
      }),
    )
    const user = userEvent.setup()
    renderRoute('/commissions/rates')

    const table = await screen.findByRole('table')
    expect(within(table).getByText(hy.commissions.rates.fromDefault)).toBeInTheDocument()
    const heatingRow = within(table).getByText('Ջեռուցում').closest('tr')

    const setRate = async (value) => {
      await user.click(within(heatingRow).getByRole('button', { name: hy.commissions.rates.set }))
      const dialog = await lastDialog()
      const input = within(dialog).getByRole('spinbutton', { name: hy.commissions.rates.own })
      expect(input).toHaveValue('10.00')
      await user.clear(input)
      await user.type(input, value)
      await user.click(within(dialog).getByRole('button', { name: hy.commissions.rates.save }))
      return dialog
    }

    await setRate('7.5')
    await waitFor(() => expect(puts[0]).toEqual({ id: 'cat-heating', percent: 7.5 }))
    expect(await screen.findByText(hy.commissions.rates.saved)).toBeInTheDocument()

    const dialog = await setRate('8')
    expect(await within(dialog).findByText(hy.errors.percent.invalid)).toBeInTheDocument()
    await user.click(within(dialog).getByRole('button', { name: hy.commissions.rates.save }))
    expect(await screen.findByText(hy.errors.category.not_found)).toBeInTheDocument()

    const remove = async (name) => {
      const row = within(table).getByText(name).closest('tr')
      await user.click(within(row).getByRole('button', { name: hy.commissions.rates.clear }))
      const confirms = await screen.findAllByRole('button', { name: hy.commissions.rates.clear })
      await user.click(confirms.at(-1))
    }
    await remove('Սանտեխնիկա')
    await waitFor(() => expect(deletes).toEqual(['cat-plumbing']))
    expect(await screen.findByText(hy.commissions.rates.cleared)).toBeInTheDocument()
    await remove('Կաթսաներ')
    await waitFor(() => expect(deletes).toEqual(['cat-plumbing', 'cat-boilers']))
    expect(await screen.findByText(hy.errors.generic)).toBeInTheDocument()
  }, SLOW)

  it('shows rates read-only to staff who can only view, and load errors', async () => {
    signedInAs(staffMember(['commissions.view']))
    renderRoute('/commissions/rates')
    expect(await screen.findByText('Կաթսաներ')).toBeInTheDocument()
    expect(screen.getByText(hy.commissions.rates.fromParent)).toBeInTheDocument()
    expect(screen.getAllByText('10%')).toHaveLength(3)
    expect(screen.queryByRole('spinbutton')).not.toBeInTheDocument()
    expect(screen.queryByRole('button', { name: hy.commissions.rates.change })).not.toBeInTheDocument()

    server.use(http.get('*/api/v1/admin/commission-rates', () => problem(500, 'boom')))
    renderRoute('/commissions/rates')
    expect(await screen.findByText(hy.errors.generic)).toBeInTheDocument()
  })

  it('switches between the tabs', async () => {
    const user = userEvent.setup()
    renderRoute('/commissions/statements')
    await user.click(await screen.findByRole('tab', { name: hy.commissions.tabs.rates }))
    expect(await screen.findByText(hy.commissions.rates.help)).toBeInTheDocument()
  })
})
