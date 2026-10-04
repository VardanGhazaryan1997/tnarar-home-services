import { screen, waitFor, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { http, HttpResponse } from 'msw'
import hy from '@/i18n/locales/hy/common.json'
import { problem } from '@/test/auth'
import { reviewRow } from '@/test/operations'
import { page } from '@/test/partners'
import { renderRoute } from '@/test/renderWithProviders'
import { server } from '@/test/server'

// Several dialogs in one test: slow machines need more than the default 60 s.
const SLOW = 180_000

describe('Reviews page', () => {
  it('lists reviews and filters them', async () => {
    const requests = []
    server.use(
      http.get('*/api/v1/admin/reviews', ({ request }) => {
        requests.push(Object.fromEntries(new URL(request.url).searchParams))
        return HttpResponse.json(page([reviewRow({ reply: 'Կզանգեմ' }), reviewRow({ id: 'review-2', isHidden: true, hiddenReason: 'Հեռախոս', text: null })], { totalCount: 30 }))
      }),
    )
    const user = userEvent.setup()
    renderRoute('/reviews')

    const table = await screen.findByRole('table')
    expect(await within(table).findByText('Զանգեք ինձ 091000111')).toBeInTheDocument()
    expect(within(table).getByText(`${hy.reviews.reply}: Կզանգեմ`)).toBeInTheDocument()
    expect(within(table).getByText('Թաքցված է՝ Հեռախոս')).toBeInTheDocument()
    expect(requests[0]).toEqual({ page: '1', pageSize: '20' })

    await user.click(screen.getByText(hy.reviews.filters.hidden))
    await waitFor(() => expect(requests.at(-1)).toEqual({ hidden: 'true', page: '1', pageSize: '20' }))
    await user.click(screen.getByRole('combobox', { name: hy.reviews.filters.rating }))
    await user.click(await screen.findByTitle('2 աստղ կամ պակաս'))
    await waitFor(() => expect(requests.at(-1)).toEqual({ hidden: 'true', maxRating: '2', page: '1', pageSize: '20' }))
    await user.click(await screen.findByTitle('2'))
    await waitFor(() => expect(requests.at(-1)).toEqual({ hidden: 'true', maxRating: '2', page: '2', pageSize: '20' }))
    await user.click(screen.getByText(hy.reviews.filters.visible))
    await waitFor(() => expect(requests.at(-1)).toEqual({ hidden: 'false', maxRating: '2', page: '1', pageSize: '20' }))
    await user.click(screen.getByText(hy.reviews.filters.all))
    await waitFor(() => expect(requests.at(-1)).toEqual({ maxRating: '2', page: '1', pageSize: '20' }))
  })

  it('hides a review with a reason and restores it', async () => {
    const calls = []
    server.use(
      http.get('*/api/v1/admin/reviews', () => HttpResponse.json(page([reviewRow(), reviewRow({ id: 'review-2', isHidden: true, hiddenReason: 'Հեռախոս' })]))),
      http.post('*/api/v1/admin/reviews/:id/:action', async ({ params, request }) => {
        calls.push({ id: params.id, action: params.action, body: params.action === 'hide' ? await request.json() : null })
        if (calls.length >= 3) return problem(422, params.action === 'hide' ? 'review.already_hidden' : 'review.not_hidden')
        return HttpResponse.json(reviewRow())
      }),
    )
    const user = userEvent.setup()
    renderRoute('/reviews')

    await user.click(await screen.findByRole('button', { name: hy.reviews.hide.button }))
    const dialog = await screen.findByRole('dialog')
    await user.click(within(dialog).getByRole('button', { name: hy.reviews.hide.confirm }))
    expect(await within(dialog).findByText(hy.common.reasonRequired)).toBeInTheDocument()
    await user.type(within(dialog).getByRole('textbox'), 'Հեռախոսահամար')
    await user.click(within(dialog).getByRole('button', { name: hy.reviews.hide.confirm }))
    await waitFor(() => expect(calls[0]).toEqual({ id: 'review-1', action: 'hide', body: { reason: 'Հեռախոսահամար' } }))
    expect(await screen.findByText(hy.reviews.hide.done)).toBeInTheDocument()

    await user.click(screen.getByRole('button', { name: hy.reviews.restore.button }))
    await waitFor(() => expect(calls[1]).toEqual({ id: 'review-2', action: 'restore', body: null }))
    expect(await screen.findByText(hy.reviews.restore.done)).toBeInTheDocument()

    await user.click(screen.getByRole('button', { name: hy.reviews.restore.button }))
    expect(await screen.findByText(hy.errors.review.not_hidden)).toBeInTheDocument()

    await user.click(screen.getByRole('button', { name: hy.reviews.hide.button }))
    const again = (await screen.findAllByRole('dialog')).at(-1)
    await user.type(within(again).getByRole('textbox'), 'Կրկին')
    await user.click(within(again).getByRole('button', { name: hy.reviews.hide.confirm }))
    expect(await screen.findByText(hy.errors.review.already_hidden)).toBeInTheDocument()
  }, SLOW)

  it('shows list errors', async () => {
    server.use(http.get('*/api/v1/admin/reviews', () => problem(500, 'boom')))
    renderRoute('/reviews')
    expect(await screen.findByText(hy.errors.generic)).toBeInTheDocument()
  })
})
