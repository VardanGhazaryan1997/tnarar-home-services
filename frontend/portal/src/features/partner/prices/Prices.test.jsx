import { screen, waitFor, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { http, HttpResponse } from 'msw'
import en from '@/i18n/locales/en/common.json'
import { CUSTOMER, PARTNER_USER, problem, signedInAs } from '@/test/auth'
import { priceList } from '@/test/fixtures'
import { renderRoute } from '@/test/renderWithProviders'
import { server } from '@/test/server'

const p = en.partner.prices

/** A fake price-list API: answers GET with `list` and records PUT bodies, answering with the saved prices. */
function pricesApi(list = priceList()) {
  const saves = []
  let current = list
  server.use(
    http.get('*/api/v1/me/partner-profile/prices', () => HttpResponse.json(current)),
    http.put('*/api/v1/me/partner-profile/prices', async ({ request }) => {
      const body = await request.json()
      saves.push(body)
      const byId = Object.fromEntries(body.prices.map((price) => [price.workItemId, price]))
      current = {
        items: current.items.map((item) => ({
          ...item,
          priceFrom: byId[item.workItemId]?.priceFrom ?? null,
          priceTo: byId[item.workItemId]?.priceTo ?? null,
          includesMaterials: byId[item.workItemId]?.includesMaterials ?? false,
        })),
      }
      return HttpResponse.json(current)
    }),
  )
  return saves
}

const row = (name) => screen.getByRole('listitem', { name })

describe('My prices page', () => {
  beforeEach(() => signedInAs(PARTNER_USER))

  it('lists the work by category with the market range and the saved prices', async () => {
    pricesApi()
    renderRoute('/en/prices')

    expect(await screen.findByRole('heading', { level: 1, name: p.title })).toBeInTheDocument()
    expect(screen.getByRole('link', { name: en.requests.tabs.prices })).toHaveAttribute('aria-current', 'page')
    expect(await screen.findByRole('heading', { level: 3, name: 'Plumbing' })).toBeInTheDocument()
    expect(screen.getByRole('heading', { level: 4, name: 'Fixture installation' })).toBeInTheDocument()

    const toilet = row('Toilet installation')
    expect(within(toilet).getByText(p.per.Piece)).toBeInTheDocument()
    expect(within(toilet).getByText(/Usually 10,000 ֏ – 22,000 ֏/)).toBeInTheDocument()
    expect(within(toilet).getByLabelText(p.from)).toHaveValue('14000')
    expect(within(toilet).getAllByRole('textbox')[1]).toHaveValue('18000')
    expect(within(row('Leak repair')).getByText(p.noMarket)).toBeInTheDocument()
    expect(screen.getByText('1 of 3 priced')).toBeInTheDocument()
    expect(screen.getAllByRole('button', { name: p.save })[0]).toBeDisabled()
  })

  it('saves prices, ranges and the materials flag', async () => {
    const saves = pricesApi()
    const user = userEvent.setup()
    renderRoute('/en/prices')

    const faucet = await screen.findByRole('listitem', { name: 'Faucet installation' })
    await user.type(within(faucet).getByLabelText(p.from), '7 000')
    await user.click(within(faucet).getByLabelText(p.materials))
    await user.click(within(row('Leak repair')).getByLabelText(p.from))
    await user.keyboard('12000')
    await user.click(screen.getAllByRole('button', { name: p.save })[0])

    await waitFor(() => expect(saves).toHaveLength(1))
    expect(saves[0].prices).toEqual([
      { workItemId: 'wi-toilet', priceFrom: 14000, priceTo: 18000, includesMaterials: false },
      { workItemId: 'wi-faucet', priceFrom: 7000, priceTo: null, includesMaterials: true },
      { workItemId: 'wi-leak', priceFrom: 12000, priceTo: null, includesMaterials: false },
    ])
    expect(await screen.findByText(p.saved)).toBeInTheDocument()
    expect(screen.getByText('3 of 3 priced')).toBeInTheDocument()
  })

  it('fills in usual prices and clears a price', async () => {
    const saves = pricesApi()
    const user = userEvent.setup()
    renderRoute('/en/prices')

    await user.click(await screen.findByRole('button', { name: p.fillTypical }))
    expect(within(row('Faucet installation')).getByLabelText(p.from)).toHaveValue('6000')
    expect(within(row('Toilet installation')).getByLabelText(p.from)).toHaveValue('14000') // already priced: kept
    await user.click(within(row('Toilet installation')).getByRole('button', { name: p.clear }))
    await user.click(screen.getAllByRole('button', { name: p.save })[0])

    await waitFor(() => expect(saves).toHaveLength(1))
    expect(saves[0].prices).toEqual([{ workItemId: 'wi-faucet', priceFrom: 6000, priceTo: null, includesMaterials: false }])
  })

  it('points out prices that need fixing before saving', async () => {
    const saves = pricesApi()
    const user = userEvent.setup()
    renderRoute('/en/prices')

    const faucet = await screen.findByRole('listitem', { name: 'Faucet installation' })
    await user.type(within(faucet).getByLabelText(p.from), '9000')
    const to = within(faucet).getAllByRole('textbox')[1]
    await user.type(to, '5000')
    await user.type(within(row('Leak repair')).getAllByRole('textbox')[1], '300')
    await user.click(screen.getAllByRole('button', { name: p.save })[0])

    expect(await screen.findByText(p.errors.fix)).toBeInTheDocument()
    expect(within(faucet).getByText(p.errors.toBelowFrom)).toBeInTheDocument()
    expect(within(row('Leak repair')).getByText(p.errors.fromRequired)).toBeInTheDocument()
    expect(saves).toHaveLength(0)
  })

  it('searches the list', async () => {
    pricesApi()
    const user = userEvent.setup()
    renderRoute('/en/prices')

    await user.type(await screen.findByLabelText(p.search), 'faucet')

    expect(screen.queryByRole('listitem', { name: 'Toilet installation' })).not.toBeInTheDocument()
    expect(row('Faucet installation')).toBeInTheDocument()
    await user.clear(screen.getByLabelText(p.search))
    await user.type(screen.getByLabelText(p.search), 'nothing like this')
    expect(screen.getByText(p.noMatches)).toBeInTheDocument()
  })

  it('shows the API error when saving fails', async () => {
    pricesApi()
    server.use(http.put('*/api/v1/me/partner-profile/prices', () => problem(422, 'partner_price.not_offered')))
    const user = userEvent.setup()
    renderRoute('/en/prices')

    await user.type(within(await screen.findByRole('listitem', { name: 'Faucet installation' })).getByLabelText(p.from), '7000')
    await user.click(screen.getAllByRole('button', { name: p.save })[0])

    expect(await screen.findByText(en.errors.partner_price.not_offered)).toBeInTheDocument()
  })

  it('asks to choose services when there is nothing to price', async () => {
    pricesApi({ items: [] })
    renderRoute('/en/prices')

    expect(await screen.findByText(p.emptyTitle)).toBeInTheDocument()
    expect(screen.getByRole('link', { name: p.chooseServices })).toHaveAttribute('href', '/en/partner?step=services')
  })

  it('asks to create a partner profile first', async () => {
    server.use(http.get('*/api/v1/me/partner-profile/prices', () => problem(404, 'partner.not_found')))
    renderRoute('/en/prices')

    expect(await screen.findByText(p.noProfileTitle)).toBeInTheDocument()
    expect(screen.getByRole('link', { name: p.createProfile })).toHaveAttribute('href', '/en/partner')
  })
})

describe('My prices for customers', () => {
  it('is only for partners', async () => {
    signedInAs(CUSTOMER)
    renderRoute('/en/prices')

    await waitFor(() => expect(screen.queryByRole('heading', { level: 1, name: p.title })).not.toBeInTheDocument())
  })
})
