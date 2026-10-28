import { screen, waitFor, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { http, HttpResponse } from 'msw'
import en from '@/i18n/locales/en/common.json'
import { CUSTOMER, PARTNER_USER, signedInAs } from '@/test/auth'
import { inboxRequest, myRequest, offer } from '@/test/fixtures'
import { renderRoute } from '@/test/renderWithProviders'
import { server } from '@/test/server'

const SLOW = 30_000
const mat = en.estimator.materials
const TITLE = mat.title.replace('{{grade}}', mat.grades.Standard)

const LINES = [{ id: 'line-1', roomName: 'Bath', workItemId: 'w-tiles', name: 'Floor tiling', unit: 'SquareMeter', quantity: 4, estimateMin: 20000, estimateMax: 36000 }]
const MATERIALS = [{ materialId: 'm-tiles', name: 'Floor tiles', unit: 'SquareMeter', quantity: 4.4, estimateCost: 39600 }]

describe('Materials in requests and offers', () => {
  it('shows the partner the materials and lets them price them apart from the labour', async () => {
    signedInAs(PARTNER_USER)
    const sent = []
    const partnerView = inboxRequest({
      lines: LINES.map((line) => ({ ...line, estimateMin: null, estimateMax: null })),
      materialGrade: 'Standard',
      materials: MATERIALS.map((material) => ({ ...material, estimateCost: null })),
    })
    server.use(
      http.get('*/api/v1/requests/inbox/:id', () => HttpResponse.json(partnerView)),
      http.get('*/api/v1/requests/:id/offers', () => HttpResponse.json([])),
      http.get('*/api/v1/me/partner-profile/prices', () =>
        HttpResponse.json({ items: [{ workItemId: 'w-tiles', name: 'Floor tiling', unit: 'SquareMeter', priceFrom: 7000, priceTo: null, marketTypical: 6500 }] }),
      ),
      http.post('*/api/v1/requests/:id/offers', async ({ request }) => {
        sent.push(await request.json())
        return HttpResponse.json(offer(), { status: 201 })
      }),
    )
    const user = userEvent.setup()
    const { router } = renderRoute('/en/inbox/req-1')

    const card = await screen.findByRole('region', { name: TITLE })
    expect(within(card).getByText('Floor tiles')).toBeInTheDocument()
    expect(within(card).getByText('4.4 m²')).toBeInTheDocument()
    expect(within(card).queryByText(/39[\s,.]?600/)).not.toBeInTheDocument() // partners never see the estimate

    await router.navigate('/en/inbox/req-1/offer')
    await waitFor(() => expect(screen.getByLabelText(en.offers.fields.price)).toHaveValue(28000))
    await user.type(screen.getByLabelText(en.offers.fields.summary), 'Tiling the bathroom floor, tiles included.')
    await user.click(screen.getByLabelText(en.offers.fields.materialsIncluded))
    await user.type(screen.getByLabelText(new RegExp(en.offers.fields.materialsAmount.replace(/[.*+?^${}()|[\]\\]/g, '\\$&'))), '39000')
    expect(screen.getByLabelText(en.offers.fields.price)).toHaveValue(67000)
    await user.click(screen.getByRole('button', { name: en.offers.send }))

    await waitFor(() => expect(sent).toHaveLength(1))
    expect(sent[0]).toMatchObject({ price: 67000, materialsIncluded: true, materialsAmount: 39000 })
  }, SLOW)

  it('offers today’s cheapest store prices for the materials and fills them in on request', async () => {
    signedInAs(PARTNER_USER)
    const asked = []
    server.use(
      http.get('*/api/v1/requests/inbox/:id', () =>
        HttpResponse.json(inboxRequest({ materialGrade: 'Standard', materials: [...MATERIALS, { materialId: 'm-grout', name: 'Grout', unit: 'Kilogram', quantity: 2, estimateCost: null }] })),
      ),
      http.get('*/api/v1/requests/:id/offers', () => HttpResponse.json([])),
      http.post('*/api/v1/materials/where-to-buy', async ({ request }) => {
        asked.push(await request.json())
        return HttpResponse.json({
          cheapest: [{ materialId: 'm-tiles', materialSlug: 'floor-tiles', storeName: 'Demo Stroy', storeSlug: 'demo-stroy', productName: 'Tiles', packLabel: '1.44 m² box', packSize: 1.44, packs: 4, cost: 38900 }],
          stores: [],
        })
      }),
    )
    const user = userEvent.setup()
    renderRoute('/en/inbox/req-1/offer')

    await user.type(await screen.findByLabelText(en.offers.fields.summary), 'Tiling the bathroom floor, tiles included.')
    await user.type(screen.getByLabelText(en.offers.fields.price), '80000')
    expect(screen.queryByText(/Today's cheapest in stores/)).not.toBeInTheDocument()
    await user.click(screen.getByLabelText(en.offers.fields.materialsIncluded))

    expect(await screen.findByText(/Today's cheapest in stores: 38[\s,.]?900 ֏ for 1 of 2 materials/)).toBeInTheDocument()
    expect(asked).toEqual([{ grade: 'Standard', materials: [{ materialId: 'm-tiles', quantity: 4.4 }, { materialId: 'm-grout', quantity: 2 }] }])
    await user.click(screen.getByRole('button', { name: en.offers.storePrices.use }))
    expect(screen.getByLabelText(new RegExp(en.offers.fields.materialsAmount.replace(/[.*+?^${}()|[\]\\]/g, '\\$&')))).toHaveValue(38900)

    await user.click(screen.getByRole('radio', { name: en.offers.kindChoice.Visit }))
    expect(screen.queryByText(/Today's cheapest in stores/)).not.toBeInTheDocument()
  }, SLOW)

  it('compares the materials of the offers with the estimate for the customer', async () => {
    signedInAs(CUSTOMER)
    server.use(
      http.get('*/api/v1/requests/:id', () =>
        HttpResponse.json(myRequest({ lines: LINES, estimateId: 'est-1', partners: [], materialGrade: 'Standard', materials: MATERIALS })),
      ),
      http.get('*/api/v1/requests/:id/offers', () =>
        HttpResponse.json([
          offer({
            id: 'o-1',
            price: 67000,
            materialsIncluded: true,
            materialsAmount: 39000,
            partner: { partnerId: 'p-1', displayName: 'Aram', slug: null },
            lines: [{ title: 'Bath · Floor tiling', included: true, requestLineId: 'line-1', quantity: 4, unitPrice: 7000, amount: 28000 }],
            stages: [],
          }),
          offer({
            id: 'o-2',
            price: 24000,
            materialsIncluded: false,
            partner: { partnerId: 'p-2', displayName: 'Gor', slug: null },
            lines: [{ title: 'Bath · Floor tiling', included: true, requestLineId: 'line-1', quantity: 4, unitPrice: 6000, amount: 24000 }],
            stages: [],
          }),
        ]),
      ),
    )
    renderRoute('/en/requests/req-1')

    const card = await screen.findByRole('region', { name: TITLE })
    expect(within(card).getByText(/Materials: 39[\s,.]?600/)).toBeInTheDocument()
    const table = await screen.findByRole('table')
    const materials = within(table).getByRole('row', { name: new RegExp(`^${en.offers.compare.materials}`) })
    expect(within(materials).getByText(/^39[\s,.]?600/)).toBeInTheDocument()
    expect(within(materials).getByText(/^39[\s,.]?000/)).toBeInTheDocument()
    expect(within(materials).getByText(en.offers.compare.materialsNotIncluded)).toBeInTheDocument()
    const total = within(table).getByRole('row', { name: new RegExp(`^${en.offers.compare.total}`) })
    expect(total).toHaveTextContent(/59[\s,.]?600 ֏ – 75[\s,.]?600/)
    expect(screen.getAllByText(/Included: 39[\s,.]?000 ֏ \(labour 28[\s,.]?000 ֏\)/).length).toBeGreaterThan(0)
  }, SLOW)
})
