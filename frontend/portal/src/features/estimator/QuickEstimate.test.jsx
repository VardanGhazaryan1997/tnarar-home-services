import { screen, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { http, HttpResponse } from 'msw'
import en from '@/i18n/locales/en/common.json'
import { problem } from '@/test/auth'
import { renderRoute } from '@/test/renderWithProviders'
import { server } from '@/test/server'
import { parseArea } from './estimatorApi'

const line = (overrides) => ({
  workItemId: 'w1',
  name: 'Wall painting',
  unit: 'SquareMeter',
  surface: 'Wall',
  measuredQuantity: 30,
  quantity: 30,
  needsQuantity: false,
  priceMin: 30000,
  priceTypical: 45000,
  priceMax: 60000,
  factor: 1,
  ...overrides,
})

const estimate = (lines, totals) => ({
  rooms: [{ type: 'Bedroom', floorArea: 14, perimeter: 15, wallArea: 37, ceilingArea: 14, skirtingLength: 14, lines, ...totals }],
  unpricedLines: 0,
  ...totals,
})

function answerWith(body, requests = []) {
  server.use(
    http.post('/api/v1/estimates/quick', async ({ request }) => {
      requests.push(await request.json())
      return typeof body === 'function' ? body() : HttpResponse.json(body)
    }),
  )
  return requests
}


describe('QuickEstimate', () => {
  it('prices a room from its type and floor area', async () => {
    const user = userEvent.setup()
    const requests = answerWith(
      estimate(
        [line(), line({ workItemId: 'w2', name: 'Socket installation', unit: 'Piece', quantity: 4, priceMin: 8000, priceTypical: 10000, priceMax: 14000 }), line({ workItemId: 'w3', name: 'Unpriced', priceMin: null, priceTypical: null, priceMax: null })],
        { totalMin: 38000, totalTypical: 55000, totalMax: 74000 },
      ),
    )
    renderRoute('/en')

    const section = await screen.findByRole('region', { name: en.estimator.quick.title })
    await user.selectOptions(within(section).getByLabelText(en.estimator.roomType), 'Kitchen')
    await user.type(within(section).getByLabelText(en.estimator.area), '14,5')
    await user.click(within(section).getByLabelText(en.estimator.oldBuilding))
    await user.click(within(section).getByRole('button', { name: en.estimator.quick.calculate }))

    expect(await within(section).findByText(en.estimator.quick.resultLabel)).toBeInTheDocument()
    expect(requests).toEqual([{ roomType: 'Kitchen', area: 14.5, oldBuilding: true }])
    expect(section).toHaveTextContent(/38[\s,.]?000/)
    expect(section).toHaveTextContent(/74[\s,.]?000/)
    expect(within(section).getByText(/Wall painting/)).toBeInTheDocument()
    expect(within(section).getByText(/Socket installation/)).toHaveTextContent('4 pcs')
    expect(within(section).queryByText(/Unpriced/)).not.toBeInTheDocument()
    expect(within(section).getByText(en.estimator.quick.included.replace('{{count}}', '2'))).toBeInTheDocument()
    expect(within(section).getByRole('link', { name: en.estimator.quick.cta })).toHaveAttribute('href', '/en/requests/new')
  })

  it('asks for a sensible area before calling the server', async () => {
    const user = userEvent.setup()
    const requests = answerWith(estimate([], { totalMin: 0, totalTypical: 0, totalMax: 0 }))
    renderRoute('/en')

    const section = await screen.findByRole('region', { name: en.estimator.quick.title })
    await user.type(within(section).getByLabelText(en.estimator.area), 'abc')
    await user.click(within(section).getByRole('button', { name: en.estimator.quick.calculate }))

    expect(within(section).getByText(en.estimator.errors.area.replace('{{max}}', '2000'))).toBeInTheDocument()
    expect(requests).toEqual([])
  })

  it('says when there are no prices yet', async () => {
    const user = userEvent.setup()
    answerWith(estimate([line({ priceMin: null, priceTypical: null, priceMax: null })], { totalMin: 0, totalTypical: 0, totalMax: 0 }))
    renderRoute('/en')

    const section = await screen.findByRole('region', { name: en.estimator.quick.title })
    await user.type(within(section).getByLabelText(en.estimator.area), '10')
    await user.click(within(section).getByRole('button', { name: en.estimator.quick.calculate }))

    expect(await within(section).findByText(en.estimator.quick.noPrices)).toBeInTheDocument()
  })

  it('shows server errors', async () => {
    const user = userEvent.setup()
    answerWith(() => problem(500, 'server.error'))
    renderRoute('/en')

    const section = await screen.findByRole('region', { name: en.estimator.quick.title })
    await user.type(within(section).getByLabelText(en.estimator.area), '10')
    await user.click(within(section).getByRole('button', { name: en.estimator.quick.calculate }))

    expect(await within(section).findByRole('alert')).toBeInTheDocument()
  })
})

describe('parseArea', () => {
  it.each([
    ['12', 12],
    ['12.5', 12.5],
    [' 12,5 ', 12.5],
  ])('reads %j', (text, value) => expect(parseArea(text)).toBe(value))

  it.each(['', 'abc', '-3', '1e3', '12.'])('rejects %j', (text) => expect(parseArea(text)).toBeNaN())
})
