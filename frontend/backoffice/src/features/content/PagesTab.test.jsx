import { screen, waitFor, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { http, HttpResponse } from 'msw'
import i18n from '@/i18n'
import hy from '@/i18n/locales/hy/common.json'
import { problem, signedInAs, staffMember } from '@/test/auth'
import { pageRow } from '@/test/content'
import { renderRoute } from '@/test/renderWithProviders'
import { server } from '@/test/server'

const t = (key, options) => i18n.t(key, options)

function capture(method, path, respond = () => HttpResponse.json(pageRow())) {
  const requests = []
  server.use(
    http[method](`*/api/v1/admin/${path}`, ({ request }) => {
      requests.push(new URL(request.url).pathname)
      return respond()
    }),
  )
  return requests
}

async function openTab() {
  const result = renderRoute('/content')
  await screen.findByRole('link', { name: 'Օգտագործման պայմաններ' })
  return result
}

describe('Pages tab', () => {
  it('opens from /content and lists pages with their address, footer link and status', async () => {
    const { router } = await openTab()

    expect(router.state.location.pathname).toBe('/content/pages')
    expect(screen.getByRole('heading', { level: 1, name: hy.nav.content })).toBeInTheDocument()
    expect(screen.getByText('/pages/terms')).toBeInTheDocument()
    expect(screen.getByRole('link', { name: 'Մեր մասին' })).toHaveAttribute('href', '/content/pages/page-about')
    expect(screen.getByText(hy.content.yes)).toBeInTheDocument()
    expect(screen.getByText(hy.content.published)).toBeInTheDocument()
    expect(screen.getByText(hy.content.draft)).toBeInTheDocument()
  })

  it('shows titles in the UI language', async () => {
    await i18n.changeLanguage('en')
    renderRoute('/content/pages')
    expect(await screen.findByRole('link', { name: 'Terms of use' })).toBeInTheDocument()
    // No English title: the default language is shown.
    expect(screen.getByRole('link', { name: 'Մեր մասին' })).toBeInTheDocument()
  })

  it('publishes and unpublishes pages', async () => {
    const requests = capture('post', 'pages/:id/:action')
    const user = userEvent.setup()
    await openTab()

    await user.click(screen.getByLabelText(`${hy.content.unpublish}: Օգտագործման պայմաններ`))
    await user.click(screen.getByLabelText(`${hy.content.publish}: Մեր մասին`))

    await waitFor(() => expect(requests).toEqual(['/api/v1/admin/pages/page-terms/unpublish', '/api/v1/admin/pages/page-about/publish']))
    expect(await screen.findByText(hy.content.unpublished)).toBeInTheDocument()
  })

  it('explains why a page cannot be published', async () => {
    capture('post', 'pages/:id/:action', () => problem(422, 'page.body_required'))
    const user = userEvent.setup()
    await openTab()

    await user.click(screen.getByLabelText(`${hy.content.publish}: Մեր մասին`))
    expect(await screen.findByText(hy.errors.page.body_required)).toBeInTheDocument()
  })

  it('deletes a page after confirmation', async () => {
    const requests = capture('delete', 'pages/:id', () => new HttpResponse(null, { status: 204 }))
    const user = userEvent.setup()
    await openTab()

    await user.click(screen.getByLabelText(`${hy.catalog.actions.delete}: Մեր մասին`))
    const confirm = await screen.findByRole('tooltip')
    expect(within(confirm).getByText(t('content.pages.deleteConfirm', { name: 'Մեր մասին' }))).toBeInTheDocument()
    await user.click(within(confirm).getByRole('button', { name: hy.catalog.actions.delete }))

    await waitFor(() => expect(requests).toEqual(['/api/v1/admin/pages/page-about']))
    expect(await screen.findByText(hy.catalog.deleted)).toBeInTheDocument()
  })

  it('opens the editor to add or edit a page', async () => {
    const user = userEvent.setup()
    const { router } = await openTab()

    await user.click(screen.getByLabelText(`${hy.catalog.actions.edit}: Մեր մասին`))
    await waitFor(() => expect(router.state.location.pathname).toBe('/content/pages/page-about'))
    await user.click(await screen.findByRole('link', { name: new RegExp(hy.content.pages.back) }))
    await user.click(await screen.findByRole('button', { name: new RegExp(hy.content.pages.add) }))
    await waitFor(() => expect(router.state.location.pathname).toBe('/content/pages/new'))
  })

  it('shows load errors and an empty list', async () => {
    server.use(http.get('*/api/v1/admin/pages', () => problem(500, 'unexpected')))
    renderRoute('/content/pages')

    expect(await screen.findByText(hy.errors.generic)).toBeInTheDocument()
    expect(screen.getByText(hy.content.pages.empty)).toBeInTheDocument()
  })

  it('is closed to staff without content.manage', async () => {
    signedInAs(staffMember(['users.view']))
    renderRoute('/content/pages')
    expect(await screen.findByText(hy.forbidden.title)).toBeInTheDocument()
  })
})
