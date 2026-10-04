import { screen, waitFor, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { http, HttpResponse } from 'msw'
import hy from '@/i18n/locales/hy/common.json'
import { problem } from '@/test/auth'
import { faqRow } from '@/test/content'
import { renderRoute } from '@/test/renderWithProviders'
import { server } from '@/test/server'

const label = (field, language) => `${field} (${language})`

function capture(method, path, respond = () => HttpResponse.json(faqRow())) {
  const requests = []
  server.use(
    http[method](`*/api/v1/admin/${path}`, async ({ request }) => {
      requests.push({ url: new URL(request.url).pathname, body: await request.json().catch(() => null) })
      return respond()
    }),
  )
  return requests
}

async function openTab() {
  const result = renderRoute('/content/faqs')
  await screen.findByText('Ինչպե՞ս վճարել')
  return result
}

const dialogTitled = async (title) => (await screen.findAllByText(title)).map((element) => element.closest('[role="dialog"]')).find(Boolean)

describe('FAQ tab', () => {
  it('lists questions with audience and status, filters by audience and shows answers on demand', async () => {
    const user = userEvent.setup()
    const { container } = await openTab()

    expect(screen.getByText('Ինչպե՞ս միանալ')).toBeInTheDocument()
    expect(screen.getAllByText(hy.content.audience.Customers).length).toBeGreaterThan(0)
    expect(screen.getByText(hy.content.draft)).toBeInTheDocument()

    await user.click(container.querySelector('.ant-table-row-expand-icon'))
    expect(await screen.findByText('Քարտով կամ կանխիկ')).toBeInTheDocument()

    const audiences = container.querySelector('.ant-segmented')
    await user.click(within(audiences).getByText(hy.content.audience.Partners))
    await waitFor(() => expect(screen.queryByText('Ինչպե՞ս վճարել')).not.toBeInTheDocument())
    expect(screen.getByText('Ինչպե՞ս միանալ')).toBeInTheDocument()

    await user.click(within(audiences).getByText(hy.content.audience.General))
    expect(await screen.findByText(hy.content.faqs.empty)).toBeInTheDocument()
  })

  it('adds a question that needs a question and answer in the default language', async () => {
    const requests = capture('post', 'faqs', () => HttpResponse.json(faqRow({ id: 'faq-new' }), { status: 201 }))
    const user = userEvent.setup()
    await openTab()

    await user.click(screen.getByRole('button', { name: new RegExp(hy.content.faqs.add) }))
    const dialog = await dialogTitled(hy.content.faqs.add)
    await user.click(within(dialog).getByRole('button', { name: hy.catalog.form.save }))
    expect(await within(dialog).findAllByText(hy.content.form.requiredInDefault)).toHaveLength(2)

    await user.type(within(dialog).getByLabelText(label(hy.content.faqs.question, 'Հայերեն')), 'Ինչքա՞ն արժե')
    await user.type(within(dialog).getByLabelText(label(hy.content.faqs.answer, 'Հայերեն')), 'Կախված է աշխատանքից')
    await user.click(within(dialog).getByRole('combobox', { name: hy.content.faqs.audience }))
    await user.click((await screen.findAllByTitle(hy.content.audience.Customers)).find((option) => option.closest('.ant-select-dropdown')))
    await user.click(within(dialog).getByRole('button', { name: hy.catalog.form.save }))

    await waitFor(() =>
      expect(requests).toEqual([
        { url: '/api/v1/admin/faqs', body: { question: { hy: 'Ինչքա՞ն արժե' }, answer: { hy: 'Կախված է աշխատանքից' }, audience: 'Customers', sortOrder: 0 } },
      ]),
    )
    expect(await screen.findByText(hy.catalog.created)).toBeInTheDocument()
  })

  it('edits a question and shows server errors', async () => {
    let response = () => HttpResponse.json({ status: 400, code: 'validation_failed', errors: { answer: ['answer.too_long'] } }, { status: 400 })
    const requests = capture('put', 'faqs/:id', () => response())
    const user = userEvent.setup()
    await openTab()

    await user.click(screen.getByLabelText(`${hy.catalog.actions.edit}: Ինչպե՞ս վճարել`))
    const dialog = await dialogTitled(hy.content.faqs.edit)
    await user.click(within(dialog).getByRole('button', { name: hy.catalog.form.save }))
    expect(await within(dialog).findByText(hy.errors.answer.too_long)).toBeInTheDocument()

    response = () => problem(404, 'faq.not_found')
    await user.click(within(dialog).getByRole('button', { name: hy.catalog.form.save }))
    expect(await screen.findByText(hy.errors.faq.not_found)).toBeInTheDocument()

    response = () => HttpResponse.json(faqRow())
    await user.click(within(dialog).getByRole('button', { name: hy.catalog.form.save }))
    expect(await screen.findByText(hy.catalog.saved)).toBeInTheDocument()
    expect(requests.at(-1)).toEqual({
      url: '/api/v1/admin/faqs/faq-pay',
      body: { question: { hy: 'Ինչպե՞ս վճարել', en: 'How do I pay?' }, answer: { hy: 'Քարտով կամ կանխիկ', en: 'By card or cash' }, audience: 'Customers', sortOrder: 1 },
    })
  })

  it('publishes, unpublishes and deletes questions', async () => {
    const actions = capture('post', 'faqs/:id/:action')
    const deletes = capture('delete', 'faqs/:id', () => new HttpResponse(null, { status: 204 }))
    const user = userEvent.setup()
    await openTab()

    await user.click(screen.getByLabelText(`${hy.content.unpublish}: Ինչպե՞ս վճարել`))
    await user.click(screen.getByLabelText(`${hy.content.publish}: Ինչպե՞ս միանալ`))
    await waitFor(() => expect(actions.map((r) => r.url)).toEqual(['/api/v1/admin/faqs/faq-pay/unpublish', '/api/v1/admin/faqs/faq-join/publish']))

    await user.click(screen.getByLabelText(`${hy.catalog.actions.delete}: Ինչպե՞ս միանալ`))
    const confirm = (await screen.findAllByRole('tooltip')).find((tooltip) => within(tooltip).queryByText(hy.content.faqs.deleteConfirm))
    await user.click(within(confirm).getByRole('button', { name: hy.catalog.actions.delete }))
    await waitFor(() => expect(deletes.map((r) => r.url)).toEqual(['/api/v1/admin/faqs/faq-join']))
  })

  it('explains failed actions and load errors', async () => {
    capture('post', 'faqs/:id/:action', () => problem(404, 'faq.not_found'))
    const user = userEvent.setup()
    await openTab()

    await user.click(screen.getByLabelText(`${hy.content.publish}: Ինչպե՞ս միանալ`))
    expect(await screen.findByText(hy.errors.faq.not_found)).toBeInTheDocument()
  })

  it('shows an error when questions cannot be loaded', async () => {
    server.use(http.get('*/api/v1/admin/faqs', () => problem(500, 'unexpected')))
    renderRoute('/content/faqs')
    expect(await screen.findByText(hy.errors.generic)).toBeInTheDocument()
  })
})
