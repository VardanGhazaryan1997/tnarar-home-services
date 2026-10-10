import { screen, waitFor, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { http, HttpResponse } from 'msw'
import i18n from '@/i18n'
import hy from '@/i18n/locales/hy/common.json'
import { WORK_ITEMS } from '@/test/catalog'
import { renderRoute } from '@/test/renderWithProviders'
import { server } from '@/test/server'

const SLOW = 60_000
const rt = hy.catalog.roomTemplates
const SOCKETS = {
  ...WORK_ITEMS[2],
  id: 'wi-sockets',
  slug: 'socket-installation',
  name: { hy: 'Վարդակի տեղադրում' },
  unit: 'Point',
  surface: 'None',
}

function capturePut() {
  const requests = []
  server.use(
    http.put('*/api/v1/admin/catalog/room-templates/:type', async ({ request, params }) => {
      const body = await request.json()
      requests.push({ type: params.type, body })
      return HttpResponse.json({ type: params.type, items: body.items })
    }),
  )
  return requests
}

async function openTab() {
  renderRoute('/catalog/room-templates')
  return (await screen.findByText('Պատերի սվաղում')).closest('table')
}

async function choose(user, label, option) {
  await user.click(screen.getByLabelText(label))
  await user.click((await screen.findAllByTitle(option)).find((element) => element.closest('.ant-select-dropdown')))
}

describe('Room templates tab', () => {
  it('opens from the Catalog tabs and shows a room type’s work', async () => {
    const user = userEvent.setup()
    const { router } = renderRoute('/catalog/categories')

    await user.click(await screen.findByRole('tab', { name: hy.catalog.tabs['room-templates'] }))

    await waitFor(() => expect(router.state.location.pathname).toBe('/catalog/room-templates'))
    const table = (await screen.findByText('Պատերի սվաղում')).closest('table')
    expect(within(table).getByText('Արտահոսքի վերացում')).toBeInTheDocument()
    expect(within(table).getByLabelText(`${rt.values.count}: Արտահոսքի վերացում`)).toHaveValue('2.00')
    expect(within(table).queryByLabelText(`${rt.values.count}: Պատերի սվաղում`)).not.toBeInTheDocument()
    expect(screen.getByRole('button', { name: hy.catalog.form.save })).toBeDisabled()
  }, SLOW)

  it('adds, reorders and removes work, then saves the template', async () => {
    const requests = capturePut()
    const user = userEvent.setup()
    const table = await openTab()

    await choose(user, rt.add, 'floor-tiling')
    expect(await within(table).findByText('Հատակի սալիկապատում')).toBeInTheDocument()
    expect(within(table).getByText(hy.catalog.status.hidden)).toBeInTheDocument()
    await user.click(within(table).getByLabelText(`${rt.up}: Հատակի սալիկապատում`))
    await user.click(within(table).getByLabelText(`${rt.remove}: Արտահոսքի վերացում`))
    await user.click(screen.getByRole('button', { name: hy.catalog.form.save }))

    await waitFor(() => expect(requests).toHaveLength(1))
    expect(requests[0]).toEqual({
      type: 'LivingRoom',
      body: {
        items: [
          { workItemId: 'wi-plastering', quantity: null, quantityPerSquareMeter: null },
          { workItemId: 'wi-floor-tiling', quantity: null, quantityPerSquareMeter: null },
        ],
      },
    })
    expect(await screen.findByText(hy.catalog.saved)).toBeInTheDocument()
  }, SLOW)

  it('gives counts to work that can’t be measured and checks them', async () => {
    server.use(http.get('*/api/v1/admin/catalog/work-items', () => HttpResponse.json([...WORK_ITEMS, SOCKETS])))
    const requests = capturePut()
    const user = userEvent.setup()
    await openTab()

    await choose(user, rt.roomType, `${rt.roomTypes.Bathroom} (0)`)
    expect(await screen.findByText(rt.empty)).toBeInTheDocument()
    await choose(user, rt.add, 'socket-installation')
    const count = await screen.findByLabelText(`${rt.values.count}: Վարդակի տեղադրում`)
    expect(count).toHaveValue('1.00')

    await choose(user, `${rt.amount}: Վարդակի տեղադրում`, rt.modes.perSquareMeter)
    const perSquareMeter = await screen.findByLabelText(`${rt.values.perSquareMeter}: Վարդակի տեղադրում`)
    await user.clear(perSquareMeter)
    expect(await screen.findByText(rt.perSquareMeterInvalid)).toBeInTheDocument()
    expect(screen.getByRole('button', { name: hy.catalog.form.save })).toBeDisabled()

    await user.type(perSquareMeter, '0.25')
    await user.click(screen.getByRole('button', { name: hy.catalog.form.save }))
    await waitFor(() => expect(requests).toHaveLength(1))
    expect(requests[0]).toEqual({ type: 'Bathroom', body: { items: [{ workItemId: 'wi-sockets', quantity: null, quantityPerSquareMeter: 0.25 }] } })
  }, SLOW)

  it('undoes changes and shows server errors', async () => {
    server.use(
      http.put('*/api/v1/admin/catalog/room-templates/:type', () =>
        HttpResponse.json({ status: 422, code: 'room_template.quantity_required' }, { status: 422 }),
      ),
    )
    const user = userEvent.setup()
    const table = await openTab()

    await user.click(within(table).getByLabelText(`${rt.remove}: Պատերի սվաղում`))
    await user.click(screen.getByRole('button', { name: new RegExp(rt.undo) }))
    expect(await within(table).findByText('Պատերի սվաղում')).toBeInTheDocument()
    expect(screen.getByRole('button', { name: new RegExp(rt.undo) })).toBeDisabled()

    await user.click(within(table).getByLabelText(`${rt.remove}: Պատերի սվաղում`))
    await user.click(screen.getByRole('button', { name: hy.catalog.form.save }))
    expect(await screen.findByText(i18n.t('errors.room_template.quantity_required'))).toBeInTheDocument()
  }, SLOW)
})
