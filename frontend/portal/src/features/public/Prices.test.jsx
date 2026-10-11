import { screen, waitFor, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { http, HttpResponse } from 'msw'
import en from '@/i18n/locales/en/common.json'
import { publicPartner } from '@/test/fixtures'
import { renderRoute } from '@/test/renderWithProviders'
import { server } from '@/test/server'

const SLOW = 30_000
const g = en.public.priceGuide

const WORK_ITEMS = [
  { id: 'w-boiler', categoryId: 'cat-boilers', slug: 'boiler', name: 'Boiler installation', unit: 'Piece', surface: 'None', priceMin: 20000, priceTypical: 30000, priceMax: 40000, partnerCount: 6 },
  { id: 'w-service', categoryId: 'cat-boilers', slug: 'service', name: 'Boiler service', unit: 'Fixed', surface: 'None', priceMin: 8000, priceTypical: 10000, priceMax: 12000, partnerCount: null },
  { id: 'w-new', categoryId: 'cat-boilers', slug: 'new', name: 'Not priced yet', unit: 'Piece', surface: 'None', priceMin: null, priceTypical: null, priceMax: null },
]

const price = (overrides) => ({
  workItemId: 'w-1',
  name: 'Leak repair',
  unit: 'Fixed',
  categoryId: 'cat-leaks',
  categoryName: 'Leaks',
  mainCategoryId: 'cat-plumbing',
  mainCategoryName: 'Plumbing',
  priceFrom: 8000,
  priceTo: null,
  includesMaterials: false,
  marketMin: 10000,
  marketTypical: 15000,
  marketMax: 20000,
  comparison: 'Below',
  ...overrides,
})

describe('Renovation prices', () => {
  it('lists the usual price of each kind of work by service, and finds work', async () => {
    server.use(http.get('*/api/v1/work-items', () => HttpResponse.json(WORK_ITEMS)))
    const user = userEvent.setup()
    renderRoute('/en/renovation-prices')

    expect(await screen.findByRole('heading', { level: 1, name: g.title })).toBeInTheDocument()
    const heating = await screen.findByRole('region', { name: 'Boilers' })
    expect(within(heating).getByText('Boiler installation')).toBeInTheDocument()
    expect(within(heating).getByText(g.fromSpecialists.replace('{{n}}', '6'))).toBeInTheDocument()
    expect(within(heating).getByText(g.ourEstimate)).toBeInTheDocument()
    expect(within(heating).getByText(/20[\s,.]?000 ֏ – 40[\s,.]?000 ֏/)).toBeInTheDocument()
    expect(within(heating).queryByText('Not priced yet')).not.toBeInTheDocument()
    expect(document.title).toBe(g.documentTitle)

    await user.type(screen.getByLabelText(g.search), 'service')
    await waitFor(() => expect(within(heating).queryByText('Boiler installation')).not.toBeInTheDocument())
    expect(within(heating).getByText('Boiler service')).toBeInTheDocument()

    await user.clear(screen.getByLabelText(g.search))
    await user.type(screen.getByLabelText(g.search), 'zzz')
    expect(await screen.findByText(g.none)).toBeInTheDocument()
  }, SLOW)

  it('shows a region’s own prices where it has them', async () => {
    const asked = []
    server.use(
      http.get('*/api/v1/work-items', ({ request }) => {
        const region = new URL(request.url).searchParams.get('region')
        asked.push(region)
        return HttpResponse.json(
          region === 'ararat' ? [{ ...WORK_ITEMS[0], priceMin: 15000, priceTypical: 25000, priceMax: 35000, partnerCount: 5, regional: true }, WORK_ITEMS[1]] : WORK_ITEMS,
        )
      }),
    )
    const user = userEvent.setup()
    renderRoute('/en/renovation-prices')

    const heating = await screen.findByRole('region', { name: 'Boilers' })
    expect(within(heating).getByText(g.fromSpecialists.replace('{{n}}', '6'))).toBeInTheDocument()
    await user.selectOptions(screen.getByLabelText(g.region), 'ararat')

    expect(await within(await screen.findByRole('region', { name: 'Boilers' })).findByText(g.fromLocalSpecialists.replace('{{n}}', '5'))).toBeInTheDocument()
    expect(asked[0]).toBeNull()
    expect(asked.at(-1)).toBe('ararat')
  }, SLOW)

  it('is linked from the footer', async () => {
    renderRoute('/en')
    expect(await screen.findByRole('link', { name: en.layout.links.prices })).toHaveAttribute('href', '/en/renovation-prices')
  }, SLOW)
})

describe('Prices on a partner profile', () => {
  it('shows the partner’s prices next to the market', async () => {
    server.use(
      http.get('*/api/v1/partners/aram-plumbing', () => HttpResponse.json(publicPartner())),
      http.get('*/api/v1/partners/aram-plumbing/prices', () =>
        HttpResponse.json([
          price(),
          price({ workItemId: 'w-2', name: 'Tap installation', priceFrom: 6000, priceTo: 8000, unit: 'Piece', comparison: 'Typical' }),
          price({ workItemId: 'w-3', name: 'Pipe replacement', includesMaterials: true, comparison: null }),
        ]),
      ),
    )
    renderRoute('/en/partners/aram-plumbing')

    const plumbing = await screen.findByRole('region', { name: 'Plumbing' })
    const leak = within(plumbing).getByText('Leak repair').closest('li')
    expect(leak).toHaveTextContent(/8[\s,.]?000 ֏ \/ job/)
    expect(within(leak).getByText(en.public.prices.comparison.Below)).toBeInTheDocument()
    expect(within(leak).getByText(/Market: 10[\s,.]?000 ֏ – 20[\s,.]?000 ֏/)).toBeInTheDocument()
    expect(within(plumbing).getByText(en.public.prices.comparison.Typical)).toBeInTheDocument()
    expect(within(plumbing).getByText(en.public.prices.withMaterials)).toBeInTheDocument()
  }, SLOW)

  it('shows nothing while the partner has no prices', async () => {
    server.use(http.get('*/api/v1/partners/aram-plumbing', () => HttpResponse.json(publicPartner())))
    renderRoute('/en/partners/aram-plumbing')

    expect(await screen.findByRole('heading', { level: 1, name: 'Aram Plumbing' })).toBeInTheDocument()
    expect(screen.queryByText(en.public.prices.hint)).not.toBeInTheDocument()
  }, SLOW)
})
