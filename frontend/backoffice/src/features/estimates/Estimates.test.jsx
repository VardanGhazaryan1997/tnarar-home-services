import { screen, waitFor, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { http, HttpResponse } from 'msw'
import i18n from '@/i18n'
import hy from '@/i18n/locales/hy/common.json'
import { problem, signedInAs, staffMember } from '@/test/auth'
import { requestDetail } from '@/test/operations'
import { renderRoute } from '@/test/renderWithProviders'
import { server } from '@/test/server'

const SLOW = 60_000
const e = hy.estimates

const ROW = {
  id: 'est-1',
  title: 'Մեր բնակարանը',
  owner: { userId: 'user-ani', phone: '+37491000111', fullName: 'Անի Պետրոսյան' },
  roomCount: 2,
  lineCount: 5,
  totalMin: 120000,
  totalTypical: 160000,
  totalMax: 210000,
  isShared: true,
  requestCount: 1,
  createdAt: '2026-10-05T09:00:00Z',
  updatedAt: null,
}

const line = (overrides) => ({
  workItemId: 'w-tiles',
  name: 'Սալիկապատում',
  unit: 'SquareMeter',
  surface: 'Floor',
  measuredQuantity: 4,
  quantity: 4,
  needsQuantity: false,
  priceMin: 20000,
  priceTypical: 26000,
  priceMax: 36000,
  factor: 1,
  ...overrides,
})

const DETAIL = {
  id: 'est-1',
  title: 'Մեր բնակարանը',
  owner: ROW.owner,
  cityId: null,
  oldBuilding: true,
  isShared: false,
  createdAt: '2026-10-05T09:00:00Z',
  updatedAt: '2026-10-06T09:00:00Z',
  rooms: [
    {
      id: 'r1',
      name: 'Լոգարան',
      type: 'Bathroom',
      length: null,
      width: null,
      area: 4,
      height: 2.7,
      openings: [{ kind: 'Door', width: 0.8, height: 2, count: 1 }],
      lines: [
        { workItemId: 'w-tiles', quantity: null },
        { workItemId: 'w-gone', quantity: 1 },
      ],
    },
  ],
  measurement: {
    rooms: [{ type: 'Bathroom', floorArea: 4, perimeter: 8, wallArea: 20, ceilingArea: 4, skirtingLength: 7.2, lines: [line()], totalMin: 20000, totalTypical: 26000, totalMax: 36000 }],
    totalMin: 20000,
    totalTypical: 26000,
    totalMax: 36000,
    unpricedLines: 0,
  },
  requests: [{ id: 'request-1', status: 'Open', categoryName: 'Վերանորոգում', lineCount: 1, createdAt: '2026-10-06T10:00:00Z' }],
}

function api() {
  const lists = []
  server.use(
    http.get('*/api/v1/admin/estimates', ({ request }) => {
      lists.push(Object.fromEntries(new URL(request.url).searchParams))
      return HttpResponse.json({ items: [ROW], page: 1, pageSize: 20, totalCount: 1 })
    }),
    http.get('*/api/v1/admin/estimates/stats', () =>
      HttpResponse.json({ total: 40, withRequest: 10, shared: 5, conversionPercent: 25, totalLast30Days: 12, withRequestLast30Days: 4 }),
    ),
    http.get('*/api/v1/admin/estimates/:id', ({ params }) => (params.id === 'est-1' ? HttpResponse.json(DETAIL) : problem(404, 'estimate.not_found'))),
  )
  return lists
}

describe('Estimates', () => {
  it('shows conversion and filters the list', async () => {
    const lists = api()
    const user = userEvent.setup()
    const { router } = renderRoute('/estimates')

    const table = await screen.findByRole('table')
    expect(await within(table).findByRole('link', { name: ROW.title })).toHaveAttribute('href', '/estimates/est-1')
    expect(within(table).getByText(/120[\s,.]?000 ֏ – 210[\s,.]?000 ֏/)).toBeInTheDocument()
    expect(within(table).getByText(e.shared)).toBeInTheDocument()
    expect(await screen.findByText('25')).toBeInTheDocument()
    expect(screen.getByText('4 / 12')).toBeInTheDocument()
    expect(lists[0]).toEqual({ page: '1', pageSize: '20' })

    await user.click(screen.getByText(e.filters.withoutRequest))
    await waitFor(() => expect(lists.at(-1)).toEqual({ withRequest: 'false', page: '1', pageSize: '20' }))
    expect(router.state.location.search).toBe('?show=withoutRequest')

    await user.click(screen.getByRole('combobox', { name: e.filters.shared }))
    await user.click(await screen.findByTitle(e.filters.shared_yes))
    await user.type(screen.getByRole('searchbox', { name: e.filters.search }), 'բնակ{Enter}')
    await waitFor(() => expect(lists.at(-1)).toEqual({ withRequest: 'false', shared: 'true', search: 'բնակ', page: '1', pageSize: '20' }))
  }, SLOW)

  it('shows an estimate with its rooms, prices and requests', async () => {
    api()
    renderRoute('/estimates/est-1')

    expect(await screen.findByRole('heading', { name: DETAIL.title })).toBeInTheDocument()
    expect(screen.getByText(e.oldBuilding)).toBeInTheDocument()
    const bath = screen.getByText(`Լոգարան · ${hy.catalog.roomTemplates.roomTypes.Bathroom}`).closest('.ant-card')
    expect(within(bath).getByText('Սալիկապատում')).toBeInTheDocument()
    expect(within(bath).getByText(`4 ${hy.catalog.workItems.units.SquareMeter}`)).toBeInTheDocument()
    expect(within(bath).getByText(e.gone.replace('{{n}}', '1'))).toBeInTheDocument()
    expect(screen.getByRole('link', { name: 'Վերանորոգում' })).toHaveAttribute('href', '/requests/request-1')
  }, SLOW)

  it('is in the menu for staff who can see requests, and says when an estimate is gone', async () => {
    api()
    signedInAs(staffMember(['requests.view']))
    renderRoute('/estimates/nope')

    expect(await screen.findByText(hy.errors.estimate.not_found)).toBeInTheDocument()
    expect(screen.getByRole('menuitem', { name: new RegExp(hy.nav.estimates) })).toBeInTheDocument()
  }, SLOW)

  it('shows the work of a request made from an estimate', async () => {
    server.use(
      http.get('*/api/v1/admin/requests/request-1', () =>
        HttpResponse.json(
          requestDetail({
            estimateId: 'est-1',
            lines: [{ id: 'l1', roomName: 'Լոգարան', workItemId: 'w-tiles', name: 'Սալիկապատում', unit: 'SquareMeter', quantity: 4, estimateMin: 20000, estimateMax: 36000 }],
            materialGrade: 'Standard',
            materials: [{ materialId: 'm-tiles', name: 'Սալիկ', unit: 'SquareMeter', quantity: 4.4, estimateCost: 39600 }],
          }),
        ),
      ),
    )
    renderRoute('/requests/request-1')

    const card = (await screen.findByText(hy.requests.detail.lines)).closest('.ant-card')
    expect(within(card).getByText('Սալիկապատում')).toBeInTheDocument()
    expect(within(card).getByText(/20[\s,.]?000 ֏ – 36[\s,.]?000 ֏/)).toBeInTheDocument()
    expect(within(card).getByRole('link', { name: hy.requests.detail.openEstimate })).toHaveAttribute('href', '/estimates/est-1')
    const materials = screen.getByText(i18n.t('requests.detail.materials', { grade: hy.estimates.grades.Standard })).closest('.ant-card')
    expect(within(materials).getByText('Սալիկ')).toBeInTheDocument()
    expect(within(materials).getByText(/39[\s,.]?600 ֏/)).toBeInTheDocument()
  }, SLOW)
})
