import { screen, waitFor, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { http, HttpResponse } from 'msw'
import en from '@/i18n/locales/en/common.json'
import { materialDetail, materialRow, offerProduct, offerStore, storeDetail } from '@/test/fixtures'
import { renderRoute } from '@/test/renderWithProviders'
import { server } from '@/test/server'

const SLOW = 30_000
const m = en.materialPrices
const d = m.detail
const o = m.offer
const s = en.storePage

describe('Building material prices', () => {
  it('lists materials with the usual price per grade, the store count and the lowest price', async () => {
    renderRoute('/en/material-prices')

    expect(await screen.findByRole('heading', { level: 1, name: m.title })).toBeInTheDocument()
    const putty = await screen.findByRole('article', { name: 'Finishing putty' })
    expect(within(putty).getByRole('link', { name: 'Finishing putty' })).toHaveAttribute('href', '/en/material-prices/finishing-putty')
    expect(within(putty).getByText('150 ֏')).toBeInTheDocument()
    expect(within(putty).getByText('230 ֏')).toBeInTheDocument()
    expect(within(putty).getByText(new RegExp(m.fromStoresShort))).toBeInTheDocument()
    expect(within(putty).getByText(/From 3 stores · lowest 210 ֏ per kg/)).toBeInTheDocument()
    expect(within(await screen.findByRole('article', { name: 'Primer' })).getByText(m.noStores)).toBeInTheDocument()
    expect(document.title).toBe(m.documentTitle)
  }, SLOW)

  it('finds a material by name', async () => {
    const user = userEvent.setup()
    renderRoute('/en/material-prices')
    await screen.findByRole('article', { name: 'Primer' })

    await user.type(screen.getByLabelText(m.search), 'putty')
    await waitFor(() => expect(screen.queryByRole('article', { name: 'Primer' })).not.toBeInTheDocument())
    expect(screen.getByRole('article', { name: 'Finishing putty' })).toBeInTheDocument()

    await user.clear(screen.getByLabelText(m.search))
    await user.type(screen.getByLabelText(m.search), 'zzz')
    expect(await screen.findByText(m.none)).toBeInTheDocument()
  }, SLOW)

  it('keeps the region in the address and carries it to the comparison', async () => {
    const asked = []
    server.use(
      http.get('*/api/v1/materials', ({ request }) => {
        asked.push(new URL(request.url).searchParams.get('region'))
        return HttpResponse.json([materialRow()])
      }),
    )
    const user = userEvent.setup()
    const { router } = renderRoute('/en/material-prices')
    await screen.findByRole('article', { name: 'Finishing putty' })

    await user.selectOptions(screen.getByLabelText(m.region), 'ararat')
    await waitFor(() => expect(asked.at(-1)).toBe('ararat'))
    expect(router.state.location.search).toBe('?region=ararat')
    expect(screen.getByRole('link', { name: 'Finishing putty' })).toHaveAttribute('href', '/en/material-prices/finishing-putty?region=ararat')

    await user.selectOptions(screen.getByLabelText(m.region), '')
    await waitFor(() => expect(router.state.location.search).toBe(''))
    expect(asked[0]).toBeNull()
  }, SLOW)

  it('shows an error with a retry when the prices can not be loaded', async () => {
    server.use(http.get('*/api/v1/materials', () => HttpResponse.json({ status: 500, code: 'internal' }, { status: 500 })))
    renderRoute('/en/material-prices')
    expect(await screen.findByRole('button', { name: en.common.retry })).toBeInTheDocument()
  }, SLOW)

  it('is linked from the footer', async () => {
    renderRoute('/en/material-prices')
    expect(await screen.findByRole('link', { name: en.layout.links.materialPrices })).toHaveAttribute('href', '/en/material-prices')
  }, SLOW)
})

describe('Material comparison', () => {
  it('shows the usual prices and every store’s offer, marking the lowest price in stock', async () => {
    renderRoute('/en/material-prices/finishing-putty')

    expect(await screen.findByRole('heading', { level: 1, name: 'Finishing putty' })).toBeInTheDocument()
    expect(screen.getByText('230 ֏')).toBeInTheDocument()
    const cheap = await screen.findByRole('article', { name: 'Putty 25 kg — Demo Market' })
    expect(within(cheap).getByRole('link', { name: 'Demo Market' })).toHaveAttribute('href', '/en/stores/demo-market')
    expect(within(cheap).getByText('DemoBrand · 25 kg bag')).toBeInTheDocument()
    expect(within(cheap).getByText('5,250 ֏')).toBeInTheDocument()
    expect(within(cheap).getByText('210 ֏ per kg')).toBeInTheDocument()
    expect(within(cheap).getByText(o.lowest)).toBeInTheDocument()
    expect(within(cheap).getByText(o.inStock)).toBeInTheDocument()
    expect(within(cheap).getByText(/branches: Yerevan/)).toBeInTheDocument()

    const sale = screen.getByRole('article', { name: 'Putty 5 kg — Demo Home' })
    expect(within(sale).getByText('1,000 ֏')).toBeInTheDocument()
    expect(within(sale).getByText('1,500 ֏')).toBeInTheDocument()
    expect(within(sale).getByText(/Sale until/)).toBeInTheDocument()
    expect(within(sale).getByText(o.outOfStock)).toBeInTheDocument()
    expect(within(sale).queryByText(o.lowest)).not.toBeInTheDocument()
    expect(document.title).toBe(d.documentTitle.replace('{{name}}', 'Finishing putty'))
    expect(screen.getByText(d.offers.replace('{{n}}', '2'))).toBeInTheDocument()
  }, SLOW)

  it('shows the price from about three months ago when it was different, with its direction', async () => {
    server.use(
      http.get('*/api/v1/materials/finishing-putty', () =>
        HttpResponse.json(
          materialDetail({
            offers: [
              { store: offerStore(), product: offerProduct({ id: 'a', name: 'Dearer now', price: 5500, currentPrice: 5500, priceBefore: 5000 }) },
              { store: offerStore(), product: offerProduct({ id: 'b', name: 'Cheaper now', price: 4800, currentPrice: 4800, priceBefore: 5200 }) },
              { store: offerStore(), product: offerProduct({ id: 'c', name: 'Unchanged', price: 5000, currentPrice: 5000 }) },
            ],
            storeCount: 3,
          }),
        ),
      ),
    )
    renderRoute('/en/material-prices/finishing-putty')

    const dearer = await screen.findByRole('article', { name: /Dearer now/ })
    expect(within(dearer).getByText(/Was 5,000 ֏ about 3 months ago/)).toBeInTheDocument()
    expect(within(dearer).getByText(/↑/)).toBeInTheDocument()
    const cheaper = screen.getByRole('article', { name: /Cheaper now/ })
    expect(within(cheaper).getByText(/Was 5,200 ֏ about 3 months ago/)).toBeInTheDocument()
    expect(within(cheaper).getByText(/↓/)).toBeInTheDocument()
    expect(within(screen.getByRole('article', { name: /Unchanged/ })).queryByText(/about 3 months ago/)).not.toBeInTheDocument()
  }, SLOW)

  it('narrows the offers by quality, region and stock, keeping them in the address', async () => {
    const asked = []
    server.use(
      http.get('*/api/v1/materials/finishing-putty', ({ request }) => {
        asked.push(Object.fromEntries(new URL(request.url).searchParams))
        return HttpResponse.json(materialDetail())
      }),
    )
    const user = userEvent.setup()
    const { router } = renderRoute('/en/material-prices/finishing-putty')
    await screen.findByRole('article', { name: 'Putty 25 kg — Demo Market' })
    expect(asked[0]).toEqual({})

    await user.click(screen.getByRole('radio', { name: m.grades.Premium }))
    await waitFor(() => expect(asked.at(-1)).toEqual({ grade: 'Premium' }))
    expect(router.state.location.search).toBe('?grade=Premium')

    await user.selectOptions(screen.getByLabelText(m.region), 'ararat')
    await user.click(screen.getByRole('checkbox', { name: d.inStockOnly }))
    await waitFor(() => expect(asked.at(-1)).toEqual({ grade: 'Premium', region: 'ararat', inStock: 'true' }))
    expect(router.state.location.search).toBe('?grade=Premium&region=ararat&inStock=1')

    await user.click(screen.getByRole('radio', { name: d.allGrades }))
    await user.click(screen.getByRole('checkbox', { name: d.inStockOnly }))
    await waitFor(() => expect(asked.at(-1)).toEqual({ region: 'ararat' }))
  }, SLOW)

  it('reads the filters from the address and ignores an unknown grade', async () => {
    const asked = []
    server.use(
      http.get('*/api/v1/materials/finishing-putty', ({ request }) => {
        asked.push(Object.fromEntries(new URL(request.url).searchParams))
        return HttpResponse.json(materialDetail())
      }),
    )
    renderRoute('/en/material-prices/finishing-putty?grade=Gold&region=ararat&inStock=1')

    await screen.findByRole('article', { name: 'Putty 25 kg — Demo Market' })
    expect(asked[0]).toEqual({ region: 'ararat', inStock: 'true' })
    expect(screen.getByRole('link', { name: m.all })).toHaveAttribute('href', '/en/material-prices?region=ararat')
  }, SLOW)

  it('says when no offer matches, and shows the offer’s photo when it has one', async () => {
    server.use(http.get('*/api/v1/materials/finishing-putty', () => HttpResponse.json(materialDetail({ offers: [], storeCount: 0 }))))
    renderRoute('/en/material-prices/finishing-putty')
    expect(await screen.findByText(d.noOffers)).toBeInTheDocument()
  }, SLOW)

  it('shows a photo and no branches when the store has none', async () => {
    server.use(
      http.get('*/api/v1/materials/finishing-putty', () =>
        HttpResponse.json(
          materialDetail({ offers: [{ store: offerStore({ branches: [] }), product: offerProduct({ photoUrl: 'https://storage.test/p.jpg' }) }] }),
        ),
      ),
    )
    renderRoute('/en/material-prices/finishing-putty')

    const offer = await screen.findByRole('article', { name: 'Putty 25 kg — Demo Market' })
    expect(offer.querySelector('img')).toHaveAttribute('src', 'https://storage.test/p.jpg')
    expect(within(offer).queryByText(/branches:/)).not.toBeInTheDocument()
  }, SLOW)

  it('says when the material does not exist', async () => {
    server.use(http.get('*/api/v1/materials/nope', () => HttpResponse.json({ status: 404, code: 'material.not_found' }, { status: 404 })))
    renderRoute('/en/material-prices/nope')
    expect(await screen.findByText(m.notFound)).toBeInTheDocument()
    expect(screen.getByRole('link', { name: m.all })).toHaveAttribute('href', '/en/material-prices')
  }, SLOW)
})

describe('Store page', () => {
  it('shows the store, its branches and its prices by material', async () => {
    renderRoute('/en/stores/demo-market')

    expect(await screen.findByRole('heading', { level: 1, name: 'Demo Market' })).toBeInTheDocument()
    expect(screen.getByText('Building materials for every room')).toBeInTheDocument()
    expect(screen.getByRole('link', { name: s.website })).toHaveAttribute('href', 'https://demo-market.example')
    expect(screen.getByRole('link', { name: '+374 10 000000' })).toHaveAttribute('href', 'tel:+37410000000')
    expect(screen.getByText('Yerevan, Kentron, Komitas 1')).toBeInTheDocument()
    expect(screen.getByText('Mon–Sat 9–19')).toBeInTheDocument()
    expect(screen.getByText('Gyumri, Garegin 3')).toBeInTheDocument()
    expect(screen.getByRole('link', { name: 'Finishing putty' })).toHaveAttribute('href', '/en/material-prices/finishing-putty')
    expect(screen.getByText('Premium putty')).toBeInTheDocument()
    expect(screen.getByText('320 ֏ per kg')).toBeInTheDocument()
    expect(document.title).toBe(s.documentTitle.replace('{{name}}', 'Demo Market'))
  }, SLOW)

  it('copes with a store that has no description, contacts, branches or prices yet', async () => {
    server.use(
      http.get('*/api/v1/stores/bare', () =>
        HttpResponse.json(storeDetail({ slug: 'bare', name: 'Bare Store', description: null, website: null, phone: null, branches: [], materials: [] })),
      ),
    )
    renderRoute('/en/stores/bare')

    expect(await screen.findByRole('heading', { level: 1, name: 'Bare Store' })).toBeInTheDocument()
    expect(screen.getByText(s.noBranches)).toBeInTheDocument()
    expect(screen.getByText(s.noProducts)).toBeInTheDocument()
    expect(screen.queryByRole('link', { name: s.website })).not.toBeInTheDocument()
  }, SLOW)

  it('shows the logo when the store has one', async () => {
    server.use(http.get('*/api/v1/stores/demo-market', () => HttpResponse.json(storeDetail({ logoUrl: 'https://storage.test/logo.png' }))))
    renderRoute('/en/stores/demo-market')
    await screen.findByRole('heading', { level: 1, name: 'Demo Market' })
    expect(document.querySelector('img[src="https://storage.test/logo.png"]')).not.toBeNull()
  }, SLOW)

  it('says when the store does not exist', async () => {
    server.use(http.get('*/api/v1/stores/nope', () => HttpResponse.json({ status: 404, code: 'store.not_found' }, { status: 404 })))
    renderRoute('/en/stores/nope')
    expect(await screen.findByText(s.notFound)).toBeInTheDocument()
  }, SLOW)
})
