import { screen, waitFor, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { http, HttpResponse } from 'msw'
import en from '@/i18n/locales/en/common.json'
import { problem } from '@/test/auth'
import { page, partnerCard, publicPartner, publicReview } from '@/test/fixtures'
import { renderRoute } from '@/test/renderWithProviders'
import { server } from '@/test/server'

/** Answers partner searches with `results` and records each query string. */
function searchApi(results = (params) => page([partnerCard()], { pageSize: Number(params.get('pageSize')) })) {
  const calls = []
  server.use(
    http.get('*/api/v1/partners', ({ request }) => {
      const params = new URL(request.url).searchParams
      calls.push(Object.fromEntries(params))
      return HttpResponse.json(results(params))
    }),
  )
  return calls
}

const where = (router) => `${router.state.location.pathname}${router.state.location.search}`

describe('Home page', () => {
  it('searches by service and city, and shows categories, new partners and the partner invitation', async () => {
    const user = userEvent.setup()
    const calls = searchApi()
    const { router } = renderRoute('/en')

    expect(await screen.findByRole('heading', { level: 1, name: en.home.title })).toBeInTheDocument()
    const services = await screen.findByRole('list', { name: en.home.servicesTitle })
    expect(within(services).getByRole('link', { name: 'Plumbing' })).toHaveAttribute('href', '/en/services/plumbing')
    expect(await screen.findByRole('link', { name: 'Aram Plumbing' })).toHaveAttribute('href', '/en/partners/aram-plumbing')
    expect(calls[0]).toMatchObject({ pageSize: '4' })
    expect(screen.getByRole('link', { name: en.home.join.cta })).toHaveAttribute('href', '/en/how-it-works?for=partners')

    const search = screen.getByRole('search', { name: en.public.search.label })
    await user.selectOptions(within(search).getByLabelText(en.public.search.what), 'boilers')
    await user.selectOptions(within(search).getByLabelText(en.public.search.where), 'yerevan')
    await user.click(within(search).getByRole('button', { name: en.public.search.submit }))
    await waitFor(() => expect(where(router)).toBe('/en/services/boilers?city=yerevan'))
  })

  it('searches everyone when nothing is chosen', async () => {
    const user = userEvent.setup()
    const { router } = renderRoute('/en')
    await user.click(await screen.findByRole('button', { name: en.public.search.submit }))
    await waitFor(() => expect(where(router)).toBe('/en/search'))
  })
})

describe('Services page', () => {
  it('lists every category with its subcategories', async () => {
    renderRoute('/en/services')

    expect(await screen.findByRole('link', { name: 'Heating' })).toHaveAttribute('href', '/en/services/heating')
    expect(screen.getByRole('link', { name: 'Boilers' })).toHaveAttribute('href', '/en/services/boilers')
    expect(screen.getByRole('link', { name: en.public.services.everyone })).toHaveAttribute('href', '/en/search')
  })
})

describe('Search page', () => {
  it('shows a category with its subcategories and sends the filters from the address', async () => {
    const calls = searchApi()
    renderRoute('/en/services/heating?city=yerevan&district=kentron&type=Company&q=aram')

    expect(await screen.findByRole('heading', { level: 1, name: 'Heating' })).toBeInTheDocument()
    expect(await screen.findByText(en.public.search.found.replace('{{n}}', '1'))).toBeInTheDocument()
    await waitFor(() => expect(calls.at(-1)).toEqual({ category: 'heating', city: 'yerevan', district: 'kentron', type: 'Company', search: 'aram', page: '1', pageSize: '12' }))

    const chips = screen.getByRole('navigation', { name: en.public.search.subcategories })
    expect(within(chips).getByRole('link', { name: 'All: Heating' })).toHaveAttribute('aria-current', 'page')
    expect(within(chips).getByRole('link', { name: 'Boilers' })).toHaveAttribute('href', '/en/services/boilers?city=yerevan&district=kentron&type=Company&q=aram')
    expect(screen.getByRole('button', { name: en.public.search.filtersCount.replace('{{n}}', '4') })).toHaveAttribute('aria-expanded', 'false')

    const card = screen.getByRole('article')
    expect(within(card).getByRole('link', { name: 'Aram Plumbing' })).toHaveAttribute('href', '/en/partners/aram-plumbing')
    expect(card).toHaveTextContent('20 yrs experience')
    expect(card).toHaveTextContent('Works: 3')
  })

  it('changes filters in the address, resets the page and clears them', async () => {
    const user = userEvent.setup()
    const calls = searchApi((params) => page([partnerCard()], { page: Number(params.get('page')), pageSize: 12, totalCount: 30 }))
    const { router } = renderRoute('/en/search')

    expect(await screen.findByRole('heading', { level: 1, name: en.public.search.title })).toBeInTheDocument()
    await user.click(await screen.findByRole('button', { name: en.pagination.next }))
    await waitFor(() => expect(where(router)).toBe('/en/search?page=2'))

    await user.click(screen.getByRole('button', { name: en.public.search.filters }))
    expect(screen.getByLabelText(en.public.search.district)).toBeDisabled()
    await user.selectOptions(screen.getByLabelText(en.public.search.city), 'yerevan')
    await waitFor(() => expect(where(router)).toBe('/en/search?city=yerevan'))
    await user.selectOptions(screen.getByLabelText(en.public.search.district), 'kentron')
    await user.selectOptions(screen.getByLabelText(en.public.search.type), 'Company')
    await user.type(screen.getByLabelText(en.public.search.name), 'Aram')
    await user.click(screen.getByRole('button', { name: en.public.search.submit }))
    await waitFor(() => expect(where(router)).toBe('/en/search?city=yerevan&district=kentron&type=Company&q=Aram'))
    await waitFor(() => expect(calls.at(-1)).toMatchObject({ city: 'yerevan', district: 'kentron', type: 'Company', search: 'Aram' }))

    await user.click(screen.getByRole('button', { name: en.public.search.clear }))
    await waitFor(() => expect(where(router)).toBe('/en/search'))
    expect(screen.getByLabelText(en.public.search.name)).toHaveValue('')
  })

  it('suggests a request when no one matches, and explains unknown services', async () => {
    searchApi(() => page([]))
    const { unmount } = renderRoute('/en/services/plumbing')

    expect(await screen.findByText(en.public.search.emptyTitle)).toBeInTheDocument()
    expect(screen.getByRole('link', { name: en.home.cta })).toHaveAttribute('href', '/en/requests/new?category=cat-plumbing')
    unmount()

    renderRoute('/en/services/roofing')
    expect(await screen.findByText(en.public.search.unknownCategory)).toBeInTheDocument()
    expect(within(screen.getByRole('main')).getByRole('link', { name: en.public.services.title })).toHaveAttribute('href', '/en/services')
  })

  it('shows search errors with a retry', async () => {
    server.use(http.get('*/api/v1/partners', () => problem(500, 'server')))
    renderRoute('/en/search')
    expect(await screen.findByRole('button', { name: en.common.retry })).toBeInTheDocument()
  })
})

describe('Partner profile', () => {
  it('shows who they are, where they work and their work, and sends a request to them', async () => {
    server.use(http.get('*/api/v1/partners/aram-plumbing', () => HttpResponse.json(publicPartner())))
    renderRoute('/en/partners/aram-plumbing')

    expect(await screen.findByRole('heading', { level: 1, name: 'Aram Plumbing' })).toBeInTheDocument()
    expect(screen.getByText('20 yrs experience')).toBeInTheDocument()
    expect(screen.getByText(/On Tnarar since 2026/)).toBeInTheDocument()
    expect(screen.getByText(/Clean and on time/)).toBeInTheDocument()
    expect(screen.getByRole('link', { name: 'Boilers' })).toHaveAttribute('href', '/en/services/boilers')
    expect(screen.getByText('Kentron, Arabkir')).toBeInTheDocument()
    expect(screen.getByText(en.public.profile.wholeCity)).toBeInTheDocument()
    for (const link of screen.getAllByRole('link', { name: en.public.profile.request })) {
      expect(link).toHaveAttribute('href', '/en/requests/new?partner=partner-1&name=Aram%20Plumbing')
    }
    expect(document.body).not.toHaveTextContent('+374')
  })

  it('shows the rating and pages through reviews with the partner replies', async () => {
    const user = userEvent.setup()
    const pages = []
    server.use(
      http.get('*/api/v1/partners/aram-plumbing', () => HttpResponse.json(publicPartner({ rating: 4.5, reviewCount: 6 }))),
      http.get('*/api/v1/partners/aram-plumbing/reviews', ({ request }) => {
        const number = Number(new URL(request.url).searchParams.get('page'))
        pages.push(number)
        const items =
          number === 1
            ? [publicReview({ reply: 'Thanks, Ani!' }), publicReview({ id: 'review-2', customerName: null, text: null, rating: 5 })]
            : [publicReview({ id: 'review-6', text: 'Came on time.' })]
        return HttpResponse.json(page(items, { page: number, pageSize: 5, totalCount: 6 }))
      }),
    )
    renderRoute('/en/partners/aram-plumbing')

    expect(await screen.findByRole('img', { name: '4.5 out of 5' })).toBeInTheDocument()
    expect(screen.getByText('(6 reviews)')).toBeInTheDocument()
    const reviews = (await screen.findByRole('heading', { name: 'Reviews (6)' })).closest('section')
    expect(await within(reviews).findByText('Fixed the leak in an hour.')).toBeInTheDocument()
    expect(within(reviews).getByText('Thanks, Ani!')).toBeInTheDocument()
    expect(within(reviews).getByText(en.reviews.anonymous)).toBeInTheDocument()

    await user.click(within(reviews).getByRole('button', { name: en.pagination.next }))
    expect(await within(reviews).findByText('Came on time.')).toBeInTheDocument()
    expect(pages).toEqual([1, 2])
  })

  it('opens work examples large, one after another', async () => {
    const user = userEvent.setup()
    server.use(http.get('*/api/v1/partners/aram-plumbing', () => HttpResponse.json(publicPartner())))
    renderRoute('/en/partners/aram-plumbing')

    const gallery = await screen.findByRole('list', { name: en.public.profile.workList })
    await user.click(within(gallery).getAllByRole('button')[0])
    let dialog = screen.getByRole('dialog', { name: 'Work 1 of 2' })
    expect(within(dialog).getByRole('img', { name: 'A new bathroom' })).toHaveAttribute('src', 'https://files/bathroom.jpg')
    expect(within(dialog).getByText('A new bathroom')).toBeInTheDocument()

    await user.click(within(dialog).getByRole('button', { name: en.public.profile.next }))
    dialog = screen.getByRole('dialog', { name: 'Work 2 of 2' })
    expect(dialog.querySelector('video')).toHaveAttribute('src', 'https://files/boiler.mp4')
    await user.click(within(dialog).getByRole('button', { name: en.public.profile.next }))
    expect(screen.getByRole('dialog', { name: 'Work 1 of 2' })).toBeInTheDocument()
    await user.click(screen.getByRole('button', { name: en.public.profile.previous }))
    expect(screen.getByRole('dialog', { name: 'Work 2 of 2' })).toBeInTheDocument()
    await user.keyboard('{Escape}')
    expect(screen.queryByRole('dialog')).not.toBeInTheDocument()
  })

  it('handles a profile with little in it, and missing profiles', async () => {
    server.use(
      http.get('*/api/v1/partners/new-one', () =>
        HttpResponse.json(publicPartner({ slug: 'new-one', type: 'Company', about: '', yearsOfExperience: null, avatar: null, memberSince: null, workExamples: [] })),
      ),
      http.get('*/api/v1/partners/gone', () => problem(404, 'partner.not_found')),
    )
    const { unmount } = renderRoute('/en/partners/new-one')
    expect(await screen.findByText(en.public.profile.noWork)).toBeInTheDocument()
    expect(screen.getByText(en.public.partnerType.Company)).toBeInTheDocument()
    expect(screen.queryByRole('heading', { name: en.public.profile.about })).not.toBeInTheDocument()
    unmount()

    renderRoute('/en/partners/gone')
    expect(await screen.findByText(en.public.profile.notFound)).toBeInTheDocument()
  })
})

describe('How it works', () => {
  const faqs = [
    { id: 'f1', question: 'Is it free?', answer: 'Yes, for customers.', audience: 'Customers' },
    { id: 'f2', question: 'Who checks partners?', answer: 'Our team.', audience: 'General' },
    { id: 'f3', question: 'How do I get requests?', answer: 'Create a profile.', audience: 'Partners' },
  ]

  it('explains the steps for customers and for partners, with their questions', async () => {
    const user = userEvent.setup()
    server.use(http.get('*/api/v1/faqs', () => HttpResponse.json(faqs)))
    const { router } = renderRoute('/en/how-it-works')

    expect(await screen.findByRole('heading', { name: en.public.how.customers.steps.describe.title })).toBeInTheDocument()
    expect(await screen.findByText('Is it free?')).toBeInTheDocument()
    expect(screen.getByText('Who checks partners?')).toBeInTheDocument()
    expect(screen.queryByText('How do I get requests?')).not.toBeInTheDocument()
    expect(screen.getByRole('link', { name: en.public.how.customers.cta })).toHaveAttribute('href', '/en/requests/new')

    await user.click(screen.getByRole('radio', { name: en.public.how.partners.tab }))
    await waitFor(() => expect(where(router)).toBe('/en/how-it-works?for=partners'))
    expect(screen.getByRole('heading', { name: en.public.how.partners.steps.review.title })).toBeInTheDocument()
    expect(screen.getByText('How do I get requests?')).toBeInTheDocument()
    expect(screen.queryByText('Is it free?')).not.toBeInTheDocument()
    expect(screen.getByRole('link', { name: en.public.how.partners.cta })).toHaveAttribute('href', '/en/partner')

    await user.click(screen.getByRole('radio', { name: en.public.how.customers.tab }))
    await waitFor(() => expect(where(router)).toBe('/en/how-it-works'))
  })
})

describe('Information pages', () => {
  it('renders Markdown without raw HTML', async () => {
    server.use(
      http.get('*/api/v1/pages/terms', () =>
        HttpResponse.json({ slug: 'terms', title: 'Terms of use', body: '## Payments\n\nSee [prices](https://example.com).\n\n<script>alert(1)</script>', updatedAt: '2026-09-01T10:00:00Z' }),
      ),
    )
    renderRoute('/en/pages/terms')

    // The Markdown renderer is a large lazy chunk: the first load can be slow on a busy machine.
    expect(await screen.findByRole('heading', { level: 1, name: 'Terms of use' }, { timeout: 30000 })).toBeInTheDocument()
    expect(screen.getByRole('heading', { level: 2, name: 'Payments' })).toBeInTheDocument()
    expect(screen.getByRole('link', { name: 'prices' })).toHaveAttribute('href', 'https://example.com')
    expect(screen.getByText(/Updated Sep 1, 2026/)).toBeInTheDocument()
    expect(document.querySelector('main script')).toBeNull()
  })

  it('says when a page does not exist', async () => {
    server.use(http.get('*/api/v1/pages/nope', () => problem(404, 'page.not_found')))
    renderRoute('/en/pages/nope')
    expect(await screen.findByText(en.public.info.notFound)).toBeInTheDocument()
  })
})
