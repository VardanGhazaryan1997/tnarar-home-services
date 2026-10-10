import { screen, waitFor, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { http, HttpResponse } from 'msw'
import en from '@/i18n/locales/en/common.json'
import { CUSTOMER, PARTNER_USER, signedInAs } from '@/test/auth'
import { inboxRequest, myRequest, offer } from '@/test/fixtures'
import { renderRoute } from '@/test/renderWithProviders'
import { server } from '@/test/server'

const SLOW = 30_000
const lp = en.offers.linePricing

const LINES = [
  { id: 'line-1', roomName: 'Bath', workItemId: 'w-tiles', name: 'Floor tiling', unit: 'SquareMeter', quantity: 4, estimateMin: 20000, estimateMax: 36000 },
  { id: 'line-2', roomName: 'Bath', workItemId: 'w-toilet', name: 'Toilet installation', unit: 'Piece', quantity: null, estimateMin: null, estimateMax: null },
]
const partnerLines = LINES.map((line) => ({ ...line, estimateMin: null, estimateMax: null }))

describe('Requests made from an estimate', () => {
  it('lets the partner price the work line by line from their price list', async () => {
    signedInAs(PARTNER_USER)
    const sent = []
    server.use(
      http.get('*/api/v1/requests/inbox/:id', () => HttpResponse.json(inboxRequest({ lines: partnerLines }))),
      http.get('*/api/v1/requests/:id/offers', () => HttpResponse.json([])),
      http.get('*/api/v1/me/partner-profile/prices', () =>
        HttpResponse.json({
          items: [
            { workItemId: 'w-tiles', name: 'Floor tiling', unit: 'SquareMeter', priceFrom: 6000, priceTo: 8000, marketTypical: 6500 },
            { workItemId: 'w-toilet', name: 'Toilet installation', unit: 'Piece', priceFrom: null, priceTo: null, marketTypical: 15000 },
          ],
        }),
      ),
      http.post('*/api/v1/requests/:id/offers', async ({ request }) => {
        sent.push(await request.json())
        return HttpResponse.json(offer(), { status: 201 })
      }),
    )
    const user = userEvent.setup()
    const { router } = renderRoute('/en/inbox/req-1/offer')

    const tiling = await screen.findByRole('region', { name: 'Bath' })
    await waitFor(() => expect(within(tiling).getAllByLabelText('Price per m², ֏')[0]).toHaveValue('7000'))
    expect(within(tiling).getByText(lp.source.mine)).toBeInTheDocument()
    expect(within(tiling).getByLabelText('Price per pcs, ֏')).toHaveValue('15000')
    expect(within(tiling).getByText(lp.source.market)).toBeInTheDocument()

    await user.type(screen.getByLabelText(en.offers.fields.summary), 'Tiling and a new toilet in the bathroom.')
    await user.click(screen.getByRole('button', { name: en.offers.send }))
    expect(await screen.findByText(lp.priceEvery)).toBeInTheDocument()
    expect(sent).toHaveLength(0)

    await user.type(within(tiling).getByLabelText('Amount, pcs'), '1')
    expect(screen.getByLabelText(en.offers.fields.price)).toHaveValue(43000)
    expect(screen.getByText(/Total of the lines: 43[\s,.]?000/)).toBeInTheDocument()
    await user.click(screen.getByRole('button', { name: en.offers.send }))

    await waitFor(() => expect(router.state.location.pathname).toBe('/en/inbox/req-1'))
    expect(sent[0].price).toBe(43000)
    expect(sent[0].lines).toEqual([
      { title: 'Bath · Floor tiling', included: true, requestLineId: 'line-1', quantity: 4, unitPrice: 7000 },
      { title: 'Bath · Toilet installation', included: true, requestLineId: 'line-2', quantity: 1, unitPrice: 15000 },
    ])
  }, SLOW)

  it('shows the partner the work without the estimate', async () => {
    signedInAs(PARTNER_USER)
    server.use(
      http.get('*/api/v1/requests/inbox/:id', () => HttpResponse.json(inboxRequest({ lines: partnerLines }))),
      http.get('*/api/v1/requests/:id/offers', () => HttpResponse.json([])),
    )
    renderRoute('/en/inbox/req-1')

    const bath = await screen.findByRole('region', { name: 'Bath' })
    expect(within(bath).getByText('Floor tiling')).toBeInTheDocument()
    expect(within(bath).getByText('4 m²')).toBeInTheDocument()
    expect(within(bath).getByText(en.requests.lines.toAgree)).toBeInTheDocument()
    expect(within(bath).queryByText(/36[\s,.]?000/)).not.toBeInTheDocument()
  }, SLOW)

  it('compares the offers line by line with the estimate for the customer', async () => {
    signedInAs(CUSTOMER)
    server.use(
      http.get('*/api/v1/requests/:id', () => HttpResponse.json(myRequest({ lines: LINES, estimateId: 'est-1', partners: [] }))),
      http.get('*/api/v1/requests/:id/offers', () =>
        HttpResponse.json([
          offer({
            id: 'o-1',
            price: 43000,
            partner: { partnerId: 'p-1', displayName: 'Aram', slug: null },
            lines: [
              { title: 'Bath · Floor tiling', included: true, requestLineId: 'line-1', quantity: 4, unitPrice: 7000, amount: 28000 },
              { title: 'Bath · Toilet installation', included: true, requestLineId: 'line-2', quantity: 1, unitPrice: 15000, amount: 15000 },
            ],
            stages: [],
          }),
          offer({
            id: 'o-2',
            price: 24000,
            partner: { partnerId: 'p-2', displayName: 'Gor', slug: null },
            lines: [
              { title: 'Bath · Floor tiling', included: true, requestLineId: 'line-1', quantity: 4, unitPrice: 6000, amount: 24000 },
              { title: 'Bath · Toilet installation', included: false, requestLineId: 'line-2', quantity: null, unitPrice: null, amount: null },
            ],
            stages: [],
          }),
        ]),
      ),
    )
    renderRoute('/en/requests/req-1')

    const table = await screen.findByRole('table')
    const headers = within(table).getAllByRole('columnheader').map((th) => th.textContent)
    expect(headers).toEqual([en.offers.compare.work, en.offers.compare.estimate, 'Aram', 'Gor'])
    const tiling = within(table).getByRole('row', { name: /Floor tiling/ })
    expect(within(tiling).getByText(/24[\s,.]?000/)).toHaveClass('offer-compare__best')
    const toilet = within(table).getByRole('row', { name: /Toilet installation/ })
    expect(within(toilet).getByText(en.offers.compare.notIncluded)).toBeInTheDocument()

    const work = screen.getByRole('link', { name: en.requests.lines.openEstimate })
    expect(work).toHaveAttribute('href', '/en/estimates/est-1')
  }, SLOW)

  it('starts a request from a saved estimate', async () => {
    signedInAs(CUSTOMER)
    const created = []
    server.use(
      http.get('*/api/v1/me/estimates/est-1', () =>
        HttpResponse.json({
          id: 'est-1',
          title: 'Our flat',
          oldBuilding: false,
          shareToken: null,
          rooms: [{ id: 'r1', name: 'Bath', type: 'Bathroom', area: 4, height: 2.7, openings: [], lines: [{ workItemId: 'w-boiler', quantity: 1 }] }],
          measurement: { rooms: [], totalMin: 20000, totalTypical: 30000, totalMax: 40000, unpricedLines: 0 },
        }),
      ),
      http.get('*/api/v1/work-items', () => HttpResponse.json([{ id: 'w-boiler', categoryId: 'cat-boilers', name: 'Boiler', unit: 'Piece' }])),
      http.post('*/api/v1/requests', async ({ request }) => {
        created.push(await request.json())
        return HttpResponse.json(myRequest(), { status: 201 })
      }),
      http.get('*/api/v1/requests/req-1', () => HttpResponse.json(myRequest())),
      http.get('*/api/v1/requests/req-1/offers', () => HttpResponse.json([])),
    )
    const user = userEvent.setup()
    renderRoute('/en/requests/new?estimate=est-1')

    expect(await screen.findByText(en.requests.fromEstimate.title.replace('{{title}}', 'Our flat'))).toBeInTheDocument()
    await waitFor(() => expect(screen.getByLabelText(en.requests.fields.category)).toHaveValue('cat-heating'))
    await user.selectOptions(screen.getByLabelText(en.requests.fields.city), 'city-yerevan')
    await user.click(screen.getByRole('button', { name: en.common.next }))
    expect(screen.getByLabelText(en.requests.fields.description)).toHaveValue(
      en.requests.fromEstimate.description.replace('{{title}}', 'Our flat').replace('{{rooms}}', 'Bath'),
    )
    await user.click(screen.getByRole('button', { name: en.common.next }))
    expect(screen.getByLabelText(new RegExp(en.requests.fields.budgetMin.replace(/[()]/g, '\\$&')))).toHaveValue(20000)
    await user.click(screen.getByRole('button', { name: new RegExp(en.requests.submit) }))

    await waitFor(() => expect(created).toHaveLength(1))
    expect(created[0]).toMatchObject({ categoryId: 'cat-heating', estimateId: 'est-1', budgetMin: 20000, budgetMax: 40000 })
  }, SLOW)
})
