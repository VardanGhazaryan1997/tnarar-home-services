import { screen, waitFor, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { http, HttpResponse } from 'msw'
import hy from '@/i18n/locales/hy/common.json'
import { problem } from '@/test/auth'
import { pageRow } from '@/test/content'
import { renderRoute } from '@/test/renderWithProviders'
import { server } from '@/test/server'

const label = (field, language) => `${field} (${language})`

function capture(method, path, respond) {
  const bodies = []
  server.use(
    http[method](`*/api/v1/admin/${path}`, async ({ request }) => {
      bodies.push({ url: new URL(request.url).pathname, body: await request.json().catch(() => null) })
      return respond()
    }),
  )
  return bodies
}

describe('Page editor', () => {
  it('creates a draft page and continues editing it', async () => {
    const bodies = capture('post', 'pages', () => HttpResponse.json(pageRow({ id: 'page-new', isPublished: false }), { status: 201 }))
    const user = userEvent.setup()
    const { router } = renderRoute('/content/pages/new')

    expect(await screen.findByRole('heading', { level: 1, name: hy.content.pages.newTitle })).toBeInTheDocument()
    await user.click(screen.getByRole('button', { name: hy.catalog.form.save }))
    expect(await screen.findByText(hy.catalog.form.slugRequired)).toBeInTheDocument()
    expect(screen.getByText(hy.content.form.requiredInDefault)).toBeInTheDocument()

    await user.type(screen.getByLabelText(hy.content.form.slug), 'Bad slug')
    await user.click(screen.getByRole('button', { name: hy.catalog.form.save }))
    expect(await screen.findByText(hy.errors.slug.invalid)).toBeInTheDocument()

    await user.clear(screen.getByLabelText(hy.content.form.slug))
    await user.type(screen.getByLabelText(hy.content.form.slug), 'privacy')
    await user.type(screen.getByLabelText(label(hy.content.form.title, 'Հայերեն')), ' Գաղտնիություն ')
    await user.type(screen.getByLabelText(label(hy.content.form.body, 'Հայերեն')), 'Տեքստ')
    await user.click(screen.getByRole('tab', { name: 'English' }))
    await user.type(screen.getByLabelText(label(hy.content.form.title, 'English')), 'Privacy')
    await user.click(screen.getByRole('switch', { name: hy.content.form.showInFooter }))
    await user.click(screen.getByRole('button', { name: hy.catalog.form.save }))

    await waitFor(() =>
      expect(bodies).toEqual([
        {
          url: '/api/v1/admin/pages',
          body: { slug: 'privacy', title: { hy: 'Գաղտնիություն', en: 'Privacy' }, body: { hy: 'Տեքստ' }, showInFooter: true, sortOrder: 0 },
        },
      ]),
    )
    await waitFor(() => expect(router.state.location.pathname).toBe('/content/pages/page-new'))
  })

  it('edits a published page, keeping translations for inactive languages', async () => {
    server.use(http.get('*/api/v1/admin/pages/:id', () => HttpResponse.json(pageRow({ title: { hy: 'Պայմաններ', fr: 'Conditions' } }))))
    const bodies = capture('put', 'pages/:id', () => HttpResponse.json(pageRow()))
    const user = userEvent.setup()
    renderRoute('/content/pages/page-terms')

    expect(await screen.findByRole('heading', { level: 1, name: 'Պայմաններ' })).toBeInTheDocument()
    expect(screen.getByText(hy.content.pages.liveNotice)).toBeInTheDocument()
    const title = screen.getByLabelText(label(hy.content.form.title, 'Հայերեն'))
    await user.clear(title)
    await user.type(title, 'Նոր պայմաններ')
    await user.click(screen.getByRole('button', { name: hy.catalog.form.save }))

    await waitFor(() =>
      expect(bodies).toEqual([
        {
          url: '/api/v1/admin/pages/page-terms',
          body: { slug: 'terms', title: { hy: 'Նոր պայմաններ', fr: 'Conditions' }, body: { hy: '## Պայմաններ', en: '## Terms' }, showInFooter: true, sortOrder: 1 },
        },
      ]),
    )
    expect(await screen.findByText(hy.catalog.saved)).toBeInTheDocument()
  })

  it('shows server errors on the matching fields', async () => {
    let response = () => problem(409, 'page.slug_taken')
    capture('put', 'pages/:id', () => response())
    const user = userEvent.setup()
    renderRoute('/content/pages/page-terms')
    await screen.findByRole('heading', { level: 1, name: 'Օգտագործման պայմաններ' })

    await user.click(screen.getByRole('button', { name: hy.catalog.form.save }))
    expect(await screen.findByText(hy.errors.page.slug_taken)).toBeInTheDocument()

    response = () => HttpResponse.json({ status: 400, code: 'validation_failed', errors: { title: ['title.default_language_required'] } }, { status: 400 })
    await user.click(screen.getByRole('button', { name: hy.catalog.form.save }))
    expect(await screen.findByText(hy.errors.title.default_language_required)).toBeInTheDocument()

    response = () => problem(404, 'page.not_found')
    await user.click(screen.getByRole('button', { name: hy.catalog.form.save }))
    expect(await screen.findByText(hy.errors.page.not_found)).toBeInTheDocument()
  })

  it('publishes and unpublishes from the editor', async () => {
    let response = () => problem(422, 'page.body_required')
    const bodies = capture('post', 'pages/:id/:action', () => response())
    const user = userEvent.setup()
    renderRoute('/content/pages/page-about')
    await screen.findByRole('heading', { level: 1, name: 'Մեր մասին' })

    await user.click(screen.getByRole('button', { name: hy.content.publish }))
    expect(await screen.findByText(hy.errors.page.body_required)).toBeInTheDocument()

    response = () => HttpResponse.json(pageRow())
    // The button's loading icon fades out slowly in jsdom, so match its name loosely.
    await user.click(await screen.findByRole('button', { name: new RegExp(hy.content.publish) }))
    expect(await screen.findByText(hy.content.publishedDone)).toBeInTheDocument()
    expect(bodies.map((b) => b.url)).toEqual(['/api/v1/admin/pages/page-about/publish', '/api/v1/admin/pages/page-about/publish'])
  })

  it('unpublishes a published page', async () => {
    const bodies = capture('post', 'pages/:id/:action', () => HttpResponse.json(pageRow({ isPublished: false })))
    const user = userEvent.setup()
    renderRoute('/content/pages/page-terms')
    await screen.findByRole('heading', { level: 1, name: 'Օգտագործման պայմաններ' })

    await user.click(screen.getByRole('button', { name: hy.content.unpublish }))
    await waitFor(() => expect(bodies.map((b) => b.url)).toEqual(['/api/v1/admin/pages/page-terms/unpublish']))
    expect(await screen.findByText(hy.content.unpublished)).toBeInTheDocument()
  })

  it('says when the page does not exist', async () => {
    server.use(http.get('*/api/v1/admin/pages/:id', () => problem(404, 'page.not_found')))
    renderRoute('/content/pages/missing')

    const alert = await screen.findByRole('alert')
    expect(within(alert).getByText(hy.errors.page.not_found)).toBeInTheDocument()
    expect(screen.getByRole('link', { name: new RegExp(hy.content.pages.back) })).toHaveAttribute('href', '/content/pages')
  })
})
