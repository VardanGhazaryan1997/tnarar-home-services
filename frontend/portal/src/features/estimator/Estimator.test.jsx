import { screen, waitFor, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { http, HttpResponse } from 'msw'
import en from '@/i18n/locales/en/common.json'
import { CUSTOMER, problem, signedInAs } from '@/test/auth'
import { renderRoute } from '@/test/renderWithProviders'
import { server } from '@/test/server'

const SLOW = 30_000
const ed = en.estimator.editor
const room = en.estimator.room

const WORK_ITEMS = [
  { id: 'w-tiles', categoryId: 'cat-plumbing', slug: 'floor-tiling', name: 'Floor tiling', unit: 'SquareMeter', surface: 'Floor', priceMin: 5000, priceTypical: 6500, priceMax: 9000 },
  { id: 'w-toilet', categoryId: 'cat-plumbing', slug: 'toilet-installation', name: 'Toilet installation', unit: 'Piece', surface: 'None', priceMin: 10000, priceTypical: 15000, priceMax: 20000 },
  { id: 'w-paint', categoryId: 'cat-plumbing', slug: 'wall-painting', name: 'Wall painting', unit: 'SquareMeter', surface: 'Wall', priceMin: 1000, priceTypical: 1500, priceMax: 2000 },
]

const TEMPLATES = ['LivingRoom', 'Bedroom', 'KidsRoom', 'Kitchen', 'Bathroom', 'Toilet', 'Hallway', 'Balcony', 'Office', 'Garage', 'Other'].map((type) => ({
  type,
  items:
    type === 'Bathroom'
      ? [
          { workItemId: 'w-tiles', quantity: null, quantityPerSquareMeter: null },
          { workItemId: 'w-toilet', quantity: 1, quantityPerSquareMeter: null },
        ]
      : [],
}))

const measuredLine = (overrides) => ({
  workItemId: 'w-tiles',
  name: 'Floor tiling',
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

const BATHROOM_MEASURED = {
  type: 'Bathroom',
  floorArea: 4,
  perimeter: 8,
  wallArea: 21.6,
  ceilingArea: 4,
  skirtingLength: 8,
  lines: [
    measuredLine(),
    measuredLine({ workItemId: 'w-toilet', name: 'Toilet installation', unit: 'Piece', surface: 'None', measuredQuantity: null, quantity: 1, priceMin: 10000, priceTypical: 15000, priceMax: 20000 }),
  ],
  totalMin: 30000,
  totalTypical: 41000,
  totalMax: 56000,
}

const MEASUREMENT = { rooms: [BATHROOM_MEASURED], totalMin: 30000, totalTypical: 41000, totalMax: 56000, unpricedLines: 0 }

const SAVED = {
  id: 'est-1',
  title: 'Our flat',
  cityId: null,
  oldBuilding: false,
  shareToken: null,
  createdAt: '2026-10-01T10:00:00Z',
  updatedAt: null,
  rooms: [
    {
      id: 'room-1',
      name: 'Bathroom',
      type: 'Bathroom',
      length: null,
      width: null,
      area: 4,
      height: 2.7,
      openings: [],
      lines: [
        { workItemId: 'w-tiles', quantity: null },
        { workItemId: 'w-toilet', quantity: 1 },
      ],
    },
  ],
  measurement: MEASUREMENT,
}

/** Default answers for the estimator's data; returns the measure requests it receives. */
function estimatorServer() {
  const measured = []
  server.use(
    http.get('*/api/v1/estimates/templates', () => HttpResponse.json(TEMPLATES)),
    http.get('*/api/v1/work-items', () => HttpResponse.json(WORK_ITEMS)),
    http.post('*/api/v1/estimates/measure', async ({ request }) => {
      const body = await request.json()
      measured.push(body)
      return HttpResponse.json({ ...MEASUREMENT, rooms: body.rooms.map(() => BATHROOM_MEASURED) })
    }),
  )
  return measured
}

async function addBathroom(user) {
  await user.selectOptions(await screen.findByLabelText(ed.newRoom), 'Bathroom')
  await waitFor(() => expect(screen.getByRole('button', { name: new RegExp(ed.addRoom) })).toBeEnabled())
  await user.click(screen.getByRole('button', { name: new RegExp(ed.addRoom) }))
  const card = await screen.findByRole('region', { name: 'Bathroom' })
  await user.click(within(card).getByRole('radio', { name: room.byArea }))
  await user.type(within(card).getByLabelText(en.estimator.area), '4')
  return card
}

describe('Room-by-room estimator', () => {
  it('lets a visitor build an estimate with live prices and keeps it on the device', async () => {
    const measured = estimatorServer()
    const user = userEvent.setup()
    renderRoute('/en/estimates/new')

    expect(await screen.findByText(ed.noRooms)).toBeInTheDocument()
    const card = await addBathroom(user)

    expect(within(card).getByText('Floor tiling')).toBeInTheDocument()
    expect(within(card).getByText('Toilet installation')).toBeInTheDocument()
    const summary = screen.getByRole('complementary', { name: ed.summary })
    await waitFor(() => expect(summary).toHaveTextContent(/30[\s,.]?000/))
    expect(summary).toHaveTextContent(/56[\s,.]?000/)
    expect(measured.at(-1)).toEqual({
      oldBuilding: false,
      rooms: [
        {
          name: 'Bathroom',
          type: 'Bathroom',
          length: null,
          width: null,
          area: 4,
          height: null,
          openings: [],
          lines: [
            { workItemId: 'w-tiles', quantity: null },
            { workItemId: 'w-toilet', quantity: 1 },
          ],
        },
      ],
    })
    expect(within(card).getByText(/This room: 30/)).toBeInTheDocument()
    expect(within(summary).getByRole('link', { name: ed.signInToSave })).toHaveAttribute('href', '/en/sign-in')

    await user.click(screen.getByLabelText(en.estimator.oldBuilding))
    await waitFor(() => expect(measured.at(-1).oldBuilding).toBe(true))

    expect(JSON.parse(localStorage.getItem('hs_estimate_draft')).rooms).toHaveLength(1)
  }, SLOW)

  it('adds doors, windows and more work, and checks sizes', async () => {
    const measured = estimatorServer()
    const user = userEvent.setup()
    renderRoute('/en/estimates/new')
    const card = await addBathroom(user)

    await user.click(within(card).getByRole('button', { name: new RegExp(room.addDoor) }))
    const door = within(card).getByRole('group', { name: `${en.estimator.openings.Door} 1` })
    expect(within(door).getByLabelText(room.openingWidth)).toHaveValue('0.9')

    await user.click(within(card).getByRole('button', { name: new RegExp(room.addWork) }))
    const picker = await screen.findByRole('dialog')
    expect(within(picker).queryByText('Floor tiling')).not.toBeInTheDocument() // already in the room
    await user.type(within(picker).getByLabelText(en.estimator.picker.search), 'paint')
    await user.click(within(picker).getByRole('button', { name: `Add: Wall painting` }))
    await user.click(within(picker).getByRole('button', { name: en.estimator.picker.done }))
    expect(within(card).getByText('Wall painting')).toBeInTheDocument()
    await waitFor(() => expect(measured.at(-1).rooms[0].lines.map((line) => line.workItemId)).toContain('w-paint'))
    expect(measured.at(-1).rooms[0].openings).toEqual([{ kind: 'Door', width: 0.9, height: 2, count: 1 }])

    await user.clear(within(card).getByLabelText(en.estimator.area))
    await user.type(within(card).getByLabelText(en.estimator.area), '5000')
    expect(within(card).getByText('From 0 to 2000 m²')).toBeInTheDocument()
    expect(screen.getByText(ed.unmeasured.replace('{{number}}', '1'))).toBeInTheDocument()

    await user.click(within(card).getByRole('button', { name: room.remove }))
    expect(screen.queryByRole('region', { name: 'Bathroom' })).not.toBeInTheDocument()
  }, SLOW)

  it('saves a signed-in user’s new estimate to their account', async () => {
    estimatorServer()
    signedInAs(CUSTOMER)
    const created = []
    server.use(
      http.post('*/api/v1/me/estimates', async ({ request }) => {
        created.push(await request.json())
        return HttpResponse.json(SAVED, { status: 201 })
      }),
      http.get('*/api/v1/me/estimates/est-1', () => HttpResponse.json(SAVED)),
    )
    const user = userEvent.setup()
    const { router } = renderRoute('/en/estimates/new')
    await addBathroom(user)

    await user.click(await screen.findByRole('button', { name: new RegExp(ed.save) }))

    await waitFor(() => expect(router.state.location.pathname).toBe('/en/estimates/est-1'))
    expect(created[0]).toMatchObject({ title: ed.defaultTitle, oldBuilding: false, cityId: null })
    expect(created[0].rooms[0]).toMatchObject({ type: 'Bathroom', area: 4 })
    expect(localStorage.getItem('hs_estimate_draft')).toBeNull()
    expect(await screen.findByRole('heading', { name: 'Our flat' })).toBeInTheDocument()
  }, SLOW)

  it('asks for valid sizes before saving', async () => {
    estimatorServer()
    signedInAs(CUSTOMER)
    const user = userEvent.setup()
    renderRoute('/en/estimates/new')
    const card = await addBathroom(user)
    await user.clear(within(card).getByLabelText(en.estimator.area))

    await user.click(await screen.findByRole('button', { name: new RegExp(ed.save) }))
    expect(await screen.findByText(en.estimator.errors.fixRooms)).toBeInTheDocument()
  }, SLOW)

  it('edits, shares and deletes a saved estimate', async () => {
    estimatorServer()
    signedInAs(CUSTOMER)
    const calls = []
    let current = SAVED
    server.use(
      http.get('*/api/v1/me/estimates/est-1', () => HttpResponse.json(current)),
      http.put('*/api/v1/me/estimates/est-1', async ({ request }) => {
        const body = await request.json()
        calls.push(['put', body])
        current = { ...current, title: body.title }
        return HttpResponse.json(current)
      }),
      http.post('*/api/v1/me/estimates/est-1/share', () => {
        calls.push(['share'])
        current = { ...current, shareToken: 'abcdefghijklmnopqrstuv' }
        return HttpResponse.json(current)
      }),
      http.delete('*/api/v1/me/estimates/est-1', () => {
        calls.push(['delete'])
        return new HttpResponse(null, { status: 204 })
      }),
      http.get('*/api/v1/me/estimates', () => HttpResponse.json([])),
    )
    const user = userEvent.setup()
    const { router } = renderRoute('/en/estimates/est-1')

    const title = await screen.findByLabelText(ed.title)
    expect(screen.getByRole('button', { name: new RegExp(en.estimator.saved.save) })).toBeDisabled()
    await user.clear(title)
    await user.type(title, 'Flat in Arabkir')
    await user.click(screen.getByRole('button', { name: new RegExp(en.estimator.saved.save) }))
    expect(await screen.findByText(en.estimator.saved.saved)).toBeInTheDocument()
    expect(calls[0][1]).toMatchObject({ title: 'Flat in Arabkir', rooms: [{ name: 'Bathroom', area: 4, lines: [{ workItemId: 'w-tiles', quantity: null }, { workItemId: 'w-toilet', quantity: 1 }] }] })

    await user.click(screen.getByRole('button', { name: new RegExp(en.estimator.saved.share) }))
    expect(await screen.findByLabelText(en.estimator.saved.link)).toHaveValue(`${window.location.origin}/en/estimates/shared/abcdefghijklmnopqrstuv`)

    await user.click(screen.getByRole('button', { name: new RegExp(en.estimator.saved.delete) }))
    const dialog = await screen.findByRole('dialog')
    await user.click(within(dialog).getByRole('button', { name: en.estimator.saved.delete }))
    await waitFor(() => expect(router.state.location.pathname).toBe('/en/estimates'))
    expect(calls.map((call) => call[0])).toEqual(['put', 'share', 'delete'])
  }, SLOW)

  it('lists saved estimates and the one on this device', async () => {
    signedInAs(CUSTOMER)
    server.use(
      http.get('*/api/v1/me/estimates', () =>
        HttpResponse.json([
          { id: 'est-1', title: 'Our flat', roomCount: 3, totalMin: 300000, totalTypical: 410000, totalMax: 560000, isShared: true, createdAt: '2026-10-01T10:00:00Z', updatedAt: null },
        ]),
      ),
    )
    localStorage.setItem('hs_estimate_draft', JSON.stringify({ title: 'Dacha', oldBuilding: false, rooms: [] }))
    renderRoute('/en/estimates')

    const link = await screen.findByRole('link', { name: /Our flat/ })
    expect(link).toHaveAttribute('href', '/en/estimates/est-1')
    expect(link).toHaveTextContent(/300[\s,.]?000/)
    expect(link).toHaveTextContent(en.estimator.list.shared)
    expect(screen.getByRole('link', { name: 'Dacha' })).toHaveAttribute('href', '/en/estimates/new')
    expect(screen.getByRole('link', { name: new RegExp(en.estimator.list.continue) })).toBeInTheDocument()
  }, SLOW)

  it('invites visitors to start an estimate', async () => {
    renderRoute('/en/estimates')

    expect(await screen.findByText(en.estimator.list.visitorTitle)).toBeInTheDocument()
    expect(screen.getByRole('link', { name: new RegExp(en.estimator.list.new) })).toHaveAttribute('href', '/en/estimates/new')
  }, SLOW)

  it('shows a shared estimate read-only, and says when the link stopped working', async () => {
    server.use(
      http.get('*/api/v1/estimates/shared/good-token-1234567890', () =>
        HttpResponse.json({ title: 'Our flat', cityId: null, oldBuilding: true, updatedAt: '2026-10-01T10:00:00Z', rooms: SAVED.rooms, measurement: MEASUREMENT }),
      ),
      http.get('*/api/v1/estimates/shared/gone', () => problem(404, 'estimate.not_found')),
    )
    renderRoute('/en/estimates/shared/good-token-1234567890')

    expect(await screen.findByRole('heading', { name: 'Our flat' })).toBeInTheDocument()
    const bathroom = screen.getByRole('region', { name: 'Bathroom' })
    expect(within(bathroom).getByText('Toilet installation')).toBeInTheDocument()
    expect(screen.getByText(en.estimator.shared.oldBuilding)).toBeInTheDocument()
    expect(screen.queryByLabelText(ed.title)).not.toBeInTheDocument()
  }, SLOW)

  it('says when a shared link no longer works', async () => {
    server.use(http.get('*/api/v1/estimates/shared/gone', () => problem(404, 'estimate.not_found')))
    renderRoute('/en/estimates/shared/gone')

    expect(await screen.findByText(en.estimator.shared.notFound)).toBeInTheDocument()
  }, SLOW)
})
