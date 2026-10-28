import { screen, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { http, HttpResponse } from 'msw'
import en from '@/i18n/locales/en/common.json'
import { renderRoute } from '@/test/renderWithProviders'
import { server } from '@/test/server'
import { cheapestOf, topStores, whereToBuyRequest } from './storeOffers'

const SLOW = 30_000

const MEASUREMENT = {
  rooms: [{ type: 'Bedroom', floorArea: 14, perimeter: 15, wallArea: 37, ceilingArea: 14, skirtingLength: 14, lines: [], totalMin: 30000, totalTypical: 45000, totalMax: 60000 }],
  totalMin: 30000,
  totalTypical: 45000,
  totalMax: 60000,
  unpricedLines: 0,
  materialGrade: 'Standard',
  materials: [
    { materialId: 'm-putty', name: 'Finishing putty', unit: 'Kilogram', quantity: 60, unitPrice: 160, cost: 9600 },
    { materialId: 'm-paint', name: 'Interior paint', unit: 'Liter', quantity: 9.5, unitPrice: 800, cost: 7600 },
  ],
  materialsTotal: 17200,
}

const OFFERS = {
  cheapest: [
    { materialId: 'm-putty', materialSlug: 'finishing-putty', storeName: 'Demo Stroy', storeSlug: 'demo-stroy', productName: 'Putty', packLabel: '25 kg bag', packSize: 25, packs: 3, cost: 7500 },
  ],
  stores: [
    {
      storeName: 'Demo Stroy',
      storeSlug: 'demo-stroy',
      total: 7500,
      covered: 1,
      missing: 1,
      items: [{ materialId: 'm-putty', materialSlug: 'finishing-putty', storeName: 'Demo Stroy', storeSlug: 'demo-stroy', productName: 'Putty', packLabel: '25 kg bag', packSize: 25, packs: 3, cost: 7500 }],
    },
    {
      storeName: 'Demo Home Center',
      storeSlug: 'demo-home-center',
      total: 9100,
      covered: 1,
      missing: 1,
      items: [{ materialId: 'm-paint', materialSlug: 'interior-paint', storeName: 'Demo Home Center', storeSlug: 'demo-home-center', productName: 'Paint 10 l', packLabel: null, packSize: 10, packs: 1, cost: 9100 }],
    },
  ],
}

describe('where to buy helpers', () => {
  it('asks only when there are materials, with each material and its quantity', () => {
    expect(whereToBuyRequest(null)).toBeNull()
    expect(whereToBuyRequest({ ...MEASUREMENT, materialGrade: null })).toBeNull()
    expect(whereToBuyRequest({ ...MEASUREMENT, materials: [] })).toBeNull()
    expect(whereToBuyRequest(MEASUREMENT)).toEqual({
      grade: 'Standard',
      materials: [
        { materialId: 'm-putty', quantity: 60 },
        { materialId: 'm-paint', quantity: 9.5 },
      ],
    })
  })

  it('finds a material’s cheapest offer and the top three stores', () => {
    expect(cheapestOf(OFFERS, 'm-putty').storeSlug).toBe('demo-stroy')
    expect(cheapestOf(OFFERS, 'm-paint')).toBeUndefined()
    expect(cheapestOf(undefined, 'm-putty')).toBeUndefined()

    const many = { stores: ['a', 'b', 'c', 'd'].map((slug) => ({ storeSlug: slug })) }
    expect(topStores(many).map((s) => s.storeSlug)).toEqual(['a', 'b', 'c'])
    expect(topStores(undefined)).toEqual([])
  })
})

describe('Where to buy', () => {
  it('shows the cheapest store for each material and the best stores for the whole list on a shared estimate', async () => {
    const requests = []
    server.use(
      http.get('*/api/v1/estimates/shared/tok-1234567890', () =>
        HttpResponse.json({ title: 'Our flat', cityId: null, oldBuilding: false, updatedAt: '2026-10-01T10:00:00Z', rooms: [], measurement: MEASUREMENT, materials: 'Standard' }),
      ),
      http.post('*/api/v1/materials/where-to-buy', async ({ request }) => {
        requests.push(await request.json())
        return HttpResponse.json(OFFERS)
      }),
    )
    renderRoute('/en/estimates/shared/tok-1234567890')

    const list = await screen.findByRole('region', { name: 'Materials: Standard materials' })
    const link = await within(list).findByRole('link', { name: /Cheapest: 3 × 25 kg bag at Demo Stroy — 7[\s,.]?500 ֏/ })
    expect(link).toHaveAttribute('href', '/en/material-prices/finishing-putty')
    expect(within(list).queryByText(/Cheapest:.*paint/i)).not.toBeInTheDocument()
    expect(requests).toEqual([{ grade: 'Standard', materials: [{ materialId: 'm-putty', quantity: 60 }, { materialId: 'm-paint', quantity: 9.5 }] }])

    const box = await screen.findByRole('region', { name: en.whereToBuy.title })
    expect(within(box).getByRole('link', { name: 'Demo Stroy' })).toHaveAttribute('href', '/en/stores/demo-stroy')
    expect(within(box).getAllByText(/has 1 of 2 materials/)).toHaveLength(2)
    expect(within(box).getByText(/9[\s,.]?100 ֏/)).toBeInTheDocument()
    expect(within(box).getByRole('link', { name: en.whereToBuy.compare })).toHaveAttribute('href', '/en/material-prices')
    expect(requests).toHaveLength(1) // the list and the box share one request
  }, SLOW)

  it('makes a shopping list to copy or send, per cheapest store or all at one store', async () => {
    const user = userEvent.setup()
    const writeText = vi.fn().mockResolvedValue(undefined)
    Object.defineProperty(navigator, 'clipboard', { value: { writeText }, configurable: true })
    server.use(
      http.get('*/api/v1/estimates/shared/tok-1234567890', () =>
        HttpResponse.json({ title: 'Our flat', cityId: null, oldBuilding: false, updatedAt: '2026-10-01T10:00:00Z', rooms: [], measurement: MEASUREMENT, materials: 'Standard' }),
      ),
      http.post('*/api/v1/materials/where-to-buy', () => HttpResponse.json(OFFERS)),
    )
    renderRoute('/en/estimates/shared/tok-1234567890')

    const box = await screen.findByRole('region', { name: en.whereToBuy.title })
    await user.click(within(box).getByRole('button', { name: en.whereToBuy.list.open }))
    const dialog = await screen.findByRole('dialog', { name: 'Shopping list: Standard materials' })
    expect(within(dialog).getByRole('region', { name: 'Demo Stroy' })).toHaveTextContent(/Finishing putty — 3 × 25 kg bag — 7[\s,.]?500 ֏/)
    expect(within(dialog).getByRole('region', { name: en.whereToBuy.list.missing })).toHaveTextContent('Interior paint (9.5 l)')

    await user.click(within(dialog).getByRole('button', { name: en.whereToBuy.list.copy }))
    expect(writeText).toHaveBeenCalledTimes(1)
    expect(writeText.mock.calls[0][0]).toMatch(/^Shopping list: Standard materials\n\nDemo Stroy\n• Finishing putty/)
    expect(within(dialog).getByRole('status')).toHaveTextContent(en.whereToBuy.list.copied)

    await user.click(within(dialog).getByRole('radio', { name: en.whereToBuy.list.oneStore }))
    await user.selectOptions(within(dialog).getByLabelText(en.whereToBuy.list.store), 'demo-home-center')
    expect(within(dialog).getByRole('region', { name: 'Demo Home Center' })).toHaveTextContent(/Interior paint — 1 × 10 l — 9[\s,.]?100 ֏/)
    expect(within(dialog).getByRole('region', { name: en.whereToBuy.list.missing })).toHaveTextContent('Finishing putty (60 kg)')
    const whatsapp = within(dialog).getByRole('link', { name: en.whereToBuy.list.whatsapp })
    expect(whatsapp.getAttribute('href')).toContain(`https://wa.me/?text=${encodeURIComponent('Shopping list: Standard materials\n\nDemo Home Center')}`)
    expect(document.body).toHaveClass('shopping-list-printing')

    await user.click(within(dialog).getByRole('button', { name: en.common.close }))
    expect(document.body).not.toHaveClass('shopping-list-printing')
    delete navigator.clipboard
  }, SLOW)

  it('shows nothing extra when no store sells the materials, or the request fails', async () => {
    server.use(
      http.get('*/api/v1/estimates/shared/tok-1234567890', () =>
        HttpResponse.json({ title: 'Our flat', cityId: null, oldBuilding: false, updatedAt: '2026-10-01T10:00:00Z', rooms: [], measurement: MEASUREMENT, materials: 'Standard' }),
      ),
      http.post('*/api/v1/materials/where-to-buy', () => HttpResponse.json({ title: 'Boom' }, { status: 500 })),
    )
    renderRoute('/en/estimates/shared/tok-1234567890')

    const list = await screen.findByRole('region', { name: 'Materials: Standard materials' })
    expect(within(list).getByText('Finishing putty')).toBeInTheDocument()
    await new Promise((resolve) => setTimeout(resolve, 300))
    expect(screen.queryByRole('region', { name: en.whereToBuy.title })).not.toBeInTheDocument()
    expect(within(list).queryByText(/Cheapest:/)).not.toBeInTheDocument()
  }, SLOW)

  it('lists the best stores under a quick estimate with materials', async () => {
    const user = userEvent.setup()
    server.use(
      http.post('*/api/v1/estimates/quick', () => HttpResponse.json(MEASUREMENT)),
      http.post('*/api/v1/materials/where-to-buy', () => HttpResponse.json(OFFERS)),
    )
    renderRoute('/en')

    const section = await screen.findByRole('region', { name: en.estimator.quick.title })
    await user.type(within(section).getByLabelText(en.estimator.area), '14')
    await user.selectOptions(within(section).getByLabelText(en.estimator.materials.label), 'Standard')
    await user.click(within(section).getByRole('button', { name: en.estimator.quick.calculate }))

    const box = await within(section).findByRole('region', { name: en.whereToBuy.title })
    expect(within(box).getByRole('link', { name: 'Demo Home Center' })).toBeInTheDocument()
  }, SLOW)
})
